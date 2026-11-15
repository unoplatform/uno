// Dawn's Linux release is compiled against glibc 2.38+ headers, where strtoul in C23 mode becomes __isoc23_strtoul.
// Defining it here keeps the library loadable on older glibc; the two differ only on C23's "0b" prefix.
// Must be compiled as C11 or earlier, or this strtoul would itself be redirected to __isoc23_strtoul.
#include <stdlib.h>

unsigned long __isoc23_strtoul(const char* s, char** end, int base)
{
	return strtoul(s, end, base);
}
