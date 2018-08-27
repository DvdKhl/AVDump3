// written and placed in the public domain by Wei Dai
//Taken from https://github.com/weidai11/cryptopp/commit/6200029faae23710fa88638269e9272664509b91
//(DvdKhl) Modified to make it standalone and stripped down to bare minimum

#include "AVD3NativeLibApi.h"

#ifndef CRYPTOPP_L1_CACHE_LINE_SIZE
// This should be a lower bound on the L1 cache line size. It's used for defense against timing attacks.
// Also see http://stackoverflow.com/questions/794632/programmatically-get-the-cache-line-size.
#if defined(_M_X64) || defined(__x86_64__) || (__arm64__) || (__aarch64__)
#define CRYPTOPP_L1_CACHE_LINE_SIZE 64
#else
// L1 cache line size is 32 on Pentium III and earlier
#define CRYPTOPP_L1_CACHE_LINE_SIZE 32
#endif
#endif

//#define GETBYTE(x, y) (unsigned int)byte((x)>>(8*(y)))
#define GETBYTE(x, y) (unsigned int)(((x)>>(8*(y)))&255)


#define CRYPTOPP_GENERATE_X64_MASM
#define CRYPTOPP_X86_ASM_AVAILABLE
#define CRYPTOPP_BOOL_X64 1
#define CRYPTOPP_BOOL_SSE2_ASM_AVAILABLE 1

static uint8_t TrySSE2()
{
#if CRYPTOPP_BOOL_X64
	return 1;
#elif defined(CRYPTOPP_MS_STYLE_INLINE_ASSEMBLY)
	__try
	{
#if CRYPTOPP_BOOL_SSE2_ASM_AVAILABLE
		AS2(por xmm0, xmm0)        // executing SSE2 instruction
#elif CRYPTOPP_BOOL_SSE2_INTRINSICS_AVAILABLE
		__m128i x = _mm_setzero_si128();
		return _mm_cvtsi128_si32(x) == 0;
#endif
	}
	// GetExceptionCode() == EXCEPTION_ILLEGAL_INSTRUCTION
	__except (EXCEPTION_EXECUTE_HANDLER)
	{
		return 0;
	}
	return 1;
#else
	// longjmp and clobber warnings. Volatile is required.
	// http://github.com/weidai11/cryptopp/issues/24 and http://stackoverflow.com/q/7721854
	volatile uint8_t result = 1;

	volatile SigHandler oldHandler = signal(SIGILL, SigIllHandlerSSE2);
	if (oldHandler == SIG_ERR)
		return 0;

# ifndef __MINGW32__
	volatile sigset_t oldMask;
	if (sigprocmask(0, NULL, (sigset_t*)&oldMask))
		return 0;
# endif

	if (setjmp(s_jmpNoSSE2))
		result = 0;
	else
	{
#if CRYPTOPP_uint8_t_SSE2_ASM_AVAILABLE
		__asm __volatile("por %xmm0, %xmm0");
#elif CRYPTOPP_uint8_t_SSE2_INTRINSICS_AVAILABLE
		__m128i x = _mm_setzero_si128();
		result = _mm_cvtsi128_si32(x) == 0;
#endif
	}

# ifndef __MINGW32__
	sigprocmask(SIG_SETMASK, (sigset_t*)&oldMask, NULL);
# endif

	signal(SIGILL, oldHandler);
	return result;
#endif
}


#if _MSC_VER >= 1400 && CRYPTOPP_BOOL_X64

uint8_t CpuId(uint32_t input, uint32_t output[4])
{
	__cpuid((int *)output, input);
	return 1;
}

#else

#ifndef CRYPTOPP_MS_STYLE_INLINE_ASSEMBLY
extern "C"
{
	static jmp_buf s_jmpNoCPUID;
	static void SigIllHandlerCPUID(int)
	{
		longjmp(s_jmpNoCPUID, 1);
	}

	static jmp_buf s_jmpNoSSE2;
	static void SigIllHandlerSSE2(int)
	{
		longjmp(s_jmpNoSSE2, 1);
	}
}
#endif

bool CpuId(word32 input, word32 output[4])
{
#if defined(CRYPTOPP_MS_STYLE_INLINE_ASSEMBLY)
	__try
	{
		__asm
		{
			mov eax, input
			mov ecx, 0
			cpuid
			mov edi, output
			mov[edi], eax
			mov[edi + 4], ebx
			mov[edi + 8], ecx
			mov[edi + 12], edx
		}
	}
	// GetExceptionCode() == EXCEPTION_ILLEGAL_INSTRUCTION
	__except (EXCEPTION_EXECUTE_HANDLER)
	{
		return false;
	}

	// function 0 returns the highest basic function understood in EAX
	if (input == 0)
		return !!output[0];

	return true;
#else
	// longjmp and clobber warnings. Volatile is required.
	// http://github.com/weidai11/cryptopp/issues/24 and http://stackoverflow.com/q/7721854
	volatile bool result = true;

	volatile SigHandler oldHandler = signal(SIGILL, SigIllHandlerCPUID);
	if (oldHandler == SIG_ERR)
		return false;

# ifndef __MINGW32__
	volatile sigset_t oldMask;
	if (sigprocmask(0, NULL, (sigset_t*)&oldMask))
		return false;
# endif

	if (setjmp(s_jmpNoCPUID))
		result = false;
	else
	{
		asm volatile
			(
				// save ebx in case -fPIC is being used
				// TODO: this might need an early clobber on EDI.
# if CRYPTOPP_BOOL_X32 || CRYPTOPP_BOOL_X64
				"pushq %%rbx; cpuid; mov %%ebx, %%edi; popq %%rbx"
# else
				"push %%ebx; cpuid; mov %%ebx, %%edi; pop %%ebx"
# endif
				: "=a" (output[0]), "=D" (output[1]), "=c" (output[2]), "=d" (output[3])
				: "a" (input), "c" (0)
				);
	}

# ifndef __MINGW32__
	sigprocmask(SIG_SETMASK, (sigset_t*)&oldMask, NULL);
# endif

	signal(SIGILL, oldHandler);
	return result;
#endif
}

#endif
uint8_t g_x86DetectionDone = 0;
uint8_t g_hasMMX = 0,  g_hasISSE = 0,  g_hasSSE2 = 0,  g_hasSSSE3 = 0;
uint8_t  g_hasSSE4 = 0,  g_hasAESNI = 0,  g_hasCLMUL = 0,  g_hasSHA = 0;
uint8_t  g_hasRDRAND = 0,  g_hasRDSEED = 0,  g_isP4 = 0;
uint8_t  g_hasPadlockRNG = 0,  g_hasPadlockACE = 0,  g_hasPadlockACE2 = 0;
uint8_t  g_hasPadlockPHE = 0,  g_hasPadlockPMM = 0;
uint32_t  g_cacheLineSize = CRYPTOPP_L1_CACHE_LINE_SIZE;

static inline uint8_t IsIntel(const uint32_t output[4])
{
	// This is the "GenuineIntel" string
	return (output[1] /*EBX*/ == 0x756e6547) &&
		(output[2] /*ECX*/ == 0x6c65746e) &&
		(output[3] /*EDX*/ == 0x49656e69);
}

static inline uint8_t IsAMD(const uint32_t output[4])
{
	// This is the "AuthenticAMD" string. Some early K5's can return "AMDisbetter!"
	return (output[1] /*EBX*/ == 0x68747541) &&
		(output[2] /*ECX*/ == 0x444D4163) &&
		(output[3] /*EDX*/ == 0x69746E65);
}

static inline uint8_t IsVIA(const uint32_t output[4])
{
	// This is the "CentaurHauls" string. Some non-PadLock's can return "VIA VIA VIA "
	return (output[1] /*EBX*/ == 0x746e6543) &&
		(output[2] /*ECX*/ == 0x736c7561) &&
		(output[3] /*EDX*/ == 0x48727561);
}

void detectX86Features()
{
	uint32_t cpuid[4], cpuid1[4];
	if (!CpuId(0, cpuid))
		return;
	if (!CpuId(1, cpuid1))
		return;

	g_hasMMX = (cpuid1[3] & (1 << 23)) != 0;
	if ((cpuid1[3] & (1 << 26)) != 0)
		g_hasSSE2 = TrySSE2();
	g_hasSSSE3 = g_hasSSE2 && (cpuid1[2] & (1 << 9));
	g_hasSSE4 = g_hasSSE2 && ((cpuid1[2] & (1 << 19)) && (cpuid1[2] & (1 << 20)));
	g_hasAESNI = g_hasSSE2 && (cpuid1[2] & (1 << 25));
	g_hasCLMUL = g_hasSSE2 && (cpuid1[2] & (1 << 1));

	if ((cpuid1[3] & (1 << 25)) != 0)
		g_hasISSE = 1;
	else
	{
		uint32_t cpuid2[4];
		CpuId(0x080000000, cpuid2);
		if (cpuid2[0] >= 0x080000001)
		{
			CpuId(0x080000001, cpuid2);
			g_hasISSE = (cpuid2[3] & (1 << 22)) != 0;
		}
	}

	if (IsIntel(cpuid))
	{
		static const unsigned int RDRAND_FLAG = (1 << 30);
		static const unsigned int RDSEED_FLAG = (1 << 18);
		static const unsigned int    SHA_FLAG = (1 << 29);

		g_isP4 = ((cpuid1[0] >> 8) & 0xf) == 0xf;
		g_cacheLineSize = 8 * GETBYTE(cpuid1[1], 1);
		g_hasRDRAND = !!(cpuid1[2] /*ECX*/ & RDRAND_FLAG);

		if (cpuid[0] /*EAX*/ >= 7)
		{
			uint32_t cpuid3[4];
			if (CpuId(7, cpuid3))
			{
				g_hasRDSEED = !!(cpuid3[1] /*EBX*/ & RDSEED_FLAG);
				g_hasSHA = !!(cpuid3[1] /*EBX*/ & SHA_FLAG);
			}
		}
	}
	else if (IsAMD(cpuid))
	{
		static const unsigned int RDRAND_FLAG = (1 << 30);

		CpuId(0x01, cpuid);
		g_hasRDRAND = !!(cpuid[2] /*ECX*/ & RDRAND_FLAG);

		CpuId(0x80000005, cpuid);
		g_cacheLineSize = GETBYTE(cpuid[2], 0);
	}
	else if (IsVIA(cpuid))
	{
		static const unsigned int  RNG_FLAGS = (0x3 << 2);
		static const unsigned int  ACE_FLAGS = (0x3 << 6);
		static const unsigned int ACE2_FLAGS = (0x3 << 8);
		static const unsigned int  PHE_FLAGS = (0x3 << 10);
		static const unsigned int  PMM_FLAGS = (0x3 << 12);

		CpuId(0xC0000000, cpuid);
		if (cpuid[0] >= 0xC0000001)
		{
			// Extended features available
			CpuId(0xC0000001, cpuid);
			g_hasPadlockRNG = !!(cpuid[3] /*EDX*/ & RNG_FLAGS);
			g_hasPadlockACE = !!(cpuid[3] /*EDX*/ & ACE_FLAGS);
			g_hasPadlockACE2 = !!(cpuid[3] /*EDX*/ & ACE2_FLAGS);
			g_hasPadlockPHE = !!(cpuid[3] /*EDX*/ & PHE_FLAGS);
			g_hasPadlockPMM = !!(cpuid[3] /*EDX*/ & PMM_FLAGS);
		}
	}

	if (!g_cacheLineSize)
		g_cacheLineSize = CRYPTOPP_L1_CACHE_LINE_SIZE;

	*((volatile uint8_t*)&g_x86DetectionDone) = 1;
}


uint64_t RetrieveCPUInstructions() {
	if (!g_x86DetectionDone) detectX86Features();
	return
		(g_hasMMX         ? 1 <<  0 : 0) |
		(g_hasISSE        ? 1 <<  1 : 0) |
		(g_hasSSE2        ? 1 <<  2 : 0) |
		(g_hasSSSE3       ? 1 <<  3 : 0) |
		(g_hasSSE4        ? 1 <<  4 : 0) |
		(g_hasSHA         ? 1 <<  5 : 0) |
		(g_hasRDRAND      ? 1 <<  6 : 0) |
		(g_hasRDSEED      ? 1 <<  7 : 0) |
		(g_hasPadlockRNG  ? 1 <<  8 : 0) |
		(g_hasPadlockACE  ? 1 <<  9 : 0) |
		(g_hasPadlockACE2 ? 1 << 10 : 0) |
		(g_hasPadlockPHE  ? 1 << 11 : 0) |
		(g_hasPadlockPMM  ? 1 << 12 : 0);
}