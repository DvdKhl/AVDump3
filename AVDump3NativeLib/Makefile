ARCH ?= $(shell uname -m)
SYS ?= $(shell uname -s)

SOEXT = .so

ifeq ($(SYS),Linux)
CC = $(ARCH)-linux-gnu-gcc
else ifeq ($(SYS),Darwin)
CC = cc -arch $(ARCH)
SOEXT = .dylib
else
CC = cc
endif

CFLAGS=-I. -Wall -Werror -fpic -O3
OBJ = AVD3MirrorBuffer.o AVD3NativeLibApi.o InstructionCheck.o CRC32.o MD4.o SHA3.o Tiger.o

SSE4_FLAGS :=
SHA_FLAGS :=

ifeq ($(ARCH),x86_64)
OBJ += SHA1.o SHA256.o CRC32C.o
OBJ_SSE4 = CRC32C.o SHA1.o SHA256.o
OBJ_SHA = SHA1.o SHA256.o

$(OBJ_SSE4): SSE4_FLAGS := -msse4
$(OBJ_SHA): SHA_FLAGS := -msha
endif

AVDump3NativeLib$(SOEXT): $(OBJ)
	$(CC) -shared -o $@ $^ $(CFLAGS)

%.o: src/%.c
	$(CC) -c -o $@ $< $(CFLAGS) $(SSE4_FLAGS) $(SHA_FLAGS)

