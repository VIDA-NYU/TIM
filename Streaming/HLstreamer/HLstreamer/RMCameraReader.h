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

#include <thread>
#include "ResearchModeApi.h"
#include "TimeConverter.h"
#include "HttpClientConnection.h"
#include "FrameHeader.h"
#include <winrt/Windows.Perception.Spatial.h>
#include <winrt/Windows.Perception.Spatial.Preview.h>

#define ENABLE_IMU
#define ENABLE_DEPTH

class RMCameraReader
{
public:
	RMCameraReader(IResearchModeSensor* pLLSensor, HANDLE camConsentGiven, ResearchModeSensorConsent* camAccessConsent, SensorType type, const GUID& guid, const winrt::Windows::Perception::Spatial::SpatialCoordinateSystem worldCoordSystem):
		m_sensorType(type)
	{
		m_pRMSensor = pLLSensor;
		m_pRMSensor->AddRef();
		m_pSensorFrame = nullptr;

		m_pCameraUpdateThread = new std::thread(CameraUpdateThread, this, camConsentGiven, camAccessConsent);
		m_httpconnection.ConnectTo(server + streamNames[(size_t)type]);

		if (m_sensorType != SensorType::IMUAccel && m_sensorType != SensorType::IMUGyro && m_sensorType != SensorType::IMUMag)
		{
			m_locator = winrt::Windows::Perception::Spatial::Preview::SpatialGraphInteropPreview::CreateLocatorForNode(guid);
			m_worldCoordSystem = worldCoordSystem;
			imData.buffer = std::make_unique<uint8_t[]>(256 * 1024);
			if (m_sensorType == SensorType::LongThrow) buffer = std::make_unique<uint8_t[]>(128 * 1024);
		}
	}

	virtual ~RMCameraReader()
	{
		m_fExit = true;
		m_pCameraUpdateThread->join();

		if (m_pRMSensor)
		{
			m_pRMSensor->CloseStream();
			m_pRMSensor->Release();
		}
	}	

	void StartStreaming();
	void StopStreaming();

protected:
	// Thread for retrieving frames
	static void CameraUpdateThread(RMCameraReader* pReader, HANDLE camConsentGiven, ResearchModeSensorConsent* camAccessConsent);

	bool IsNewTimestamp(IResearchModeSensorFrame* pSensorFrame);

	void SaveFrame(IResearchModeSensorFrame* pSensorFrame);
	void SaveVLC(IResearchModeSensorFrame* pSensorFrame, IResearchModeSensorVLCFrame* pVLCFrame);
#ifdef ENABLE_DEPTH
	void SaveDepth(IResearchModeSensorFrame* pSensorFrame, IResearchModeSensorDepthFrame* pDepthFrame);
#endif
#ifdef ENABLE_IMU
	void SaveAccel(IResearchModeAccelFrame* pAccelFrame);
	void SaveGyro(IResearchModeGyroFrame* pGyroFrame);
	void SaveMag(IResearchModeMagFrame* pMagFrame);
#endif
	winrt::Windows::Foundation::IAsyncAction DumpCalibration();
	void getRigToWorld();
	winrt::Windows::Foundation::Numerics::float4x4 m_rigToWorldTransform = winrt::Windows::Foundation::Numerics::float4x4::identity();

	// Mutex to access sensor frame
	IResearchModeSensor* m_pRMSensor = nullptr;
	IResearchModeSensorFrame* m_pSensorFrame = nullptr;

	bool m_fExit = false;
	std::thread* m_pCameraUpdateThread;
	
	// variable to enable / disable writing
	bool m_fStreaming = false;

	TimeConverter m_converter;
	UINT64 m_prevTimestamp = 0;	

	HttpClientConnection m_httpconnection;
	SensorType m_sensorType;

	winrt::Windows::Perception::Spatial::SpatialLocator m_locator = nullptr;
	winrt::Windows::Perception::Spatial::SpatialCoordinateSystem m_worldCoordSystem = nullptr;

	ImageData imData;
	std::unique_ptr<uint8_t[]> buffer;
};
