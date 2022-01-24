using System;
using System.Globalization;
using Uno.Foundation;

namespace Windows.System;

public partial class MemoryManager
{
	internal static float HighPressureThreshold { get; set; } = .90f;
	internal static float MediumPressureThreshold { get; set; } = .70f;

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public static global::Windows.System.AppMemoryUsageLevel AppMemoryUsageLevel
	{
		get
		{
			if (AppMemoryUsage >= AppMemoryUsageLimit * HighPressureThreshold)
			{
				return AppMemoryUsageLevel.High;
			}

			if (AppMemoryUsage >= AppMemoryUsageLimit * MediumPressureThreshold)
			{
				return AppMemoryUsageLevel.Medium;
			}

			return AppMemoryUsageLevel.Low;
		}
	}

}
