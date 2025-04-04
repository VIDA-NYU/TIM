#pragma once
#include "SensorManager.h"
#include "HLStreamer.g.h"

namespace winrt::HLStreamerUnityPlugin::implementation
{
    struct HLStreamer : HLStreamerT<HLStreamer>
    {
    public:
        HLStreamer() = default;

        void ReleaseAllSensors();

        void Initialize(winrt::Windows::Perception::Spatial::SpatialCoordinateSystem refCoord);
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
        SensorManager m_manager;
    };
}

namespace winrt::HLStreamerUnityPlugin::factory_implementation
{
    struct HLStreamer : HLStreamerT<HLStreamer, implementation::HLStreamer>
    {
    };
}
