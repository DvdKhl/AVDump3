//https://raw.githubusercontent.com/rhash/RHash/master/librhash/sha3.c
/* sha3.c - an implementation of Secure Hash Algorithm 3 (Keccak).
 * based on the
 * The Keccak SHA-3 submission. Submission to NIST (Round 3), 2011
 * by Guido Bertoni, Joan Daemen, Michaël Peeters and Gilles Van Assche
 *
 * Copyright (c) 2013, Aleksey Kravchenko <rhash.admin@gmail.com>
 *
 * Permission to use, copy, modify, and/or distribute this software for any
 * purpose with or without fee is hereby granted.
 *
 * THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
 * REGARD TO THIS SOFTWARE  INCLUDING ALL IMPLIED WARRANTIES OF  MERCHANTABILITY
 * AND FITNESS.  IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
 * INDIRECT,  OR CONSEQUENTIAL DAMAGES  OR ANY DAMAGES WHATSOEVER RESULTING FROM
 * LOSS OF USE,  DATA OR PROFITS,  WHETHER IN AN ACTION OF CONTRACT,  NEGLIGENCE
 * OR OTHER TORTIOUS ACTION,  ARISING OUT OF  OR IN CONNECTION  WITH THE USE  OR
 * PERFORMANCE OF THIS SOFTWARE.
 */

#include "AVD3NativeLibApi.h"


#define sha3_224_hash_size  28
#define sha3_256_hash_size  32
#define sha3_384_hash_size  48
#define sha3_512_hash_size  64
#define sha3_max_permutation_size 25
#define sha3_max_rate_in_qwords 24


 /* constants */
#define NumberOfRounds 24


/**
 * SHA3 Algorithm context.
 */
typedef struct sha3_ctx {
	/* 1600 bits algorithm hashing state */
	uint64_t hash[sha3_max_permutation_size];
	/* size of a message block processed at once */
	unsigned block_size;
	unsigned bits;
} sha3_ctx;


#if defined(_MSC_VER) || defined(__BORLANDC__)
#define I64(x) x##ui64
#else
#define I64(x) x##ULL
#endif

/* SHA3 (Keccak) constants for 24 rounds */
static uint64_t keccak_round_constants[NumberOfRounds] = {
	I64(0x0000000000000001), I64(0x0000000000008082), I64(0x800000000000808A), I64(0x8000000080008000),
	I64(0x000000000000808B), I64(0x0000000080000001), I64(0x8000000080008081), I64(0x8000000000008009),
	I64(0x000000000000008A), I64(0x0000000000000088), I64(0x0000000080008009), I64(0x000000008000000A),
	I64(0x000000008000808B), I64(0x800000000000008B), I64(0x8000000000008089), I64(0x8000000000008003),
	I64(0x8000000000008002), I64(0x8000000000000080), I64(0x000000000000800A), I64(0x800000008000000A),
	I64(0x8000000080008081), I64(0x8000000000008080), I64(0x0000000080000001), I64(0x8000000080008008)
};

/* Initializing a sha3 context for given number of output bits */
static void rhash_keccak_init(sha3_ctx* ctx, unsigned bits) {
	/* NB: The Keccak capacity parameter = bits * 2 */
	unsigned rate = 1600 - bits * 2;

	memset(ctx, 0, sizeof(sha3_ctx));
	ctx->bits = bits;
	ctx->block_size = rate / 8;
}

/**
 * Initialize context before calculating hash.
 *
 * @param ctx context to initialize
 */
void rhash_sha3_224_init(sha3_ctx* ctx) {
	rhash_keccak_init(ctx, 224);
}

/**
 * Initialize context before calculating hash.
 *
 * @param ctx context to initialize
 */
void rhash_sha3_256_init(sha3_ctx* ctx) {
	rhash_keccak_init(ctx, 256);
}

/**
 * Initialize context before calculating hash.
 *
 * @param ctx context to initialize
 */
void rhash_sha3_384_init(sha3_ctx* ctx) {
	rhash_keccak_init(ctx, 384);
}

/**
 * Initialize context before calculating hash.
 *
 * @param ctx context to initialize
 */
void rhash_sha3_512_init(sha3_ctx* ctx) {
	rhash_keccak_init(ctx, 512);
}

#define XORED_A(i) A[(i)] ^ A[(i) + 5] ^ A[(i) + 10] ^ A[(i) + 15] ^ A[(i) + 20]
#define THETA_STEP(i) \
	A[(i)]      ^= D[(i)]; \
	A[(i) + 5]  ^= D[(i)]; \
	A[(i) + 10] ^= D[(i)]; \
	A[(i) + 15] ^= D[(i)]; \
	A[(i) + 20] ^= D[(i)] \

#define ROTL64(qword, n) ((qword) << (n) ^ ((qword) >> (64 - (n))))
#define me64_to_le_str(to, from, length) memcpy((to), (from), (length))


/* Keccak theta() transformation */
static void keccak_theta(uint64_t* A) {
	uint64_t D[5];
	D[0] = ROTL64(XORED_A(1), 1) ^ XORED_A(4);
	D[1] = ROTL64(XORED_A(2), 1) ^ XORED_A(0);
	D[2] = ROTL64(XORED_A(3), 1) ^ XORED_A(1);
	D[3] = ROTL64(XORED_A(4), 1) ^ XORED_A(2);
	D[4] = ROTL64(XORED_A(0), 1) ^ XORED_A(3);
	THETA_STEP(0);
	THETA_STEP(1);
	THETA_STEP(2);
	THETA_STEP(3);
	THETA_STEP(4);
}

/* Keccak pi() transformation */
static void keccak_pi(uint64_t* A) {
	uint64_t A1;
	A1 = A[1];
	A[1] = A[6];
	A[6] = A[9];
	A[9] = A[22];
	A[22] = A[14];
	A[14] = A[20];
	A[20] = A[2];
	A[2] = A[12];
	A[12] = A[13];
	A[13] = A[19];
	A[19] = A[23];
	A[23] = A[15];
	A[15] = A[4];
	A[4] = A[24];
	A[24] = A[21];
	A[21] = A[8];
	A[8] = A[16];
	A[16] = A[5];
	A[5] = A[3];
	A[3] = A[18];
	A[18] = A[17];
	A[17] = A[11];
	A[11] = A[7];
	A[7] = A[10];
	A[10] = A1;
	/* note: A[ 0] is left as is */
}

#define CHI_STEP(i) \
	A0 = A[0 + (i)]; \
	A1 = A[1 + (i)]; \
	A[0 + (i)] ^= ~A1 & A[2 + (i)]; \
	A[1 + (i)] ^= ~A[2 + (i)] & A[3 + (i)]; \
	A[2 + (i)] ^= ~A[3 + (i)] & A[4 + (i)]; \
	A[3 + (i)] ^= ~A[4 + (i)] & A0; \
	A[4 + (i)] ^= ~A0 & A1 \

/* Keccak chi() transformation */
static void keccak_chi(uint64_t* A) {
	uint64_t A0, A1;
	CHI_STEP(0);
	CHI_STEP(5);
	CHI_STEP(10);
	CHI_STEP(15);
	CHI_STEP(20);
}

static void rhash_sha3_permutation(uint64_t* state) {
	int round;
	for (round = 0; round < NumberOfRounds; round++) {
		keccak_theta(state);

		/* apply Keccak rho() transformation */
		state[1] = ROTL64(state[1], 1);
		state[2] = ROTL64(state[2], 62);
		state[3] = ROTL64(state[3], 28);
		state[4] = ROTL64(state[4], 27);
		state[5] = ROTL64(state[5], 36);
		state[6] = ROTL64(state[6], 44);
		state[7] = ROTL64(state[7], 6);
		state[8] = ROTL64(state[8], 55);
		state[9] = ROTL64(state[9], 20);
		state[10] = ROTL64(state[10], 3);
		state[11] = ROTL64(state[11], 10);
		state[12] = ROTL64(state[12], 43);
		state[13] = ROTL64(state[13], 25);
		state[14] = ROTL64(state[14], 39);
		state[15] = ROTL64(state[15], 41);
		state[16] = ROTL64(state[16], 45);
		state[17] = ROTL64(state[17], 15);
		state[18] = ROTL64(state[18], 21);
		state[19] = ROTL64(state[19], 8);
		state[20] = ROTL64(state[20], 18);
		state[21] = ROTL64(state[21], 2);
		state[22] = ROTL64(state[22], 61);
		state[23] = ROTL64(state[23], 56);
		state[24] = ROTL64(state[24], 14);

		keccak_pi(state);
		keccak_chi(state);

		/* apply iota(state, round) */
		*state ^= keccak_round_constants[round];
	}
}

/**
 * The core transformation. Process the specified block of data.
 *
 * @param hash the algorithm state
 * @param block the message block to process
 * @param block_size the size of the processed block in bytes
 */
static void rhash_sha3_process_block_224(uint64_t hash[25], const uint64_t* block, size_t block_size) {
	hash[0] ^= block[0];
	hash[1] ^= block[1];
	hash[2] ^= block[2];
	hash[3] ^= block[3];
	hash[4] ^= block[4];
	hash[5] ^= block[5];
	hash[6] ^= block[6];
	hash[7] ^= block[7];
	hash[8] ^= block[8];
	hash[9] ^= block[9];
	hash[10] ^= block[10];
	hash[11] ^= block[11];
	hash[12] ^= block[12];
	hash[13] ^= block[13];
	hash[14] ^= block[14];
	hash[15] ^= block[15];
	hash[16] ^= block[16];
	hash[17] ^= block[17];

	/* make a permutation of the hash */
	rhash_sha3_permutation(hash);
}
static void rhash_sha3_process_block_256(uint64_t hash[25], const uint64_t* block, size_t block_size) {
	hash[0] ^= block[0];
	hash[1] ^= block[1];
	hash[2] ^= block[2];
	hash[3] ^= block[3];
	hash[4] ^= block[4];
	hash[5] ^= block[5];
	hash[6] ^= block[6];
	hash[7] ^= block[7];
	hash[8] ^= block[8];
	hash[9] ^= block[9];
	hash[10] ^= block[10];
	hash[11] ^= block[11];
	hash[12] ^= block[12];
	hash[13] ^= block[13];
	hash[14] ^= block[14];
	hash[15] ^= block[15];
	hash[16] ^= block[16];

	/* make a permutation of the hash */
	rhash_sha3_permutation(hash);
}
static void rhash_sha3_process_block_384(uint64_t hash[25], const uint64_t* block, size_t block_size) {
	hash[0] ^= block[0];
	hash[1] ^= block[1];
	hash[2] ^= block[2];
	hash[3] ^= block[3];
	hash[4] ^= block[4];
	hash[5] ^= block[5];
	hash[6] ^= block[6];
	hash[7] ^= block[7];
	hash[8] ^= block[8];
	hash[9] ^= block[9];
	hash[10] ^= block[10];
	hash[11] ^= block[11];
	hash[12] ^= block[12];

	/* make a permutation of the hash */
	rhash_sha3_permutation(hash);
}
static void rhash_sha3_process_block_512(uint64_t hash[25], const uint64_t* block, size_t block_size) {
	hash[0] ^= block[0];
	hash[1] ^= block[1];
	hash[2] ^= block[2];
	hash[3] ^= block[3];
	hash[4] ^= block[4];
	hash[5] ^= block[5];
	hash[6] ^= block[6];
	hash[7] ^= block[7];
	hash[8] ^= block[8];

	/* make a permutation of the hash */
	rhash_sha3_permutation(hash);
}

#define SHA3_FINALIZED 0x80000000

/**
 * Calculate message hash.
 * Can be called repeatedly with chunks of the message to be hashed.
 *
 * @param ctx the algorithm context containing current hashing state
 * @param msg message chunk
 * @param size length of the message chunk
 */
void rhash_sha3_update(sha3_ctx* ctx, uint8_t* msg, size_t size) {
	size_t block_size = (size_t)ctx->block_size;
	uint8_t* msgEnd = msg + size;

	switch (ctx->bits) {
	case 224:
		while (msg != msgEnd) {
			rhash_sha3_process_block_224(ctx->hash, (uint64_t*)msg, block_size);
			msg += block_size;
		}
		break;
	case 256:
		while (msg != msgEnd) {
			rhash_sha3_process_block_256(ctx->hash, (uint64_t*)msg, block_size);
			msg += block_size;
		}
		break;
	case 384:
		while (msg != msgEnd) {
			rhash_sha3_process_block_384(ctx->hash, (uint64_t*)msg, block_size);
			msg += block_size;
		}
		break;
	case 512:
		while (msg != msgEnd) {
			rhash_sha3_process_block_512(ctx->hash, (uint64_t*)msg, block_size);
			msg += block_size;
		}
		break;
	}
}

/**
 * Store calculated hash into the given array.
 *
 * @param ctx the algorithm context containing current hashing state
 * @param result calculated hash in binary form
 */
void rhash_sha3_final(sha3_ctx* ctx, const unsigned char* msg, size_t size, uint8_t lastByte, unsigned char* result) {
	size_t digest_length = 100 - ctx->block_size / 2;
	const size_t block_size = ctx->block_size;

	uint8_t lastBlock[144];

	/* clear the rest of the data queue */
	memcpy(lastBlock, msg, size);
	memset((char*)lastBlock + size, 0, block_size - size);
	((char*)lastBlock)[size] |= lastByte;
	((char*)lastBlock)[block_size - 1] |= 0x80;

	/* process final block */
	rhash_sha3_update(ctx, lastBlock, block_size);

	me64_to_le_str(result, ctx->hash, digest_length);
}

/* ========================================================================= */
void* SHA3Create(uint32_t* hashLength, uint32_t* blockLength) {
	if (*hashLength != 224 && *hashLength != 256 && *hashLength != 384 && *hashLength != 512) *hashLength = 256;

	sha3_ctx* ctx = malloc(sizeof(sha3_ctx) + 4);
	*(int32_t*)(ctx + 1) = *hashLength;

	SHA3Init(ctx);
	*blockLength = ctx->block_size;

	return ctx;
}

void SHA3Init(void* handle) {
	sha3_ctx* ctx = handle;

	switch (*(uint32_t*)((uint8_t*)handle + sizeof(sha3_ctx))) {
	case 224: rhash_sha3_224_init(ctx); break;
	case 256: rhash_sha3_256_init(ctx); break;
	case 384: rhash_sha3_384_init(ctx); break;
	case 512: rhash_sha3_512_init(ctx); break;
	}
}

void SHA3Transform(void* handle, uint8_t* b, int32_t length) {
	sha3_ctx* ctx = handle;
	rhash_sha3_update(ctx, b, length);
}

void SHA3Final(void* handle, uint8_t* b, int32_t length, uint8_t* hash) {
	sha3_ctx* ctx = handle;
	rhash_sha3_final(ctx, b, length, 6, hash);
}

/* ========================================================================= */
void* KeccakCreate(uint32_t* hashLength, uint32_t* blockLength) {
	if (*hashLength != 224 && *hashLength != 256 && *hashLength != 384 && *hashLength != 512) *hashLength = 256;

	sha3_ctx* ctx = malloc(sizeof(sha3_ctx) + 4);
	*(int32_t*)(ctx + 1) = *hashLength;

	KeccakInit(ctx);
	*blockLength = ctx->block_size;

	return ctx;
}

void KeccakInit(void* handle) {
	sha3_ctx* ctx = handle;

	switch (*(uint32_t*)((uint8_t*)handle + sizeof(sha3_ctx))) {
	case 224: rhash_sha3_224_init(ctx); break;
	case 256: rhash_sha3_256_init(ctx); break;
	case 384: rhash_sha3_384_init(ctx); break;
	case 512: rhash_sha3_512_init(ctx); break;
	}
}

void KeccakTransform(void* handle, uint8_t* b, int32_t length) {
	sha3_ctx* ctx = handle;
	rhash_sha3_update(ctx, b, length);
}

void KeccakFinal(void* handle, uint8_t* b, int32_t length, uint8_t* hash) {
	sha3_ctx* ctx = handle;
	rhash_sha3_final(ctx, b, length, 1, hash);
}
