gcc -c -Wall -Werror -fpic -mavx *.c
gcc -shared -o AVDump3NativeLib.so *.o