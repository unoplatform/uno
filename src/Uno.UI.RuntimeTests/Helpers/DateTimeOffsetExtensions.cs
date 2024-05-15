using System;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Helpers
{
	internal static class DateTimeOffsetExtensions
	{
		internal static long UniversalTime(this DateTimeOffset dto)
		{
			return dto.UtcTicks;
		}

		internal static long WindowsFoundationUniversalTime(this DateTimeOffset dto)
		{
			// https://github.com/microsoft/CsWinRT/blob/7dc82799c7afeaf862c9fb7af78ad0e2fc03c48e/src/WinRT.Runtime/Projections/SystemTypes.cs#L71
			// https://github.com/microsoft/CsWinRT/blob/7dc82799c7afeaf862c9fb7af78ad0e2fc03c48e/src/WinRT.Runtime/Projections/SystemTypes.cs#L81
			const long ManagedUtcTicksAtNativeZero = 504911232000000000;
			return dto.UtcTicks - ManagedUtcTicksAtNativeZero;
		}
	}
}
