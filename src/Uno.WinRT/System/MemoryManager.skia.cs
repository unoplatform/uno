#nullable enable

using System;
using System.Globalization;
using System.Reflection;
using Uno.Foundation;
using Uno.Foundation.Logging;

namespace Windows.System
{
	public partial class MemoryManager
	{
		public static ulong AppMemoryUsage
		{
			get
			{
				return (ulong)GC.GetGCMemoryInfo().MemoryLoadBytes;
			}
		}

		public static ulong AppMemoryUsageLimit
		{
			get
			{
				return (ulong)GC.GetGCMemoryInfo().HighMemoryLoadThresholdBytes;
			}
		}
	}
}
