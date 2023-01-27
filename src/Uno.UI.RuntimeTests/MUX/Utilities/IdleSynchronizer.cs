using System;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.UI.Core;
using Private.Infrastructure;

namespace MUXControlsTestApp.Utilities
{
	internal class IdleSynchronizer
	{
		public static void Wait()
		{
#if __WASM__
			if (!Uno.UI.Dispatching.CoreDispatcher.IsThreadingSupported)
			{
				return;
			}
#endif
#if !NETFX_CORE
			if (!CoreDispatcher.Main.HasThreadAccess)
#endif
			{
				TestServices.WindowHelper.WaitForIdle().Wait(TestUtilities.DefaultWaitMs);
			}
		}
	}
}
