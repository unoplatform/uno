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
	}
}
