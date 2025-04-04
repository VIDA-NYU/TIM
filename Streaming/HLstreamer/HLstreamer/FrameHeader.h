#pragma once
#include <stdint.h>
#include <array>
#include <winrt/Windows.Foundation.h>
#include <memory>

enum class SensorType :uint8_t {
	PhotoVideo = 0,
	LongThrow,
	DEPRECATED_AHAT,
	VisibleLightLeftLeft,
	VisibleLightLeftFront,
	VisibleLightRightFront,
	VisibleLightRightRight,
	IMUAccel,
	IMUGyro,
	IMUMag,
	NumberOfSensorTypes,
	Calibration
};

struct FrameHeader {
	uint8_t VersionMajor = 0x02;
	SensorType FrameType;
	uint64_t Timestamp;
	uint32_t ImageWidth;
	uint32_t ImageHeight;
	uint32_t PixelStride;
	uint32_t ExtraInfoSize = 0;
};

const std::array<winrt::hstring, (size_t)SensorType::NumberOfSensorTypes> streamNames = { L"main", L"depthlt", L"depthahat", L"gll", L"glf", L"grf", L"grr", L"imuaccel", L"imugyro", L"imumag"};

const winrt::hstring server = L"http://<REPLACE_WITH_SERVER_ADDRESS>:7890/data/";

struct ImageData
{
	std::unique_ptr<uint8_t[]> buffer;
	uint32_t size;

	int SaveJpeg(int w, int h, int comp, const void* data);
};
