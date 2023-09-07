#nullable enable

using Windows.UI.Core;

namespace UITests;

internal static class CoreWindowAccessor
{
	public static CoreWindow? GetForCurrentThreadSafe()
	{
#if HAS_UNO
		return CoreWindow.GetForCurrentThreadSafe();
#else
		return CoreWindow.GetForCurrentThread();
#endif
	}
}
