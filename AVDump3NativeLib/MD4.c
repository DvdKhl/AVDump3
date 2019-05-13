// md4.cpp - modified by Wei Dai from Andrew M. Kuchling's md4.c
// The original code and all modifications are in the public domain.

// This is the original introductory comment:

/*
 *  md4.c : MD4 hash algorithm.
 *
 * Part of the Python Cryptography Toolkit, version 1.1
 *
 * Distribute and use freely; there are no restrictions on further
 * dissemination and usage except those imposed by the laws of your
 * country of residence.
 *
 */
 //(DvdKhl) Modified to make it standalone and stripped down to bare minimum

#include "AVD3NativeLibApi.h"


uint32_t rotlVariable(uint32_t x, int32_t y) {
	return (x << y) | (x >> (-y & 31));
}

void MD4TransformBlock(uint32_t* digest, const uint32_t* in) {
	// #define F(x, y, z) (((x) & (y)) | ((~x) & (z)))
#define F(x, y, z) ((z) ^ ((x) & ((y) ^ (z))))
#define G(x, y, z) (((x) & (y)) | ((x) & (z)) | ((y) & (z)))
#define H(x, y, z) ((x) ^ (y) ^ (z))

	uint32_t A, B, C, D;

	A = digest[0];
	B = digest[1];
	C = digest[2];
	D = digest[3];

#define function(a,b,c,d,k,s) a=rotlVariable(a+F(b,c,d)+in[k],s);

	function(A, B, C, D, 0, 3);
	function(D, A, B, C, 1, 7);
	function(C, D, A, B, 2, 11);
	function(B, C, D, A, 3, 19);
	function(A, B, C, D, 4, 3);
	function(D, A, B, C, 5, 7);
	function(C, D, A, B, 6, 11);
	function(B, C, D, A, 7, 19);
	function(A, B, C, D, 8, 3);
	function(D, A, B, C, 9, 7);
	function(C, D, A, B, 10, 11);
	function(B, C, D, A, 11, 19);
	function(A, B, C, D, 12, 3);
	function(D, A, B, C, 13, 7);
	function(C, D, A, B, 14, 11);
	function(B, C, D, A, 15, 19);

#undef function
#define function(a,b,c,d,k,s) a=rotlVariable(a+G(b,c,d)+in[k]+0x5a827999,s);
	function(A, B, C, D, 0, 3);
	function(D, A, B, C, 4, 5);
	function(C, D, A, B, 8, 9);
	function(B, C, D, A, 12, 13);
	function(A, B, C, D, 1, 3);
	function(D, A, B, C, 5, 5);
	function(C, D, A, B, 9, 9);
	function(B, C, D, A, 13, 13);
	function(A, B, C, D, 2, 3);
	function(D, A, B, C, 6, 5);
	function(C, D, A, B, 10, 9);
	function(B, C, D, A, 14, 13);
	function(A, B, C, D, 3, 3);
	function(D, A, B, C, 7, 5);
	function(C, D, A, B, 11, 9);
	function(B, C, D, A, 15, 13);

#undef function
#define function(a,b,c,d,k,s) a=rotlVariable(a+H(b,c,d)+in[k]+0x6ed9eba1,s);
	function(A, B, C, D, 0, 3);
	function(D, A, B, C, 8, 9);
	function(C, D, A, B, 4, 11);
	function(B, C, D, A, 12, 15);
	function(A, B, C, D, 2, 3);
	function(D, A, B, C, 10, 9);
	function(C, D, A, B, 6, 11);
	function(B, C, D, A, 14, 15);
	function(A, B, C, D, 1, 3);
	function(D, A, B, C, 9, 9);
	function(C, D, A, B, 5, 11);
	function(B, C, D, A, 13, 15);
	function(A, B, C, D, 3, 3);
	function(D, A, B, C, 11, 9);
	function(C, D, A, B, 7, 11);
	function(B, C, D, A, 15, 15);

	digest[0] += A;
	digest[1] += B;
	digest[2] += C;
	digest[3] += D;
}



//========================================================================
void* MD4Create(uint32_t* blockSize) {
	*blockSize = 64;

	uint8_t* b = (uint8_t*)malloc(sizeof(uint32_t) * (4 + 16*2 + 2));
	MD4Init(b);
	return b;
}

void MD4Init(void* handle) {
	uint32_t* digest = (uint32_t*)handle;
	digest[0] = 0x67452301L;
	digest[1] = 0xefcdab89L;
	digest[2] = 0x98badcfeL;
	digest[3] = 0x10325476L;
	for (size_t i = 0; i < 16*2 + 2; i++) {
		digest[4 + i] = 0;
	}
}

void MD4Transform(void* handle, uint8_t* b, int32_t length, uint8_t lastBlock) {
	uint32_t* word = (uint32_t*)b;
	uint32_t* wordEnd = (uint32_t*)b + (length / 64) * 16;
	uint32_t * digest = (uint32_t*)handle;
	while (word != wordEnd) {
		MD4TransformBlock(digest, word);
		word += 16;
	}
	*(uint64_t*)(digest + 4 + 16 * 2) += length;

	if (lastBlock) {
		b = (uint8_t*)wordEnd;

		uint32_t restLength = length % 64;
		uint8_t* lastBlock = (uint8_t*)(digest + 4);
		for (size_t i = 0; i < restLength; i++) lastBlock[i] = b[i];

		uint32_t padding;
		if (restLength < 56) padding = 56; else padding = 120;
		uint64_t bits = *(uint64_t*)(digest + 4 + 16 * 2) << 3;

		lastBlock[restLength] = 0x80;

		lastBlock[padding] = bits & 0xFF;
		lastBlock[padding + 1] = bits >> 8 & 0xFF;
		lastBlock[padding + 2] = bits >> 16 & 0xFF;
		lastBlock[padding + 3] = bits >> 24 & 0xFF;
		lastBlock[padding + 4] = bits >> 32 & 0xFF;
		lastBlock[padding + 5] = bits >> 40 & 0xFF;
		lastBlock[padding + 6] = bits >> 48 & 0xFF;
		lastBlock[padding + 7] = bits >> 56 & 0xFF;

		MD4TransformBlock(digest, (uint32_t*)lastBlock);
		if(padding == 120) MD4TransformBlock(digest, (uint32_t*)lastBlock + 16);
	}
}

void MD4Final(void* handle, uint8_t * b) {
	uint32_t* word = (uint32_t*)b;
	uint32_t* digest = (uint32_t*)handle;

	word[0] = digest[0];
	word[1] = digest[1];
	word[2] = digest[2];
	word[3] = digest[3];
}

void MD4ComputeHash(uint8_t* b, int32_t length, uint32_t *hash) {
	uint32_t digest[] = { 0x67452301L, 0xefcdab89L, 0x98badcfeL, 0x10325476L };

	uint32_t* word = (uint32_t*)b;
	uint32_t* wordEnd = (uint32_t*)b + (length / 64) * 16;
	while (word != wordEnd) {
		MD4TransformBlock(digest, word);
		word += 16;
	}

	b = (uint8_t*)wordEnd;

	uint32_t restLength = length % 64;
	uint8_t lastBlock[128] = {0};
	for (size_t i = 0; i < restLength; i++) lastBlock[i] = b[i];

	uint32_t padding;
	if (restLength < 56) padding = 56; else padding = 120;
	uint64_t bits = length << 3;

	lastBlock[restLength] = 0x80;

	lastBlock[padding] = bits & 0xFF;
	lastBlock[padding + 1] = bits >> 8 & 0xFF;
	lastBlock[padding + 2] = bits >> 16 & 0xFF;
	lastBlock[padding + 3] = bits >> 24 & 0xFF;
	lastBlock[padding + 4] = bits >> 32 & 0xFF;
	lastBlock[padding + 5] = bits >> 40 & 0xFF;
	lastBlock[padding + 6] = bits >> 48 & 0xFF;
	lastBlock[padding + 7] = bits >> 56 & 0xFF;

	MD4TransformBlock(digest, (uint32_t*)lastBlock);
	if (padding == 120) MD4TransformBlock(digest, (uint32_t*)lastBlock + 16);

	hash[0] = digest[0];
	hash[1] = digest[1];
	hash[2] = digest[2];
	hash[3] = digest[3];
}