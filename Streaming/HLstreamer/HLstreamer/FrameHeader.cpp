#include "FrameHeader.h"

#pragma warning(disable:4996)
#define STB_IMAGE_WRITE_IMPLEMENTATION
#include "stb_image_write.h"

const int JPG_QUALITY = 75;

int ImageData::SaveJpeg(int w, int h, int comp, const void* data) {
	size = 0;

	return stbi_write_jpg_to_func(
		[](void* context, void* data, int size) {
			auto imContext = (ImageData*)context;
			memcpy(imContext->buffer.get() + imContext->size, data, size);
			imContext->size += size;
		}, this, w, h, comp, data, JPG_QUALITY
	);
};
