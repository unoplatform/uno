using System;
using Android.OS;
using Android.App;
using Uno.UI;

namespace Windows.System
{
	public partial class MemoryManager
	{
		public static ulong AppMemoryUsage
		{
			get
			{
				Debug.MemoryInfo mi = new Debug.MemoryInfo();
				Debug.GetMemoryInfo(mi);
				var totalMemory = mi.TotalPss * 1024;
				return (ulong)totalMemory;
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
