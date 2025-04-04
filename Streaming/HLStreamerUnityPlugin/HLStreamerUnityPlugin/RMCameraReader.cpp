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

#include <winrt/Windows.Foundation.Collections.h>
#include "RMCameraReader.h"
#include "utils.hpp"

const long long ticksPerSecond = 10'000'000;

#ifdef ENABLE_DEPTH
namespace Depth
{
	enum InvalidationMasks
	{
		Invalid = 0x80,
	};
}
#endif

void RMCameraReader::CameraUpdateThread(RMCameraReader* pCameraReader, HANDLE camConsentGiven, ResearchModeSensorConsent* camAccessConsent)
{
	HRESULT hr = S_OK;

	DWORD waitResult = WaitForSingleObject(camConsentGiven, INFINITE);

	if (waitResult == WAIT_OBJECT_0)
	{
		switch (*camAccessConsent)
		{
		case ResearchModeSensorConsent::Allowed:
			OutputDebugString(L"Access is granted");
			break;
		case ResearchModeSensorConsent::DeniedBySystem:
			OutputDebugString(L"Access is denied by the system");
			hr = E_ACCESSDENIED;
			break;
		case ResearchModeSensorConsent::DeniedByUser:
			OutputDebugString(L"Access is denied by the user");
			hr = E_ACCESSDENIED;
			break;
		case ResearchModeSensorConsent::NotDeclaredByApp:
			OutputDebugString(L"Capability is not declared in the app manifest");
			hr = E_ACCESSDENIED;
			break;
		case ResearchModeSensorConsent::UserPromptRequired:
			OutputDebugString(L"Capability user prompt required");
			hr = E_ACCESSDENIED;
			break;
		default:
			OutputDebugString(L"Access is denied by the system");
			hr = E_ACCESSDENIED;
			break;
		}
	}
	else
	{
		hr = E_UNEXPECTED;
	}

	winrt::check_hresult(hr);

	hr = pCameraReader->m_pRMSensor->OpenStream();
	if (FAILED(hr))
	{
		pCameraReader->m_pRMSensor->Release();
		pCameraReader->m_pRMSensor = nullptr;
		winrt::check_hresult(hr);
	}

	pCameraReader->DumpCalibration();

	while (!pCameraReader->m_fExit && pCameraReader->m_pRMSensor)
	{
		IResearchModeSensorFrame* pSensorFrame = nullptr;

		hr = pCameraReader->m_pRMSensor->GetNextBuffer(&pSensorFrame);

		if (SUCCEEDED(hr))
		{
			pCameraReader->m_pSensorFrame = pSensorFrame;
			if (pCameraReader->m_pSensorFrame)
			{
				if (pCameraReader->m_fStreaming && pCameraReader->IsNewTimestamp(pCameraReader->m_pSensorFrame))
				{
					pCameraReader->SaveFrame(pCameraReader->m_pSensorFrame);
				}
				pCameraReader->m_pSensorFrame->Release();
			}
		}
	}

	if (pCameraReader->m_pRMSensor)
	{
		pCameraReader->m_pRMSensor->CloseStream();
	}
}

void RMCameraReader::StartStreaming()
{
	m_fStreaming = true;
}

void RMCameraReader::StopStreaming()
{
	m_fStreaming = false;
}

bool RMCameraReader::IsNewTimestamp(IResearchModeSensorFrame* pSensorFrame)
{
	ResearchModeSensorTimestamp timestamp;
	winrt::check_hresult(pSensorFrame->GetTimeStamp(&timestamp));

	if (m_sensorType == SensorType::VisibleLightLeftLeft || m_sensorType == SensorType::VisibleLightLeftFront || m_sensorType == SensorType::VisibleLightRightFront || m_sensorType == SensorType::VisibleLightRightRight)
	{
		if (m_prevTimestamp + ticksPerSecond > timestamp.HostTicks) return false;
	}
	else if (m_prevTimestamp == timestamp.HostTicks)
	{
		return false;
	}

	m_prevTimestamp = timestamp.HostTicks;

	return true;
}

#ifdef ENABLE_DEPTH
void RMCameraReader::SaveDepth(IResearchModeSensorFrame* pSensorFrame, IResearchModeSensorDepthFrame* pDepthFrame)
{
	getRigToWorld();

	// Get resolution
	ResearchModeSensorResolution resolution;
	pSensorFrame->GetResolution(&resolution);

	const UINT16* pAbImage = nullptr;
	size_t outAbBufferCount = 0;

	const UINT16* pDepth = nullptr;
	size_t outDepthBufferCount = 0;

	const BYTE* pSigma = nullptr;
	size_t outSigmaBufferCount = 0;

	HundredsOfNanoseconds timestamp = m_converter.RelativeTicksToAbsoluteTicks(HundredsOfNanoseconds((long long)m_prevTimestamp));

	winrt::check_hresult(pDepthFrame->GetSigmaBuffer(&pSigma, &outSigmaBufferCount));
	winrt::check_hresult(pDepthFrame->GetAbDepthBuffer(&pAbImage, &outAbBufferCount));
	winrt::check_hresult(pDepthFrame->GetBuffer(&pDepth, &outDepthBufferCount));

	assert(outAbBufferCount == outDepthBufferCount);
	assert(outAbBufferCount == outSigmaBufferCount);

	// convert 16bit ab image to 8bit
	for (size_t i = 0; i < outAbBufferCount; ++i) {
		UINT16 d = pAbImage[i] / 40;
		if (d > 255) d = 255;
		buffer[i] = (uint8_t)d;
	}

	if (0 == imData.SaveJpeg(resolution.Width, resolution.Height, 1, buffer.get())) {
		DebugPrint("Failed to encode active brightness image!");
		return;
	}

	FrameHeader header;
	header.FrameType = m_sensorType;
	header.Timestamp = timestamp.count();
	header.ImageWidth = resolution.Width;
	header.ImageHeight = resolution.Height;
	header.PixelStride = sizeof(UINT16);
	header.ExtraInfoSize = sizeof(float) * 16 + imData.size;

	if (m_httpconnection.IsReadyToSendAndLock()) {
		m_httpconnection.WriteHeader(header);

		// Validate depth
		for (size_t i = 0; i < outAbBufferCount; ++i)
		{
			UINT16 d;
			const bool invalid = (pSigma[i] & Depth::InvalidationMasks::Invalid) > 0;
			if (invalid)
			{
				d = 0;
			}
			else
			{
				d = pDepth[i];
			}

			m_httpconnection.m_writer.WriteUInt16(d);
		}

		m_httpconnection.m_writer.WriteBytes({ (BYTE*)&m_rigToWorldTransform , sizeof(float) * 16 });
		m_httpconnection.m_writer.WriteBytes({ imData.buffer.get(), (uint32_t)imData.size });

		m_httpconnection.StoreAsync();
	}
}
#endif

void RMCameraReader::SaveVLC(IResearchModeSensorFrame* pSensorFrame, IResearchModeSensorVLCFrame* pVLCFrame)
{
	getRigToWorld();

	// Get header
	ResearchModeSensorResolution resolution;
	winrt::check_hresult(pSensorFrame->GetResolution(&resolution));

	HundredsOfNanoseconds timestamp = m_converter.RelativeTicksToAbsoluteTicks(HundredsOfNanoseconds(checkAndConvertUnsigned(m_prevTimestamp)));

	// Convert the software bitmap to raw bytes    
	size_t outBufferCount = 0;
	const BYTE* pImage = nullptr;

	winrt::check_hresult(pVLCFrame->GetBuffer(&pImage, &outBufferCount));

	if (0 == imData.SaveJpeg(resolution.Width, resolution.Height, 1, pImage)) {
		DebugPrint("Failed to encode grayscale image!");
		return;
	}

	FrameHeader header;
	header.FrameType = m_sensorType;
	header.Timestamp = timestamp.count();
	header.ImageWidth = resolution.Width;
	header.ImageHeight = resolution.Height;
	header.PixelStride = imData.size;
	header.ExtraInfoSize = sizeof(float) * 16;

	if (m_httpconnection.IsReadyToSendAndLock()) {
		m_httpconnection.WriteHeader(header);

		m_httpconnection.m_writer.WriteBytes({ imData.buffer.get() , imData.size });
		m_httpconnection.m_writer.WriteBytes({ (BYTE*)&m_rigToWorldTransform , sizeof(float) * 16 });

		m_httpconnection.StoreAsync();
	}
}

#ifdef ENABLE_IMU
void RMCameraReader::SaveAccel(IResearchModeAccelFrame* pAccelFrame)
{
	const AccelDataStruct* pAccelBuffer;
	size_t BufferOutLength;
	HRESULT hr = pAccelFrame->GetCalibratedAccelarationSamples(&pAccelBuffer, &BufferOutLength);
	if (FAILED(hr)) {
		return;
	}

	HundredsOfNanoseconds timestamp = m_converter.RelativeTicksToAbsoluteTicks(HundredsOfNanoseconds(checkAndConvertUnsigned(m_prevTimestamp)));

	FrameHeader header;
	header.FrameType = m_sensorType;
	header.Timestamp = timestamp.count();
	header.ImageWidth = 3;
	header.ImageHeight = BufferOutLength;
	header.PixelStride = sizeof(float);
	header.ExtraInfoSize = sizeof(uint64_t) * BufferOutLength;

	if (m_httpconnection.IsReadyToSendAndLock()) {
		m_httpconnection.WriteHeader(header);

		for (size_t i = 0; i < BufferOutLength; i++) {
			m_httpconnection.m_writer.WriteSingle(pAccelBuffer[i].AccelValues[0]);
			m_httpconnection.m_writer.WriteSingle(pAccelBuffer[i].AccelValues[1]);
			m_httpconnection.m_writer.WriteSingle(pAccelBuffer[i].AccelValues[2]);
		}

		for (size_t i = 0; i < BufferOutLength; i++) {
			m_httpconnection.m_writer.WriteUInt64(pAccelBuffer[i].VinylHupTicks);
		}

		m_httpconnection.StoreAsync();
	}
}

void RMCameraReader::SaveGyro(IResearchModeGyroFrame* pGyroFrame)
{
	const GyroDataStruct* pGyroBuffer;
	size_t BufferOutLength;
	HRESULT hr = pGyroFrame->GetCalibratedGyroSamples(&pGyroBuffer, &BufferOutLength);
	if (FAILED(hr)) {
		return;
	}

	HundredsOfNanoseconds timestamp = m_converter.RelativeTicksToAbsoluteTicks(HundredsOfNanoseconds(checkAndConvertUnsigned(m_prevTimestamp)));

	FrameHeader header;
	header.FrameType = m_sensorType;
	header.Timestamp = timestamp.count();
	header.ImageWidth = 3;
	header.ImageHeight = BufferOutLength;
	header.PixelStride = sizeof(float);
	header.ExtraInfoSize = sizeof(uint64_t) * BufferOutLength;

	if (m_httpconnection.IsReadyToSendAndLock()) {
		m_httpconnection.WriteHeader(header);

		for (size_t i = 0; i < BufferOutLength; i++) {
			m_httpconnection.m_writer.WriteSingle(pGyroBuffer[i].GyroValues[0]);
			m_httpconnection.m_writer.WriteSingle(pGyroBuffer[i].GyroValues[1]);
			m_httpconnection.m_writer.WriteSingle(pGyroBuffer[i].GyroValues[2]);
		}

		for (size_t i = 0; i < BufferOutLength; i++) {
			m_httpconnection.m_writer.WriteUInt64(pGyroBuffer[i].VinylHupTicks);
		}

		m_httpconnection.StoreAsync();
	}
}

void RMCameraReader::SaveMag(IResearchModeMagFrame* pMagFrame)
{
	const MagDataStruct* pMagBuffer;
	size_t BufferOutLength;
	HRESULT hr = pMagFrame->GetMagnetometerSamples(&pMagBuffer, &BufferOutLength);
	if (FAILED(hr)) {
		return;
	}

	HundredsOfNanoseconds timestamp = m_converter.RelativeTicksToAbsoluteTicks(HundredsOfNanoseconds(checkAndConvertUnsigned(m_prevTimestamp)));

	FrameHeader header;
	header.FrameType = m_sensorType;
	header.Timestamp = timestamp.count();
	header.ImageWidth = 3;
	header.ImageHeight = BufferOutLength;
	header.PixelStride = sizeof(float);
	header.ExtraInfoSize = sizeof(uint64_t) * BufferOutLength;

	if (m_httpconnection.IsReadyToSendAndLock()) {
		m_httpconnection.WriteHeader(header);

		for (size_t i = 0; i < BufferOutLength; i++) {
			m_httpconnection.m_writer.WriteSingle(pMagBuffer[i].MagValues[0]);
			m_httpconnection.m_writer.WriteSingle(pMagBuffer[i].MagValues[1]);
			m_httpconnection.m_writer.WriteSingle(pMagBuffer[i].MagValues[2]);
		}

		for (size_t i = 0; i < BufferOutLength; i++) {
			m_httpconnection.m_writer.WriteUInt64(pMagBuffer[i].VinylHupTicks);
		}

		m_httpconnection.StoreAsync();
	}
}
#endif

winrt::Windows::Foundation::IAsyncAction RMCameraReader::DumpCalibration()
{
	if (m_sensorType == SensorType::IMUAccel || m_sensorType == SensorType::IMUGyro || m_sensorType == SensorType::IMUMag) co_return;

	IResearchModeSensorFrame* pSensorFrame = nullptr;

	HRESULT hr = m_pRMSensor->GetNextBuffer(&pSensorFrame);
	if (FAILED(hr)) {
		DebugPrint("Failed to dump calibration");
		co_return;
	}

	// Get frame resolution (could also be stored once at the beginning of the capture)
	ResearchModeSensorResolution resolution;
	winrt::check_hresult(pSensorFrame->GetResolution(&resolution));

	ResearchModeSensorTimestamp timestamp;
	winrt::check_hresult(pSensorFrame->GetTimeStamp(&timestamp));

	// Get camera sensor object
	IResearchModeCameraSensor* pCameraSensor = nullptr;
	hr = m_pRMSensor->QueryInterface(IID_PPV_ARGS(&pCameraSensor));
	winrt::check_hresult(hr);

	// Get extrinsics (rotation and translation) with respect to the rigNode
	DirectX::XMFLOAT4X4 cameraViewMatrix;
	pCameraSensor->GetCameraExtrinsicsMatrix(&cameraViewMatrix);

	float uv[2];
	float xy[2];
	std::vector<float> lutTable(size_t(resolution.Width * resolution.Height) * 3);
	auto pLutTable = lutTable.data();

	for (size_t y = 0; y < resolution.Height; y++)
	{
		uv[1] = (y + 0.5f);
		for (size_t x = 0; x < resolution.Width; x++)
		{
			uv[0] = (x + 0.5f);
			hr = pCameraSensor->MapImagePointToCameraUnitPlane(uv, xy);
			if (FAILED(hr))
			{
				*pLutTable++ = xy[0];
				*pLutTable++ = xy[1];
				*pLutTable++ = 0.f;
				continue;
			}
			float z = 1.0f;
			const float norm = sqrtf(xy[0] * xy[0] + xy[1] * xy[1] + z * z);
			const float invNorm = 1.0f / norm;
			xy[0] *= invNorm;
			xy[1] *= invNorm;
			z *= invNorm;

			// Dump LUT row
			*pLutTable++ = xy[0];
			*pLutTable++ = xy[1];
			*pLutTable++ = z;
		}
	}

	HttpClientConnection connection;
	connection.ConnectTo(m_httpconnection.m_uri.ToString() + L"Cal");

	FrameHeader header;
	header.FrameType = SensorType::Calibration;
	header.Timestamp = m_converter.RelativeTicksToAbsoluteTicks(HundredsOfNanoseconds(checkAndConvertUnsigned(timestamp.HostTicks))).count();
	header.ImageWidth = resolution.Width;
	header.ImageHeight = resolution.Height;
	header.PixelStride = sizeof(float) * 3;
	header.ExtraInfoSize = sizeof(float) * 16;

	pSensorFrame->Release();

	if (connection.IsReadyToSendAndLock()) {
		connection.WriteHeader(header);

		connection.WriteVector(lutTable);
		connection.m_writer.WriteBytes({ (BYTE*)&cameraViewMatrix , sizeof(float) * 16 });

		co_await connection.StoreAsync();
	}
}

void RMCameraReader::getRigToWorld()
{
	auto timestamp = winrt::Windows::Perception::PerceptionTimestampHelper::FromSystemRelativeTargetTime(HundredsOfNanoseconds(checkAndConvertUnsigned(m_prevTimestamp)));
	auto location = m_locator.TryLocateAtTimestamp(timestamp, m_worldCoordSystem);
	if (location != nullptr)
	{
		m_rigToWorldTransform = make_float4x4_from_quaternion(location.Orientation()) * make_float4x4_translation(location.Position());
	}
	else 
	{
		DebugPrint("Sensor failed to locate!");
	}
}

void RMCameraReader::SaveFrame(IResearchModeSensorFrame* pSensorFrame)
{
	IResearchModeSensorVLCFrame* pVLCFrame = nullptr;
	if (m_sensorType == SensorType::VisibleLightLeftLeft || m_sensorType == SensorType::VisibleLightLeftFront || m_sensorType == SensorType::VisibleLightRightFront || m_sensorType == SensorType::VisibleLightRightRight)
	{
		HRESULT hr = pSensorFrame->QueryInterface(IID_PPV_ARGS(&pVLCFrame));
		if (SUCCEEDED(hr))
		{
			SaveVLC(pSensorFrame, pVLCFrame);
			pVLCFrame->Release();
		}
		return;
	}

#ifdef ENABLE_DEPTH
	IResearchModeSensorDepthFrame* pDepthFrame = nullptr;
	if (m_sensorType == SensorType::LongThrow)
	{
		HRESULT hr = pSensorFrame->QueryInterface(IID_PPV_ARGS(&pDepthFrame));
		if (SUCCEEDED(hr))
		{
			SaveDepth(pSensorFrame, pDepthFrame);
			pDepthFrame->Release();
		}
		return;
	}
#endif

#ifdef ENABLE_IMU
	IResearchModeAccelFrame* pAccelFrame = nullptr;
	IResearchModeGyroFrame* pGyroFrame = nullptr;
	IResearchModeMagFrame* pMagFrame = nullptr;
	if (m_sensorType == SensorType::IMUAccel)
	{
		HRESULT hr = pSensorFrame->QueryInterface(IID_PPV_ARGS(&pAccelFrame));
		if (SUCCEEDED(hr))
		{
			SaveAccel(pAccelFrame);
			pAccelFrame->Release();
		}
		return;
	}

	if (m_sensorType == SensorType::IMUGyro)
	{
		HRESULT hr = pSensorFrame->QueryInterface(IID_PPV_ARGS(&pGyroFrame));
		if (SUCCEEDED(hr))
		{
			SaveGyro(pGyroFrame);
			pGyroFrame->Release();
		}
		return;
	}

	if (m_sensorType == SensorType::IMUMag)
	{
		HRESULT hr = pSensorFrame->QueryInterface(IID_PPV_ARGS(&pMagFrame));
		if (SUCCEEDED(hr))
		{
			SaveMag(pMagFrame);
			pMagFrame->Release();
		}
		return;
	}
#endif
}
