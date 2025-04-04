//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

#include "VideoFrameProcessor.h"
#include <winrt/Windows.Foundation.Collections.h>
#include <MemoryBuffer.h>
#include "utils.hpp"

using namespace winrt::Windows::Foundation::Collections;
using namespace winrt::Windows::Media::Capture;
using namespace winrt::Windows::Media::Capture::Frames;
using namespace winrt::Windows::Perception::Spatial;
using namespace winrt::Windows::Graphics::Imaging;
using namespace winrt::Windows::Storage;

const int kImageWidth = 760;

const long long ticksPerSecond = 10'000'000;
const long long timeInterval = long long(ticksPerSecond * 0.05);

winrt::Windows::Foundation::IAsyncAction VideoFrameProcessor::InitializeAsync()
{
	auto mediaFrameSourceGroups{ co_await MediaFrameSourceGroup::FindAllAsync() };

	MediaFrameSourceGroup selectedSourceGroup = nullptr;
	MediaCaptureVideoProfile profile = nullptr;
	MediaCaptureVideoProfileMediaDescription desc = nullptr;
	std::vector<MediaFrameSourceInfo> selectedSourceInfos;

	// Find MediaFrameSourceGroup
	for (const MediaFrameSourceGroup& mediaFrameSourceGroup : mediaFrameSourceGroups)
	{
		auto knownProfiles = MediaCapture::FindKnownVideoProfiles(mediaFrameSourceGroup.Id(), KnownVideoProfile::VideoConferencing);
		for (const auto& knownProfile : knownProfiles)
		{
			for (auto knownDesc : knownProfile.SupportedRecordMediaDescription())
			{
				if ((knownDesc.Width() == kImageWidth) && (std::round(knownDesc.FrameRate()) == 15))
				{
					DebugPrint("Main camera resolution:", knownDesc.Width(), "*", knownDesc.Height(), "Framerate:", knownDesc.FrameRate());

					profile = knownProfile;
					desc = knownDesc;
					selectedSourceGroup = mediaFrameSourceGroup;
					break;
				}
			}
		}
	}

	winrt::check_bool(selectedSourceGroup != nullptr);

	for (auto sourceInfo : selectedSourceGroup.SourceInfos())
	{
		// Workaround since multiple Color sources can be found,
		// and not all of them are necessarily compatible with the selected video profile
		if (sourceInfo.SourceKind() == MediaFrameSourceKind::Color)
		{
			selectedSourceInfos.push_back(sourceInfo);
		}
	}
	winrt::check_bool(!selectedSourceInfos.empty());

	// Initialize a MediaCapture object
	MediaCaptureInitializationSettings settings;
	settings.VideoProfile(profile);
	settings.RecordMediaDescription(desc);
	settings.VideoDeviceId(selectedSourceGroup.Id());
	settings.StreamingCaptureMode(StreamingCaptureMode::Video);
	settings.MemoryPreference(MediaCaptureMemoryPreference::Cpu);
	settings.SharingMode(MediaCaptureSharingMode::ExclusiveControl);
	settings.SourceGroup(selectedSourceGroup);

	MediaCapture mediaCapture = MediaCapture();
	co_await mediaCapture.InitializeAsync(settings);
	MediaFrameSource selectedSource = nullptr;
	MediaFrameFormat preferredFormat = nullptr;

	for (MediaFrameSourceInfo sourceInfo : selectedSourceInfos)
	{
		auto tmpSource = mediaCapture.FrameSources().Lookup(sourceInfo.Id());
		for (MediaFrameFormat format : tmpSource.SupportedFormats())
		{
			if (format.VideoFormat().Width() == kImageWidth)
			{
				selectedSource = tmpSource;
				preferredFormat = format;
				break;
			}
		}
	}

	winrt::check_bool(preferredFormat != nullptr);
	co_await selectedSource.SetFormatAsync(preferredFormat);
	auto mediaFrameReader = co_await mediaCapture.CreateFrameReaderAsync(selectedSource);
	auto status = co_await mediaFrameReader.StartAsync();

	winrt::check_bool(status == MediaFrameReaderStartStatus::Success);
	mediaFrameReader.AcquisitionMode(MediaFrameReaderAcquisitionMode::Realtime);
	m_OnFrameArrivedRegistration = mediaFrameReader.FrameArrived({ this, &VideoFrameProcessor::OnFrameArrived });
	m_OnFailedRegistration = mediaCapture.Failed({ this, &VideoFrameProcessor::OnFailed });
	m_OnExclusiveControlStatusChangedRegistration = mediaCapture.CaptureDeviceExclusiveControlStatusChanged({ this, &VideoFrameProcessor::OnExclusiveControlStatusChanged });
	m_OnCameraStreamStateChangedRegistration = mediaCapture.CameraStreamStateChanged({ this, &VideoFrameProcessor::OnCameraStreamStateChanged });

	DebugPrint("Main camera v2 initialized");
}

void VideoFrameProcessor::OnFrameArrived(const MediaFrameReader& sender, const MediaFrameArrivedEventArgs& args)
{
	if (!m_fStreaming) return;

	if (MediaFrameReference frame = sender.TryAcquireLatestFrame())
	{
		m_latestFrame = frame;
		if (m_latestFrame != nullptr)
		{
			long long timestamp = m_converter.RelativeTicksToAbsoluteTicks(HundredsOfNanoseconds(m_latestFrame.SystemRelativeTime().Value().count())).count();
			if (timestamp > m_latestTimestamp + timeInterval)
			{
				m_latestTimestamp = timestamp;
				SoftwareBitmap softwareBitmap = SoftwareBitmap::Convert(m_latestFrame.VideoMediaFrame().SoftwareBitmap(), BitmapPixelFormat::Rgba8);
				DumpFrame(softwareBitmap, m_latestTimestamp);
			}
		}
	}
}

void VideoFrameProcessor::OnExclusiveControlStatusChanged(const winrt::Windows::Media::Capture::MediaCapture& mediaCapture, const winrt::Windows::Media::Capture::MediaCaptureDeviceExclusiveControlStatusChangedEventArgs& args)
{
	DebugPrint("Main camera ExclusiveControlStatusChanged", (size_t)mediaCapture.CameraStreamState());
}

void VideoFrameProcessor::OnCameraStreamStateChanged(const winrt::Windows::Media::Capture::MediaCapture& mediaCapture, const winrt::Windows::Foundation::IInspectable& args)
{
	DebugPrint("Main camera CameraStreamStateChanged", (size_t)mediaCapture.CameraStreamState());
}

void VideoFrameProcessor::OnFailed(const winrt::Windows::Media::Capture::MediaCapture& mediaCapture, const winrt::Windows::Media::Capture::MediaCaptureFailedEventArgs& args)
{
	DebugPrint("Main camera Failed", (size_t)mediaCapture.CameraStreamState());
}

void VideoFrameProcessor::DumpFrame(const SoftwareBitmap& softwareBitmap, long long timestamp)
{
	// Get bitmap buffer object of the frame
	BitmapBuffer bitmapBuffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode::Read);

	// Get raw pointer to the buffer object
	uint32_t pixelBufferDataLength = 0;
	uint8_t* pixelBufferData;

	auto spMemoryBufferByteAccess{ bitmapBuffer.CreateReference().as<::Windows::Foundation::IMemoryBufferByteAccess>() };
	winrt::check_hresult(spMemoryBufferByteAccess->GetBuffer(&pixelBufferData, &pixelBufferDataLength));

	auto fx = m_latestFrame.VideoMediaFrame().CameraIntrinsics().FocalLength().x;
	auto fy = m_latestFrame.VideoMediaFrame().CameraIntrinsics().FocalLength().y;
	auto px = m_latestFrame.VideoMediaFrame().CameraIntrinsics().PrincipalPoint().x;
	auto py = m_latestFrame.VideoMediaFrame().CameraIntrinsics().PrincipalPoint().y;
	auto transform = m_latestFrame.CoordinateSystem().TryGetTransformTo(m_worldCoordSystem);
	if (transform != nullptr) {
		m_PVToWorldTransform = transform.Value();
	}
	else {
		DebugPrint("PV camera failed to locate");
	}

	if (0 == imData.SaveJpeg(softwareBitmap.PixelWidth(), softwareBitmap.PixelHeight(), 4, pixelBufferData)) {
		DebugPrint("Failed to encode PV image!");
		return;
	}

	FrameHeader header;
	header.FrameType = SensorType::PhotoVideo;
	header.Timestamp = timestamp;
	header.ImageHeight = softwareBitmap.PixelHeight(),
	header.ImageWidth = softwareBitmap.PixelWidth();
	header.PixelStride = imData.size;
	header.ExtraInfoSize = sizeof(float) * 20;

	if (m_httpconnection.IsReadyToSendAndLock()) {
		m_httpconnection.WriteHeader(header);

		m_httpconnection.m_writer.WriteBytes({ imData.buffer.get() , imData.size });

		m_httpconnection.m_writer.WriteBytes({ (BYTE*)&m_PVToWorldTransform , sizeof(float) * 16 });
		m_httpconnection.m_writer.WriteSingle(fx);
		m_httpconnection.m_writer.WriteSingle(fy);
		m_httpconnection.m_writer.WriteSingle(px);
		m_httpconnection.m_writer.WriteSingle(py);

		m_httpconnection.StoreAsync();
	}
}

void VideoFrameProcessor::StartStreaming()
{
	m_fStreaming = true;
}

void VideoFrameProcessor::StopStreaming()
{
	m_fStreaming = false;
}
