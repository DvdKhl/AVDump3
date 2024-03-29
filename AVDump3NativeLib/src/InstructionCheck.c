#include "AVD3NativeLibApi.h"
#include <stdbool.h>

#ifdef _WIN32
//  Windows
#define cpuid(info, x) __cpuidex(info, x, 0)

#elif defined(_M_X64) || defined(__x86_64__)
//  GCC Intrinsics
#include <cpuid.h>

void cpuid(int info[4], int InfoType) {
	__cpuid_count(InfoType, 0, info[0], info[1], info[2], info[3]);
}
#endif

#if defined(_M_X64) || defined(__x86_64__)

uint64_t RetrieveCPUInstructions() {
	//  Misc.
	bool HW_MMX = 0;
	bool HW_x64 = 0;
	bool HW_ABM = 0;      // Advanced Bit Manipulation
	bool HW_RDRAND = 0;
	bool HW_BMI1 = 0;
	bool HW_BMI2 = 0;
	bool HW_ADX = 0;
	bool HW_PREFETCHWT1 = 0;

	//  SIMD: 128-bit
	bool HW_SSE = 0;
	bool HW_SSE2 = 0;
	bool HW_SSE3 = 0;
	bool HW_SSSE3 = 0;
	bool HW_SSE41 = 0;
	bool HW_SSE42 = 0;
	bool HW_SSE4a = 0;
	bool HW_AES = 0;
	bool HW_SHA = 0;

	//  SIMD: 256-bit
	bool HW_AVX = 0;
	bool HW_XOP = 0;
	bool HW_FMA3 = 0;
	bool HW_FMA4 = 0;
	bool HW_AVX2 = 0;

	//  SIMD: 512-bit
	bool HW_AVX512F = 0;    //  AVX512 Foundation
	bool HW_AVX512CD = 0;   //  AVX512 Conflict Detection
	bool HW_AVX512PF = 0;   //  AVX512 Prefetch
	bool HW_AVX512ER = 0;   //  AVX512 Exponential + Reciprocal
	bool HW_AVX512VL = 0;   //  AVX512 Vector Length Extensions
	bool HW_AVX512BW = 0;   //  AVX512 Byte + Word
	bool HW_AVX512DQ = 0;   //  AVX512 Doubleword + Quadword
	bool HW_AVX512IFMA = 0; //  AVX512 Integer 52-bit Fused Multiply-Add
	bool HW_AVX512VBMI = 0; //  AVX512 Vector Byte Manipulation Instructions

	int info[4];
	cpuid(info, 0);
	int nIds = info[0];

	cpuid(info, 0x80000000);
	unsigned nExIds = info[0];

	//  Detect Features
	if (nIds >= 0x00000001) {
		cpuid(info, 0x00000001);
		HW_MMX = (info[3] & ((int)1 << 23)) != 0;
		HW_SSE = (info[3] & ((int)1 << 25)) != 0;
		HW_SSE2 = (info[3] & ((int)1 << 26)) != 0;
		HW_SSE3 = (info[2] & ((int)1 << 0)) != 0;

		HW_SSSE3 = (info[2] & ((int)1 << 9)) != 0;
		HW_SSE41 = (info[2] & ((int)1 << 19)) != 0;
		HW_SSE42 = (info[2] & ((int)1 << 20)) != 0;
		HW_AES = (info[2] & ((int)1 << 25)) != 0;

		HW_AVX = (info[2] & ((int)1 << 28)) != 0;
		HW_FMA3 = (info[2] & ((int)1 << 12)) != 0;

		HW_RDRAND = (info[2] & ((int)1 << 30)) != 0;
	}
	if (nIds >= 0x00000007) {
		cpuid(info, 0x00000007);
		HW_AVX2 = (info[1] & ((int)1 << 5)) != 0;

		HW_BMI1 = (info[1] & ((int)1 << 3)) != 0;
		HW_BMI2 = (info[1] & ((int)1 << 8)) != 0;
		HW_ADX = (info[1] & ((int)1 << 19)) != 0;
		HW_SHA = (info[1] & ((int)1 << 29)) != 0;
		HW_PREFETCHWT1 = (info[2] & ((int)1 << 0)) != 0;

		HW_AVX512F = (info[1] & ((int)1 << 16)) != 0;
		HW_AVX512DQ = (info[1] & ((int)1 << 17)) != 0;
		HW_AVX512PF = (info[1] & ((int)1 << 26)) != 0;
		HW_AVX512ER = (info[1] & ((int)1 << 27)) != 0;
		HW_AVX512CD = (info[1] & ((int)1 << 28)) != 0;
		HW_AVX512BW = (info[1] & ((int)1 << 30)) != 0;
		HW_AVX512VL = (info[1] & ((int)1 << 31)) != 0;
		HW_AVX512IFMA = (info[1] & ((int)1 << 21)) != 0;
		HW_AVX512VBMI = (info[2] & ((int)1 << 1)) != 0;
	}
	if (nExIds >= 0x80000001) {
		cpuid(info, 0x80000001);
		HW_x64 = (info[3] & ((int)1 << 29)) != 0;
		HW_ABM = (info[2] & ((int)1 << 5)) != 0;
		HW_SSE4a = (info[2] & ((int)1 << 6)) != 0;
		HW_FMA4 = (info[2] & ((int)1 << 16)) != 0;
		HW_XOP = (info[2] & ((int)1 << 11)) != 0;
	}

	return
		(HW_MMX ? 1 << 0 : 0) |
		(HW_x64 ? 1 << 1 : 0) |
		(HW_ABM ? 1 << 2 : 0) |
		(HW_RDRAND ? 1 << 3 : 0) |
		(HW_BMI1 ? 1 << 4 : 0) |
		(HW_BMI2 ? 1 << 5 : 0) |
		(HW_ADX ? 1 << 6 : 0) |
		(HW_PREFETCHWT1 ? 1 << 7 : 0) |
		(HW_SSE ? 1 << 8 : 0) |
		(HW_SSE2 ? 1 << 9 : 0) |
		(HW_SSE3 ? 1 << 10 : 0) |
		(HW_SSSE3 ? 1 << 11 : 0) |
		(HW_SSE41 ? 1 << 12 : 0) |
		(HW_SSE42 ? 1 << 13 : 0) |
		(HW_SSE4a ? 1 << 14 : 0) |
		(HW_AES ? 1 << 15 : 0) |
		(HW_SHA ? 1 << 16 : 0) |
		(HW_AVX ? 1 << 17 : 0) |
		(HW_XOP ? 1 << 18 : 0) |
		(HW_FMA3 ? 1 << 19 : 0) |
		(HW_FMA4 ? 1 << 20 : 0) |
		(HW_AVX2 ? 1 << 21 : 0) |
		(HW_AVX512F ? 1 << 22 : 0) |
		(HW_AVX512CD ? 1 << 23 : 0) |
		(HW_AVX512PF ? 1 << 24 : 0) |
		(HW_AVX512ER ? 1 << 25 : 0) |
		(HW_AVX512VL ? 1 << 26 : 0) |
		(HW_AVX512BW ? 1 << 27 : 0) |
		(HW_AVX512DQ ? 1 << 28 : 0) |
		(HW_AVX512IFMA ? 1 << 29 : 0) |
		(HW_AVX512VBMI ? 1 << 30 : 0);
}

#else

uint64_t RetrieveCPUInstructions() {
	return 0;
}

#endif
