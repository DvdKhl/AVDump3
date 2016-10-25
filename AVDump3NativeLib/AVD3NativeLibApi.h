#pragma once

#include <string.h>
#include <malloc.h>
#include <stdint.h>
#include <nmmintrin.h>

__declspec(dllexport) void* CRC32CCreate();
__declspec(dllexport) void CRC32CInit(void* handle);
__declspec(dllexport) void CRC32CTransform(void* handle, uint8_t *b, int32_t length);
__declspec(dllexport) void CRC32CFinal(void* handle, uint8_t *b);

__declspec(dllexport) void* CRC32Create();
__declspec(dllexport) void CRC32Init(void* handle);
__declspec(dllexport) void CRC32Transform(void* handle, uint8_t *b, int32_t length);
__declspec(dllexport) void CRC32Final(void* handle, uint8_t *b);

__declspec(dllexport) void* TigerCreate();
__declspec(dllexport) void TigerInit(void* handle);
__declspec(dllexport) void TigerTransform(void* handle, uint8_t *b, int32_t length);
__declspec(dllexport) void TigerFinal(void* handle, uint8_t *b);

__declspec(dllexport) void FreeHashObject(void* obj);