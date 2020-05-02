using System;

namespace AVDump3Lib.Processing {
	[Flags]
	public enum CPUInstructions : long {
		MMX = 1 << 0,
		x64 = 1 << 1,
		ABM = 1 << 2,
		RDRAND = 1 << 3,
		BMI1 = 1 << 4,
		BMI2 = 1 << 5,
		ADX = 1 << 6,
		PREFETCHWT1 = 1 << 7,
		SSE = 1 << 8,
		SSE2 = 1 << 9,
		SSE3 = 1 << 10,
		SSSE3 = 1 << 11,
		SSE41 = 1 << 12,
		SSE42 = 1 << 13,
		SSE4a = 1 << 14,
		AES = 1 << 15,
		SHA = 1 << 16,
		AVX = 1 << 17,
		XOP = 1 << 18,
		FMA3 = 1 << 19,
		FMA4 = 1 << 20,
		AVX2 = 1 << 21,
		AVX512F = 1 << 22,
		AVX512CD = 1 << 23,
		AVX512PF = 1 << 24,
		AVX512ER = 1 << 25,
		AVX512VL = 1 << 26,
		AVX512BW = 1 << 27,
		AVX512DQ = 1 << 28,
		AVX512IFMA = 1 << 29,
		AVX512VBMI = 1 << 30
	}
}
