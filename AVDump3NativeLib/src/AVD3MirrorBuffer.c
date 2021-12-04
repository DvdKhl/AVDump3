#define _GNU_SOURCE
#include "AVD3NativeLibApi.h"

#ifdef __unix__

#include <stdio.h>
#include <assert.h>
#include <fcntl.h>
#include <unistd.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <errno.h>

char* CreateMirrorBuffer(uint32_t minLength, AVD3MirrorBufferCreateHandle* handle) {
	uint32_t pageSize = getpagesize();
	uint32_t length = (minLength / pageSize + (minLength % pageSize == 0 ? 0 : 1)) * pageSize;

	char path[] = "/AVD3WrapAroundBuffer";
	int fd = shm_open(path, O_RDWR | O_CREAT | O_EXCL, 0600);
	if (fd == -1) return "shm_open returned -1";
	int success = shm_unlink(path);
	if (success != 0) return "shm_unlink returned non-zero";

	success = ftruncate(fd, length * 2);
	if (success != 0) return "ftruncate returned non-zero";

	uint8_t* data = mmap(0, length * 2, PROT_READ | PROT_WRITE, MAP_SHARED, fd, 0);
	if (data == (void*)-1)return "mmap (first) returned -1";

	uint8_t* mirror = mmap(data + length, length, PROT_READ | PROT_WRITE, MAP_SHARED | MAP_FIXED, fd, 0);
	if (mirror == (void*)-1) {
		if (munmap(data, length) != 0) return "munmap returned non-zero";
		return "mmap (second) returned -1";
	}

	close(fd);

	for (size_t i = 0; i < length; i++) {
		data[i] = (uint8_t)i;
		if (data[i] != mirror[i]) return "Data Mirroring failed";
		data[i] = 0;
	}

	handle->fileHandle = 0;
	handle->baseAddress = data;
	handle->length = length;

	return 0;
}

char* FreeMirrorBuffer(AVD3MirrorBufferCreateHandle * handle) {

	return 0;
}

#endif


#ifdef _WIN32

#include <Windows.h>

char* CreateMirrorBuffer(uint32_t minLength, AVD3MirrorBufferCreateHandle * handle) {
	SYSTEM_INFO sysInfo;
	GetSystemInfo(&sysInfo);
	uint32_t pageSize = sysInfo.dwAllocationGranularity;
	if (pageSize <= 0) return "GetSystemInfo dwAllocationGranularity is zero or negative";

	uint32_t length = (minLength / pageSize + (minLength % pageSize == 0 ? 0 : 1)) * pageSize;

	HANDLE fileMappingHandle = CreateFileMapping(INVALID_HANDLE_VALUE, 0, PAGE_READWRITE, 0, length, NULL);
	if (fileMappingHandle == NULL) return "CreateFileMapping returned NULL";

	uint8_t * data = 0, *mirror = 0;
	for (int i = 0; i < 100; i++) {
		void* memPtr = VirtualAlloc(0, length * 2, MEM_RESERVE, PAGE_READWRITE);
		if (memPtr == 0) continue;

		if (!VirtualFree(memPtr, 0, MEM_RELEASE)) return "VirtualFree returned NULL";


		data = MapViewOfFileEx(fileMappingHandle, FILE_MAP_WRITE, 0, 0, length, 0);
		if (data == 0) continue;

		mirror = MapViewOfFileEx(fileMappingHandle, FILE_MAP_WRITE, 0, 0, length, data + length);
		if (mirror == 0) {
			if (!UnmapViewOfFile(data)) return "UnmapViewOfFile returned NULL";
			continue;
		}
		break;
	}
	if (mirror == 0) return "Couldn't create Mirrored Buffer";

	for (size_t i = 0; i < length; i++) {
		data[i] = (char)i;
		if (data[i] != mirror[i]) {
			return "Data Mirroring failed";
		}
		data[i] = 0;
	}
	handle->fileHandle = fileMappingHandle;
	handle->baseAddress = data;
	handle->length = length;

	return 0;
}

char* FreeMirrorBuffer(AVD3MirrorBufferCreateHandle* handle) {
	if (!UnmapViewOfFile(handle->baseAddress)) return "First UnmapViewOfFile failed";
	if (!UnmapViewOfFile(handle->baseAddress + handle->length)) return "Second UnmapViewOfFile failed";
	if (!CloseHandle(handle->fileHandle)) return "CloseHandle failed";
	return 0;
}

#endif