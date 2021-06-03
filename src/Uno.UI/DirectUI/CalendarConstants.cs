using System;
using System.Linq;

namespace DirectUI
{
	internal class CalendarConstants
	{
		internal const long s_ticksPerDay = 864000000000L;       // 24 hours, the regular days
		internal const long s_ticksPerHour = 36000000000L;       // 1 hour for day light saving
		internal const long s_maxTicksPerDay = s_ticksPerDay + s_ticksPerHour;
		internal const long s_maxTicksPerMonth = 31 * s_ticksPerDay + s_ticksPerHour;
		internal const long s_maxTicksPerYear = 366 * s_ticksPerDay + s_ticksPerHour;
	}
}
