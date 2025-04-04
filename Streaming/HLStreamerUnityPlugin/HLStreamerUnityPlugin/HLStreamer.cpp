#include "pch.h"
#include "HLStreamer.h"
#include "HLStreamer.g.cpp"
#include "utils.hpp"

namespace winrt::HLStreamerUnityPlugin::implementation
{
	void HLStreamer::ReleaseAllSensors()
	{
		m_manager.ReleaseAllSensors();
	}

	void HLStreamer::Initialize(winrt::Windows::Perception::Spatial::SpatialCoordinateSystem refCoord)
	{
		m_manager.Initialize(refCoord);
	}

	void HLStreamer::StartStreaming()
	{
		m_manager.StartStreaming();
	}

	void HLStreamer::StopStreaming()
	{
		m_manager.StopStreaming();
	}

	void HLStreamer::ActivateAll()
	{
		m_manager.ActivateAll();
	}

	void HLStreamer::ActivateMain()
	{
		m_manager.ActivateMain();
	}

	void HLStreamer::ActivateLL()
	{
		m_manager.ActivateLL();
	}

	void HLStreamer::ActivateLF()
	{
		m_manager.ActivateLF();
	}

	void HLStreamer::ActivateRF()
	{
		m_manager.ActivateRF();
	}

	void HLStreamer::ActivateRR()
	{
		m_manager.ActivateRR();
	}

	void HLStreamer::ActivateDepth()
	{
		m_manager.ActivateDepth();
	}

	void HLStreamer::ActivateIMUAccel()
	{
		m_manager.ActivateIMUAccel();
	}

	void HLStreamer::ActivateIMUGyro()
	{
		m_manager.ActivateIMUGyro();
	}

	void HLStreamer::ActivateIMUMag()
	{
		m_manager.ActivateIMUMag();
	}

}
