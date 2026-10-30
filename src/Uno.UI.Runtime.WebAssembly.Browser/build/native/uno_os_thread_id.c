// System.Threading.Lock identifies its owner with SystemNative_TryGetUInt32OSThreadId, which the
// multithreaded browser runtime returns as 1 on every thread (dotnet/runtime#129900). A lock contended
// by two thread pool threads is then taken for recursion and Lock.Exit throws SynchronizationLockException.
// Linked with -Wl,--wrap=SystemNative_TryGetUInt32OSThreadId so the lock sees a real per-thread id.

#include <pthread.h>
#include <stdint.h>

uint32_t __wrap_SystemNative_TryGetUInt32OSThreadId(void)
{
	return (uint32_t)(uintptr_t)pthread_self();
}
