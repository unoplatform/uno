using System.Runtime.InteropServices;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;

namespace Uno.UI.Runtime.Skia.MacOS;

internal static class MacOSDispatcher
{
	private static readonly nint _libDispatch = NativeUnix.dlopen("/usr/lib/system/libdispatch.dylib", 0);
	// note: `dispatch_get_main_queue` does not really exists (it's inlined inside apps by the compiler)
	private static readonly nint _mainQueue = NativeUnix.dlsym(_libDispatch, "_dispatch_main_q");

	[UnmanagedCallersOnly]
	internal static void NativeToManaged(IntPtr context)
	{
		var gch = GCHandle.FromIntPtr(context);
		try
		{
			if (gch.Target is Action action)
			{
				action();
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
		finally
		{
			gch.Free();
		}
	}

	internal static unsafe void DispatchNativeSingle(Action d, NativeDispatcherPriority p)
	{
		if (typeof(MacOSDispatcher).Log().IsEnabled(LogLevel.Trace))
		{
			typeof(MacOSDispatcher).Log().Trace($"Dispatching {d}");
		}

		NativeMac.dispatch_async_f(_mainQueue, (nint)GCHandle.Alloc(d), &NativeToManaged);
	}
}
