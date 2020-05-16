CC=gcc
CFLAGS=-I. -Wall -Werror -fpic -O3 -lrt
OBJ = AVD3MirrorBuffer.o AVD3NativeLibApi.o InstructionCheck.o CRC32.o CRC32C.o MD4.o SHA1.o SHA3.o SHA256.o Tiger.o 
OBJ_SSE4 = CRC32C.o SHA1.o SHA256.o
OBJ_SHA = SHA1.o SHA256.o

$(OBJ_SSE4): SSE4_FLAGS := -msse4
$(OBJ_SHA): SHA_FLAGS := -msha

AVDump3NativeLib.so: $(OBJ)
	$(CC) -shared -o $@ $^ $(CFLAGS)

%.o: src/%.c
	$(CC) -c -o $@ $< $(CFLAGS) $(SSE4_FLAGS) $(SHA_FLAGS)

