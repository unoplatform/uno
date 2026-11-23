#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Helpers;

internal static class LeakTestHelper
{
	/// <summary>
	/// Collects at least 3 times, pumping the dispatcher in between so the removals posted by finalizers run,
	/// then keeps going until <paramref name="until"/> (by default, all of <paramref name="refs"/> collected) or 10 seconds.
	/// </summary>
	public static async Task CollectAsync(List<WeakReference> refs, Func<bool>? until = null)
	{
		until ??= () => !refs.Any(r => r.IsAlive);
		var sw = Stopwatch.StartNew();
		var timeout = TimeSpan.FromSeconds(10);
		var minIterations = 3;

		while (minIterations-- > 0 || (!until() && sw.Elapsed < timeout))
		{
			GC.Collect(2);
			GC.WaitForPendingFinalizers();
			GC.Collect(2);

			// Some platforms need a yield before weak references are cleared, as in Given_BindingMemoryLeak.
#if __SKIA__
			if (OperatingSystem.IsBrowser() || OperatingSystem.IsLinux() || OperatingSystem.IsIOS() || OperatingSystem.IsAndroid())
#endif
			{
				await Task.Yield();
				GC.Collect(2);
				GC.WaitForPendingFinalizers();
				GC.Collect(2);
			}

			await WindowHelper.WaitForIdle();
		}
	}

	/// <summary>
	/// Collects until <paramref name="refs"/> are collected, at most 10 times, without yielding: removals that finalizers
	/// post to the dispatcher stay pending. Some platforms need a yield to clear weak references, so callers check the result.
	/// </summary>
	public static void CollectWithoutDispatcher(IEnumerable<WeakReference> refs)
	{
		for (var i = 0; i < 10 && refs.Any(r => r.IsAlive); i++)
		{
			GC.Collect(2);
			GC.WaitForPendingFinalizers();
		}
	}
}
