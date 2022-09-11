using System;
using System.Globalization;
using Uno.Foundation;

namespace Windows.System;

public partial class MemoryManager
{
	internal static bool IsAvailable { get; private set; }
	internal static float HighPressureThreshold { get; set; } = .90f;
	internal static float MediumPressureThreshold { get; set; } = .70f;

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
