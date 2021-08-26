using System;
using Android.App;
using Uno.UI;

namespace Windows.System
{
	partial class MemoryManager
	{
		public static ulong AppMemoryUsage
		{
			get
			{
				return (ulong) GC.GetTotalMemory(false);
			}
		}

		public static ulong AppMemoryUsageLimit
		{
			get
			{
				ActivityManager.MemoryInfo memoryInfo = new ActivityManager.MemoryInfo();
				ActivityManager.FromContext(ContextHelper.Current)?.GetMemoryInfo(memoryInfo);

				return AppMemoryUsage + (ulong)memoryInfo.AvailMem - (ulong)memoryInfo.Threshold;
			}
		}
	}
}
