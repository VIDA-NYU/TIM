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

#pragma once

#include <winrt/Windows.Media.Devices.Core.h>
#include <winrt/Windows.Media.Capture.Frames.h>
#include <winrt/Windows.Graphics.Imaging.h>
#include <winrt/Windows.Perception.Spatial.h>
#include "TimeConverter.h"
#include "HttpClientConnection.h"
#include "FrameHeader.h"

class VideoFrameProcessor
{
public:
    VideoFrameProcessor()
    {
        m_httpconnection.ConnectTo(server + streamNames[(size_t)SensorType::PhotoVideo]);
        imData.buffer = std::make_unique<uint8_t[]>(256 * 1024);
    }

    void StartStreaming();
    void StopStreaming();
    winrt::Windows::Foundation::IAsyncAction InitializeAsync();

    winrt::Windows::Perception::Spatial::SpatialCoordinateSystem m_worldCoordSystem = nullptr;

protected:
    void OnFrameArrived(const winrt::Windows::Media::Capture::Frames::MediaFrameReader& sender,        
                        const winrt::Windows::Media::Capture::Frames::MediaFrameArrivedEventArgs& args);

    void OnExclusiveControlStatusChanged(const winrt::Windows::Media::Capture::MediaCapture& mediaCapture,
        const winrt::Windows::Media::Capture::MediaCaptureDeviceExclusiveControlStatusChangedEventArgs& args);
    void OnCameraStreamStateChanged(const winrt::Windows::Media::Capture::MediaCapture& mediaCapture,
        const winrt::Windows::Foundation::IInspectable& args);
    void OnFailed(const winrt::Windows::Media::Capture::MediaCapture& mediaCapture,
        const winrt::Windows::Media::Capture::MediaCaptureFailedEventArgs& args);

private:
    void DumpFrame(const winrt::Windows::Graphics::Imaging::SoftwareBitmap& softwareBitmap, long long timestamp);

    winrt::Windows::Media::Capture::Frames::MediaFrameReader m_mediaFrameReader = nullptr;
    winrt::event_token m_OnFrameArrivedRegistration;
    winrt::event_token m_OnExclusiveControlStatusChangedRegistration;
    winrt::event_token m_OnCameraStreamStateChangedRegistration;
    winrt::event_token m_OnFailedRegistration;

    long long m_latestTimestamp = 0;
    winrt::Windows::Media::Capture::Frames::MediaFrameReference m_latestFrame = nullptr;

    TimeConverter m_converter;
    winrt::Windows::Foundation::Numerics::float4x4 m_PVToWorldTransform = winrt::Windows::Foundation::Numerics::float4x4::identity();

    bool m_fStreaming = false;

    HttpClientConnection m_httpconnection;
    ImageData imData;
};
