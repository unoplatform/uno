// Vectorized STL helpers that Dawn's prebuilt library references and newer MSVC STLs no longer define.
// The linker only takes a definition from here when the STL lacks it.
#include <cstdint>

template <class T> static const void* MinElement(const void* first, const void* last)
{
	auto f = static_cast<const T*>(first), l = static_cast<const T*>(last), best = f;
	for (; f != l; ++f) { if (*f < *best) best = f; }
	return best;
}

template <class T> static const void* MaxElement(const void* first, const void* last)
{
	auto f = static_cast<const T*>(first), l = static_cast<const T*>(last), best = f;
	for (; f != l; ++f) { if (*best < *f) best = f; }
	return best;
}

extern "C" {
const void* __stdcall __std_min_element_1u(const void* f, const void* l) noexcept { return MinElement<uint8_t>(f, l); }
const void* __stdcall __std_min_element_4u(const void* f, const void* l) noexcept { return MinElement<uint32_t>(f, l); }
const void* __stdcall __std_min_element_8u(const void* f, const void* l) noexcept { return MinElement<uint64_t>(f, l); }
const void* __stdcall __std_max_element_4u(const void* f, const void* l) noexcept { return MaxElement<uint32_t>(f, l); }
}
