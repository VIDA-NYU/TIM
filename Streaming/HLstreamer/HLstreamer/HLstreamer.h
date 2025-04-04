#pragma once
#include "SensorManager.h"

class HLStreamer
{
public:
    virtual ~HLStreamer();

    void Initialize();
    void StartStreaming();
    void StopStreaming();
private:
    SensorManager m_sensorManager;
};