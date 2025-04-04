#include "SensorManager.h"
#include "utils.hpp"

extern "C"
HMODULE LoadLibraryA(
	LPCSTR lpLibFileName
);

static ResearchModeSensorConsent camAccessCheck;
static HANDLE camConsentGiven;
static ResearchModeSensorConsent imuAccessCheck;
static HANDLE imuConsentGiven;

void SensorManager::ReleaseAllSensors()
{
	m_cameraReaders.clear();

	if (m_pLFCameraSensor)
	{
		m_pLFCameraSensor->Release();
	}
	if (m_pRFCameraSensor)
	{
		m_pRFCameraSensor->Release();
	}
	if (m_pLLCameraSensor)
	{
		m_pLLCameraSensor->Release();
	}
	if (m_pRRCameraSensor)
	{
		m_pRRCameraSensor->Release();
	}
	if (m_pLTSensor)
	{
		m_pLTSensor->Release();
	}
	if (m_pAccelSensor)
	{
		m_pAccelSensor->Release();
	}
	if (m_pGyroSensor)
	{
		m_pGyroSensor->Release();
	}
	if (m_pMagSensor)
	{
		m_pMagSensor->Release();
	}

	if (m_pSensorDevice)
	{
		m_pSensorDevice->EnableEyeSelection();
		m_pSensorDevice->Release();
	}

	if (m_pSensorDeviceConsent)
	{
		m_pSensorDeviceConsent->Release();
	}

	DebugPrint("Sensors Released!");
}

void SensorManager::Initialize(winrt::Windows::Perception::Spatial::SpatialCoordinateSystem ref)
{
	m_worldCoordSystem = ref;

	m_videoFrameProcessorOperation = InitializeVideoFrameProcessorAsync();

	InitializeSensors();
	InitializeSensorReaders();
}

void SensorManager::StartStreaming()
{
	if (m_videoFrameProcessor) {
		m_videoFrameProcessor->StartStreaming();
	}

	for (auto& i : m_cameraReaders) {
		i->StartStreaming();
	}
}

void SensorManager::StopStreaming()
{
	if (m_videoFrameProcessor) {
		m_videoFrameProcessor->StopStreaming();
	}

	for (auto& i : m_cameraReaders) {
		i->StopStreaming();
	}
}

void SensorManager::ActivateAll()
{
	m_bMainActivated = true;
	m_bLLActivated = true;
	m_bLFActivated = true;
	m_bRFActivated = true;
	m_bRRActivated = true;
	m_bDepthActivated = true;
	m_bIMUAccelActivated = true;
	m_bIMUGyroActivated = true;
	m_bIMUMagActivated = true;
}

void SensorManager::ActivateMain()
{
	m_bMainActivated = true;
}

void SensorManager::ActivateLL()
{
	m_bLLActivated = true;
}

void SensorManager::ActivateLF()
{
	m_bLFActivated = true;
}

void SensorManager::ActivateRF()
{
	m_bRFActivated = true;
}

void SensorManager::ActivateRR()
{
	m_bRRActivated = true;
}

void SensorManager::ActivateDepth()
{
	m_bDepthActivated = true;
}

void SensorManager::ActivateIMUAccel()
{
	m_bIMUAccelActivated = true;
}

void SensorManager::ActivateIMUGyro()
{
	m_bIMUGyroActivated = true;
}

void SensorManager::ActivateIMUMag()
{
	m_bIMUMagActivated = true;
}

winrt::Windows::Foundation::IAsyncAction SensorManager::InitializeVideoFrameProcessorAsync()
{
	if (!m_bMainActivated) return;
	if (m_videoFrameProcessorOperation &&
		m_videoFrameProcessorOperation.Status() == winrt::Windows::Foundation::AsyncStatus::Completed)
	{
		return;
	}

	m_videoFrameProcessor = std::make_unique<VideoFrameProcessor>();
	if (!m_videoFrameProcessor.get())
	{
		throw winrt::hresult(E_POINTER);
	}
	m_videoFrameProcessor->m_worldCoordSystem = m_worldCoordSystem;

	co_await m_videoFrameProcessor->InitializeAsync();
}

void SensorManager::InitializeSensors()
{
	size_t sensorCount = 0;
	camConsentGiven = CreateEvent(nullptr, true, false, nullptr);
	imuConsentGiven = CreateEvent(nullptr, true, false, nullptr);

	// Load Research Mode library
	HMODULE hrResearchMode = LoadLibraryA("ResearchModeAPI");
	if (hrResearchMode)
	{
		typedef HRESULT(__cdecl* PFN_CREATEPROVIDER) (IResearchModeSensorDevice** ppSensorDevice);
		PFN_CREATEPROVIDER pfnCreate = reinterpret_cast<PFN_CREATEPROVIDER>(GetProcAddress(hrResearchMode, "CreateResearchModeSensorDevice"));
		if (pfnCreate)
		{
			winrt::check_hresult(pfnCreate(&m_pSensorDevice));
		}
	}

	// Manage Sensor Consent
	winrt::check_hresult(m_pSensorDevice->QueryInterface(IID_PPV_ARGS(&m_pSensorDeviceConsent)));
	winrt::check_hresult(m_pSensorDeviceConsent->RequestCamAccessAsync(SensorManager::CamAccessOnComplete));
#ifdef ENABLE_IMU
	winrt::check_hresult(m_pSensorDeviceConsent->RequestIMUAccessAsync(SensorManager::ImuAccessOnComplete));
#endif

	m_pSensorDevice->DisableEyeSelection();

	m_pSensorDevice->GetSensorCount(&sensorCount);
	m_sensorDescriptors.resize(sensorCount);

	m_pSensorDevice->GetSensorDescriptors(m_sensorDescriptors.data(), m_sensorDescriptors.size(), &sensorCount);

	for (auto& sensorDescriptor : m_sensorDescriptors)
	{
		if (m_bLFActivated && sensorDescriptor.sensorType == LEFT_FRONT)
		{
			winrt::check_hresult(m_pSensorDevice->GetSensor(sensorDescriptor.sensorType, &m_pLFCameraSensor));
		}

		if (m_bRFActivated && sensorDescriptor.sensorType == RIGHT_FRONT)
		{
			winrt::check_hresult(m_pSensorDevice->GetSensor(sensorDescriptor.sensorType, &m_pRFCameraSensor));
		}

		if (m_bLLActivated && sensorDescriptor.sensorType == LEFT_LEFT)
		{
			winrt::check_hresult(m_pSensorDevice->GetSensor(sensorDescriptor.sensorType, &m_pLLCameraSensor));
		}

		if (m_bRRActivated && sensorDescriptor.sensorType == RIGHT_RIGHT)
		{
			winrt::check_hresult(m_pSensorDevice->GetSensor(sensorDescriptor.sensorType, &m_pRRCameraSensor));
		}

#ifdef ENABLE_DEPTH
		if (m_bDepthActivated && sensorDescriptor.sensorType == DEPTH_LONG_THROW)
		{

			winrt::check_hresult(m_pSensorDevice->GetSensor(sensorDescriptor.sensorType, &m_pLTSensor));
		}
#endif

#ifdef ENABLE_IMU
		if (m_bIMUAccelActivated && sensorDescriptor.sensorType == IMU_ACCEL)
		{
			winrt::check_hresult(m_pSensorDevice->GetSensor(sensorDescriptor.sensorType, &m_pAccelSensor));
		}

		if (m_bIMUGyroActivated && sensorDescriptor.sensorType == IMU_GYRO)
		{
			winrt::check_hresult(m_pSensorDevice->GetSensor(sensorDescriptor.sensorType, &m_pGyroSensor));
		}

		if (m_bIMUMagActivated && sensorDescriptor.sensorType == IMU_MAG)
		{
			winrt::check_hresult(m_pSensorDevice->GetSensor(sensorDescriptor.sensorType, &m_pMagSensor));
		}
#endif
	}
}

void SensorManager::GetRigNodeId(GUID& outGuid) const
{
	IResearchModeSensorDevicePerception* pSensorDevicePerception;
	winrt::check_hresult(m_pSensorDevice->QueryInterface(IID_PPV_ARGS(&pSensorDevicePerception)));
	winrt::check_hresult(pSensorDevicePerception->GetRigNodeId(&outGuid));
	pSensorDevicePerception->Release();
}

void SensorManager::InitializeSensorReaders()
{
	// Get RigNode id which will be used to initialize
	// the spatial locators for camera readers objects
	GUID guid;
	GetRigNodeId(guid);

	if (m_pLFCameraSensor)
	{
		auto cameraReader = std::make_shared<RMCameraReader>(m_pLFCameraSensor, camConsentGiven, &camAccessCheck, SensorType::VisibleLightLeftFront, guid, m_worldCoordSystem);
		m_cameraReaders.push_back(cameraReader);
	}

	if (m_pRFCameraSensor)
	{
		auto cameraReader = std::make_shared<RMCameraReader>(m_pRFCameraSensor, camConsentGiven, &camAccessCheck, SensorType::VisibleLightRightFront, guid, m_worldCoordSystem);
		m_cameraReaders.push_back(cameraReader);
	}

	if (m_pLLCameraSensor)
	{
		auto cameraReader = std::make_shared<RMCameraReader>(m_pLLCameraSensor, camConsentGiven, &camAccessCheck, SensorType::VisibleLightLeftLeft, guid, m_worldCoordSystem);
		m_cameraReaders.push_back(cameraReader);
	}

	if (m_pRRCameraSensor)
	{
		auto cameraReader = std::make_shared<RMCameraReader>(m_pRRCameraSensor, camConsentGiven, &camAccessCheck, SensorType::VisibleLightRightRight, guid, m_worldCoordSystem);
		m_cameraReaders.push_back(cameraReader);
	}

	if (m_pLTSensor)
	{
		auto cameraReader = std::make_shared<RMCameraReader>(m_pLTSensor, camConsentGiven, &camAccessCheck, SensorType::LongThrow, guid, m_worldCoordSystem);
		m_cameraReaders.push_back(cameraReader);
	}

	if (m_pAccelSensor)
	{
		auto cameraReader = std::make_shared<RMCameraReader>(m_pAccelSensor, imuConsentGiven, &imuAccessCheck, SensorType::IMUAccel, guid, m_worldCoordSystem);
		m_cameraReaders.push_back(cameraReader);
	}

	if (m_pGyroSensor)
	{
		auto cameraReader = std::make_shared<RMCameraReader>(m_pGyroSensor, imuConsentGiven, &imuAccessCheck, SensorType::IMUGyro, guid, m_worldCoordSystem);
		m_cameraReaders.push_back(cameraReader);
	}

	if (m_pMagSensor)
	{
		auto cameraReader = std::make_shared<RMCameraReader>(m_pMagSensor, imuConsentGiven, &imuAccessCheck, SensorType::IMUMag, guid, m_worldCoordSystem);
		m_cameraReaders.push_back(cameraReader);
	}
}

void SensorManager::CamAccessOnComplete(ResearchModeSensorConsent consent)
{
	camAccessCheck = consent;
	SetEvent(camConsentGiven);
}

void SensorManager::ImuAccessOnComplete(ResearchModeSensorConsent consent)
{
	imuAccessCheck = consent;
	SetEvent(imuConsentGiven);
}

