#include "HLstreamer.h"

HLStreamer::~HLStreamer()
{
	m_sensorManager.ReleaseAllSensors();
}

void HLStreamer::Initialize()
{
	m_sensorManager.ActivateAll();
	m_sensorManager.Initialize();
}

void HLStreamer::StartStreaming()
{
	m_sensorManager.StartStreaming();
}

void HLStreamer::StopStreaming()
{
	m_sensorManager.StopStreaming();
}
