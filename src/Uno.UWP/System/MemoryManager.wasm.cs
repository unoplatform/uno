using System;
using System.Globalization;
using Uno.Foundation;

using NativeMethods = __Windows.__System.MemoryManager.NativeMethods;

namespace Windows.System
{
	public partial class MemoryManager
	{
		public static ulong AppMemoryUsage
		{
			get
			{
				return (ulong)NativeMethods.GetAppMemoryUsage();
			}
		}

		public static ulong AppMemoryUsageLimit
		{
			get
			{
				if (Environment.GetEnvironmentVariable("UNO_BOOTSTRAP_EMSCRIPTEN_MAXIMUM_MEMORY") == "4GB")
				{
					return 4ul * 1024 * 1024 * 1024;
				}

				return 2ul * 1024 * 1024 * 1024;
			}
		}
	}
}
