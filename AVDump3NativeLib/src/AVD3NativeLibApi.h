#pragma once

#include <string.h>
#include <stdint.h>
#include <stdlib.h>

#if defined(_M_X64) || defined(__x86_64__)
#include <nmmintrin.h>
#include <immintrin.h>
#endif

#include "DLLDefines.h"

DLL_PUBLIC uint64_t RetrieveCPUInstructions();

DLL_PUBLIC void* CRC32CCreate(uint32_t* hashLength, uint32_t* blockLength);
DLL_PUBLIC void CRC32CInit(void* handle);
DLL_PUBLIC void CRC32CTransform(void* handle, uint8_t* b, int32_t length);
DLL_PUBLIC void CRC32CFinal(void* handle, uint8_t* b, int32_t length, uint8_t* hash);

DLL_PUBLIC void* CRC32Create(uint32_t* hashLength, uint32_t* blockLength);
DLL_PUBLIC void CRC32Init(void* handle);
DLL_PUBLIC void CRC32Transform(void* handle, uint8_t* b, int32_t length);
DLL_PUBLIC void CRC32Final(void* handle, uint8_t* b, int32_t length, uint8_t* hash);

DLL_PUBLIC void* TigerCreate(uint32_t* hashLength, uint32_t* blockLength);
DLL_PUBLIC void TigerInit(void* handle);
DLL_PUBLIC void TigerTransform(void* handle, uint8_t* b, int32_t length);
DLL_PUBLIC void TigerFinal(void* handle, uint8_t* b, int32_t length, uint8_t* hash);

DLL_PUBLIC void TTHNodeHash(uint8_t* data, uint8_t* buffer, uint8_t* hash);
DLL_PUBLIC void TTHBlockHash(uint8_t* data, uint8_t* buffer, uint8_t* hash);
DLL_PUBLIC void TTHPartialBlockHash(uint8_t * data, uint32_t length, uint8_t * buffer, uint8_t * hash);
DLL_PUBLIC uint8_t* TTHCreateBlock();
DLL_PUBLIC uint8_t* TTHCreateNode();

DLL_PUBLIC void* SHA1Create(uint32_t* hashLength, uint32_t* blockLength);
DLL_PUBLIC void SHA1Init(void* handle);
DLL_PUBLIC void SHA1Transform(void* handle, uint8_t* b, int32_t length);
DLL_PUBLIC void SHA1Final(void* handle, uint8_t* b, int32_t length, uint8_t* hash);

DLL_PUBLIC void* SHA256Create(uint32_t* hashLength, uint32_t* blockLength);
DLL_PUBLIC void SHA256Init(void* handle);
DLL_PUBLIC void SHA256Transform(void* handle, uint8_t* b, int32_t length);
DLL_PUBLIC void SHA256Final(void* handle, uint8_t* b, int32_t length, uint8_t* hash);

DLL_PUBLIC void* SHA3Create(uint32_t* hashLength, uint32_t* blockLength);
DLL_PUBLIC void SHA3Init(void* handle);
DLL_PUBLIC void SHA3Transform(void* handle, uint8_t* b, int32_t length);
DLL_PUBLIC void SHA3Final(void* handle, uint8_t* b, int32_t length, uint8_t* hash);

DLL_PUBLIC void* KeccakCreate(uint32_t* hashLength, uint32_t* blockLength);
DLL_PUBLIC void KeccakInit(void* handle);
DLL_PUBLIC void KeccakTransform(void* handle, uint8_t* b, int32_t length);
DLL_PUBLIC void KeccakFinal(void* handle, uint8_t* b, int32_t length, uint8_t* hash);

DLL_PUBLIC void* XXHashCreate(uint32_t* hashLength, uint32_t* blockLength);
DLL_PUBLIC void XXHashInit(void* handle);
DLL_PUBLIC void XXHashTransform(void* handle, uint8_t* b, int32_t length);
DLL_PUBLIC void XXHashFinal(void* handle, uint8_t* b, int32_t length, uint8_t* hash);

DLL_PUBLIC void* MD4Create(uint32_t* hashLength, uint32_t* blockLength);
DLL_PUBLIC void MD4Init(void* handle);
DLL_PUBLIC void MD4Transform(void* handle, uint8_t* b, int32_t length);
DLL_PUBLIC void MD4Final(void* handle, uint8_t* b, int32_t length, uint8_t* hash);
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


//https://github.com/google/cityhash/blob/8af9b8c2b889d80c22d6bc26ba0df1afb79a30db/src/city.cc#L50
#ifdef _MSC_VER

#define bswap_32(x) _byteswap_ulong(x)
#define bswap_64(x) _byteswap_uint64(x)

#elif defined(__APPLE__)

// Mac OS X / Darwin features
#include <libkern/OSByteOrder.h>
#define bswap_32(x) OSSwapInt32(x)
#define bswap_64(x) OSSwapInt64(x)

#elif defined(__sun) || defined(sun)

#include <sys/byteorder.h>
#define bswap_32(x) BSWAP_32(x)
#define bswap_64(x) BSWAP_64(x)

#elif defined(__FreeBSD__)

#include <sys/endian.h>
#define bswap_32(x) bswap32(x)
#define bswap_64(x) bswap64(x)

#elif defined(__OpenBSD__)

#include <sys/types.h>
#define bswap_32(x) swap32(x)
#define bswap_64(x) swap64(x)

#elif defined(__NetBSD__)

#include <sys/types.h>
#include <machine/bswap.h>
#if defined(__BSWAP_RENAME) && !defined(__bswap_32)
#define bswap_32(x) bswap32(x)
#define bswap_64(x) bswap64(x)
#endif

#else

#include <byteswap.h>

#endif
