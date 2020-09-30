#if __SKIA__
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
#if !NET6_0_OR_GREATER
		private static PropertyInfo? _appMemoryUsageProperty;
		private static PropertyInfo? _highMemoryLoadThresholdBytesProperty;
		private static MethodInfo? _getGCMemoryInfoMethod;

		static MemoryManager()
		{
			InitializeGCMemoryInfo();
		}

		private static void InitializeGCMemoryInfo()
		{
			try
			{
				_getGCMemoryInfoMethod = typeof(GC).GetMethod("GetGCMemoryInfo", Array.Empty<Type>());

				if (_getGCMemoryInfoMethod != null)
				{
					var gcMemoryInfo = Type.GetType("System.GCMemoryInfo");

					if (gcMemoryInfo != null)
					{
						// Use TotalCommittedBytes first, if available.
						_appMemoryUsageProperty = gcMemoryInfo.GetProperty("TotalCommittedBytes");

						// Use TotalCommittedBytes first, otherwise use HeapSizeBytes (net5).
						_appMemoryUsageProperty = _appMemoryUsageProperty ?? gcMemoryInfo.GetProperty("HeapSizeBytes");
						_highMemoryLoadThresholdBytesProperty = gcMemoryInfo.GetProperty("HighMemoryLoadThresholdBytes");

						IsAvailable = true;
						return;
					}
				}
			}
			catch(Exception e)
			{
				if (typeof(MemoryManager).Log().IsEnabled(LogLevel.Warning))
				{
					typeof(MemoryManager).Log().Warn($"Memory manager failed to initialize. Please report an issue.", e);
				}
			}

			IsAvailable = false;
		}
#endif

		public static ulong AppMemoryUsage
		{
			get
			{
#if NET6_0_OR_GREATER
				return GC.GetGCMemoryInfo().MemoryLoadBytes;
#else
				if (IsAvailable)
				{
					var info = _getGCMemoryInfoMethod!.Invoke(null, null);
					return (ulong)(long)_appMemoryUsageProperty!.GetValue(info)!;
				}
				else
				{
					return 0;
				}
#endif
			}
		}

		public static ulong AppMemoryUsageLimit
		{
			get
			{
#if NET6_0_OR_GREATER
				return GC.GetGCMemoryInfo().HighMemoryLoadThresholdBytes;
#else
				if (IsAvailable)
				{
					var info = _getGCMemoryInfoMethod!.Invoke(null, null);
					return (ulong)(long)_highMemoryLoadThresholdBytesProperty!.GetValue(info)!;
				}
				else
				{
					return 0;
				}
#endif
			}
		}
	}
}
#endif
