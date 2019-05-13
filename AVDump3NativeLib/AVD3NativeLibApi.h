#pragma once

#include <string.h>
#include <malloc.h>
#include <stdint.h>
#include <nmmintrin.h>

#include "DLLDefines.h"

DLL_PUBLIC uint64_t RetrieveCPUInstructions();

DLL_PUBLIC void* CRC32CCreate(uint32_t* blockSize);
DLL_PUBLIC void CRC32CInit(void* handle);
DLL_PUBLIC void CRC32CTransform(void* handle, uint8_t* b, int32_t length, uint8_t lastBlock);
DLL_PUBLIC void CRC32CFinal(void* handle, uint8_t* b);

DLL_PUBLIC void* CRC32Create(uint32_t* blockSize);
DLL_PUBLIC void CRC32Init(void* handle);
DLL_PUBLIC void CRC32Transform(void* handle, uint8_t* b, int32_t length, uint8_t lastBlock);
DLL_PUBLIC void CRC32Final(void* handle, uint8_t* b);

DLL_PUBLIC void* TigerCreate(uint32_t* blockSize);
DLL_PUBLIC void TigerInit(void* handle);
DLL_PUBLIC void TigerTransform(void* handle, uint8_t* b, int32_t length, uint8_t lastBlock);
DLL_PUBLIC void TigerFinal(void* handle, uint8_t* b);

DLL_PUBLIC void* SHA3Create(uint32_t* blockSize);
DLL_PUBLIC void SHA3Init(void* handle);
DLL_PUBLIC void SHA3Transform(void* handle, uint8_t* b, int32_t length, uint8_t lastBlock);
DLL_PUBLIC void SHA3Final(void* handle, uint8_t* b);

DLL_PUBLIC void* MD4Create(uint32_t* blockSize);
DLL_PUBLIC void MD4Init(void* handle);
DLL_PUBLIC void MD4Transform(void* handle, uint8_t* b, int32_t length, uint8_t lastBlock);
DLL_PUBLIC void MD4Final(void* handle, uint8_t* b);
DLL_PUBLIC void MD4ComputeHash(uint8_t* b, int32_t length, uint32_t* hash);

DLL_PUBLIC void FreeHashObject(void* obj);



typedef struct {
	void* fileHandle;
	uint8_t* baseAddress;
	uint32_t length;
} AVD3MirrorBufferCreateHandle;

//Functions my only return literal strings. The C# side won't free them!
DLL_PUBLIC char* CreateMirrorBuffer(uint32_t minLength, AVD3MirrorBufferCreateHandle* handle);
DLL_PUBLIC char* FreeMirrorBuffer(AVD3MirrorBufferCreateHandle* handle);