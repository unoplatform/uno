using System;
using System.Runtime.InteropServices;
using System.Threading;
using Private.Infrastructure;

namespace MUXControlsTestApp.Utilities
{
	internal class IdleSynchronizer
	{
		public static void Wait()
		{
			TestServices.WindowHelper.WaitForIdle().Wait(TestUtilities.DefaultWaitMs);
		}
	}
}
