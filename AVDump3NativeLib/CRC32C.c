#include "AVD3NativeLibApi.h"

void* CRC32CCreate() {
	uint8_t *b = (uint8_t*)malloc(4);
	CRC32CInit(b);
	return b;
}

void CRC32CInit(void * handle) {
	memset((uint8_t*)handle, 0xFF, 4);
}

void CRC32CTransform(void* handle, uint8_t *b, int32_t length) {
	uint64_t state64 = *(uint32_t*)handle;

	uint64_t* words = (uint64_t*)b;
	uint64_t* wordEnd = words + (length >> 3);
	while (words != wordEnd) {
		state64 = _mm_crc32_u64(state64, *(words++));
	}

	uint32_t state32 = (uint32_t)state64;
	b = (uint8_t*)wordEnd;
	uint8_t *bEnd = b + (length & 7);
	while (b != bEnd) {
		state32 = _mm_crc32_u8(state32, *(b++));
	}
	*(uint32_t*)handle = state32;
}

void CRC32CFinal(void* handle, uint8_t *b) {
	*((int32_t*)b) = *(int32_t*)handle ^ 0xFFFFFFFF;
}
