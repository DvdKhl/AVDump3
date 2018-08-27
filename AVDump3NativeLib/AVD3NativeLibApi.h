#pragma once

#include <string.h>
#include <malloc.h>
#include <stdint.h>
#include <nmmintrin.h>

#include "DLLDefines.h"

DLL_PUBLIC uint64_t RetrieveCPUInstructions();

DLL_PUBLIC void* CRC32CCreate();
DLL_PUBLIC void CRC32CInit(void* handle);
DLL_PUBLIC void CRC32CTransform(void* handle, uint8_t *b, int32_t length);
DLL_PUBLIC void CRC32CFinal(void* handle, uint8_t *b);

DLL_PUBLIC void* CRC32Create();
DLL_PUBLIC void CRC32Init(void* handle);
DLL_PUBLIC void CRC32Transform(void* handle, uint8_t *b, int32_t length);
DLL_PUBLIC void CRC32Final(void* handle, uint8_t *b);

DLL_PUBLIC void* TigerCreate();
DLL_PUBLIC void TigerInit(void* handle);
DLL_PUBLIC void TigerTransform(void* handle, uint8_t *b, int32_t length, uint8_t lastBlock);
DLL_PUBLIC void TigerFinal(void* handle, uint8_t *b);

DLL_PUBLIC void* SHA3Create();
DLL_PUBLIC void SHA3Init(void* handle);
DLL_PUBLIC void SHA3Transform(void* handle, uint8_t *b, int32_t length, uint8_t lastBlock);
DLL_PUBLIC void SHA3Final(void* handle, uint8_t *b);

DLL_PUBLIC void FreeHashObject(void* obj);