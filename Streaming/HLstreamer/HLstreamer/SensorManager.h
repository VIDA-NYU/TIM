#pragma once
#include "VideoFrameProcessor.h"
#include "RMCameraReader.h"
#include <winrt/Windows.Perception.Spatial.Preview.h>

class SensorManager
{
public:
    void ReleaseAllSensors();

    void Initialize();
    void StartStreaming();
    void StopStreaming();

    void ActivateAll();
    void ActivateMain();
    void ActivateLL();
    void ActivateLF();
    void ActivateRF();
    void ActivateRR();
    void ActivateDepth();
    void ActivateIMUAccel();
    void ActivateIMUGyro();
    void ActivateIMUMag();
private:
    winrt::Windows::Foundation::IAsyncAction InitializeVideoFrameProcessorAsync();
    void InitializeSensors();
    void InitializeSensorReaders();
    void GetRigNodeId(GUID& outGuid) const;

    static void CamAccessOnComplete(ResearchModeSensorConsent consent);
    static void ImuAccessOnComplete(ResearchModeSensorConsent consent);

    void CreateStationaryFrameOfReference();

    std::unique_ptr<VideoFrameProcessor> m_videoFrameProcessor = nullptr;
    winrt::Windows::Foundation::IAsyncAction m_videoFrameProcessorOperation = nullptr;

    IResearchModeSensor* m_pLFCameraSensor = nullptr;
    IResearchModeSensor* m_pRFCameraSensor = nullptr;
    IResearchModeSensor* m_pLLCameraSensor = nullptr;
    IResearchModeSensor* m_pRRCameraSensor = nullptr;
    IResearchModeSensor* m_pLTSensor = nullptr;
    IResearchModeSensor* m_pAccelSensor = nullptr;
    IResearchModeSensor* m_pGyroSensor = nullptr;
    IResearchModeSensor* m_pMagSensor = nullptr;
    std::vector<std::shared_ptr<RMCameraReader>> m_cameraReaders;
    IResearchModeSensorDevice* m_pSensorDevice = nullptr;
    IResearchModeSensorDeviceConsent* m_pSensorDeviceConsent = nullptr;
    std::vector<ResearchModeSensorDescriptor> m_sensorDescriptors;

    winrt::Windows::Perception::Spatial::SpatialCoordinateSystem m_worldCoordSystem = nullptr;

    bool m_bMainActivated = false;
    bool m_bLLActivated = false;
    bool m_bLFActivated = false;
    bool m_bRFActivated = false;
    bool m_bRRActivated = false;
    bool m_bDepthActivated = false;
    bool m_bIMUAccelActivated = false;
    bool m_bIMUGyroActivated = false;
    bool m_bIMUMagActivated = false;
};
