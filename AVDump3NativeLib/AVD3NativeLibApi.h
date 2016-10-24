#pragma once

#include <string.h>
#include <malloc.h>
#include <stdint.h>
#include <nmmintrin.h>

__declspec(dllexport) void* CRC32CCreate();
__declspec(dllexport) void CRC32CTransform(void* handle, uint8_t *b, int32_t length);
__declspec(dllexport) void CRC32CFinal(void* handle, uint8_t *b);

__declspec(dllexport) void FreeHashObject(void* obj);