#nullable disable

// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
using System;
using Uno.Extensions.ValueType;

namespace Uno.Extensions
{
	internal static class DateTimeExtensions
	{
		public static bool IsWeekEnd(this DateTime instance)
		{
			return instance.DayOfWeek == DayOfWeek.Saturday ||
				   instance.DayOfWeek == DayOfWeek.Sunday;
		}

		public static bool IsWeekDay(this DateTime instance)
		{
			return !IsWeekEnd(instance);
		}

		public static DateTime AddWeekDays(this DateTime instance, int days)
		{
			var sign = Math.Sign(days);
			var unsignedDays = Math.Abs(days);
			for (var i = 0; i < unsignedDays; i++)
			{
				do
				{
					instance = instance.AddDays(sign);
				}
				while (instance.IsWeekEnd());
			}
			return instance;
		}

		public static DateTime AddWeekDays(this DateTime instance, TimeSpan timeSpan)
		{
			return AddWeekDays(instance, Convert.ToInt32(timeSpan.TotalDays));
		}

        public static bool Equal(this DateTime lhs, DateTime rhs, DateTimeUnit unit)
        {
            if (unit.ContainsAll(DateTimeUnit.Year))
            {
                if (lhs.Year != rhs.Year)
                {
                    return false;
                }
            }

            if (unit.ContainsAll(DateTimeUnit.Month))
            {
                if (lhs.Month != rhs.Month)
                {
                    return false;
                }
            }

            if (unit.ContainsAll(DateTimeUnit.Day))
            {
                if (lhs.Day != rhs.Day)
                {
                    return false;
                }
            }

            if (unit.ContainsAll(DateTimeUnit.Hour))
            {
                if (lhs.Hour != rhs.Hour)
                {
                    return false;
                }
            }

            if (unit.ContainsAll(DateTimeUnit.Minute))
            {
                if (lhs.Minute != rhs.Minute)
                {
                    return false;
                }
            }

            if (unit.ContainsAll(DateTimeUnit.Second))
            {
                if (lhs.Second != rhs.Second)
                {
                    return false;
                }
            }

            if (unit.ContainsAll(DateTimeUnit.Millisecond))
            {
                if (lhs.Millisecond != rhs.Millisecond)
                {
                    return false;
                }
            }

            return true;
        }

		public static DateTime Truncate(this DateTime instance, DateTimeUnit unit)
		{
			var year = unit.ContainsAll(DateTimeUnit.Year) ? instance.Year : 1;
			var month = unit.ContainsAll(DateTimeUnit.Month) ? instance.Month : 1;
			var day = unit.ContainsAll(DateTimeUnit.Day) ? instance.Day : 1;
			var hour = unit.ContainsAll(DateTimeUnit.Hour) ? instance.Hour : 0;
			var minute = unit.ContainsAll(DateTimeUnit.Minute) ? instance.Minute : 0;
			var second = unit.ContainsAll(DateTimeUnit.Second) ? instance.Second : 0;
			var millisecond = unit.ContainsAll(DateTimeUnit.Millisecond) ? instance.Millisecond : 0;

			return new DateTime(year, month, day, hour, minute, second, millisecond, instance.Kind);
		}

		public static DateTime AddWeeks(this DateTime dateTime, int weeks)
		{
			return dateTime.AddDays(weeks * 7);
		}

		public static bool IsBefore(this DateTime? instance, DateTime? unit)
		{
			if (!instance.HasValue || !unit.HasValue)
			{
				throw new ArgumentNullException();
			}
			return instance.Value.IsBefore(unit.Value);
		}

		public static bool IsBefore(this DateTime instance, DateTime unit)
		{
			return instance.CompareTo(unit) < 0;
		}

		public static bool IsAfter(this DateTime? instance, DateTime? unit)
		{
			if (!instance.HasValue || !unit.HasValue)
			{
				throw new ArgumentNullException();
			}
			return instance.Value.IsAfter(unit.Value);
		}

		public static bool IsAfter(this DateTime instance, DateTime unit)
		{
			return instance.CompareTo(unit) > 0;
		}

		public static bool IsSameDate(this DateTime? instance, DateTime? unit)
		{
			if (!instance.HasValue || !unit.HasValue)
			{
				throw new ArgumentNullException();
			}
			return instance.Value.IsSameDate(unit.Value.Date);
		}

		public static bool IsSameDate(this DateTime instance, DateTime unit)
		{
			return instance.Date.CompareTo(unit.Date) == 0;
		}

		public static bool IsBetween(this DateTime? instance, DateTime? lhs, DateTime? rhs)
		{
			if (!instance.HasValue || (!lhs.HasValue && !rhs.HasValue))
			{
				throw new ArgumentNullException();
			}
			if (lhs.HasValue && rhs.HasValue && lhs.IsAfter(rhs))
			{
				throw new ArgumentException("lhs cannot be After rhs");
			}

			if (!lhs.HasValue)
			{
				return instance.IsBefore(rhs) || instance.IsSameDate(rhs);
			}
			if (!rhs.HasValue)
			{
				return instance.IsAfter(lhs) || instance.IsSameDate(lhs);
			}
			return instance.Value.IsBetween(lhs.Value, rhs.Value);
		}

		public static bool IsBetween(this DateTime instance, DateTime lhs, DateTime rhs)
		{
			return (instance.IsAfter(lhs) || instance.IsSameDate(lhs))
			   && (instance.IsBefore(rhs) || instance.IsSameDate(rhs));

		}

		// https://github.com/dotnet/coreclr/blob/50ef79d48df81635e58ca59386620f0151df6022/src/mscorlib/src/System/DateTime.cs#L71
		private const int DaysPerYear = 365;
		private const int DaysPer4Years = DaysPerYear * 4 + 1;       // 1461
		private const int DaysPer100Years = DaysPer4Years * 25 - 1;  // 36524
		private const int DaysPer400Years = DaysPer100Years * 4 + 1; // 146097
		private const int DaysTo1970 = DaysPer400Years * 4 + DaysPer100Years * 3 + DaysPer4Years * 17 + DaysPerYear; // 719,162
		//https://github.com/dotnet/coreclr/blob/50ef79d48df81635e58ca59386620f0151df6022/src/mscorlib/src/System/DateTimeOffset.cs#L43
		private const long UnixEpochTicks = TimeSpan.TicksPerDay * /*DateTime.*/DaysTo1970; // 621,355,968,000,000,000
		private const long UnixEpochSeconds = UnixEpochTicks / TimeSpan.TicksPerSecond; // 62,135,596,800
		private const long UnixEpochMilliseconds = UnixEpochTicks / TimeSpan.TicksPerMillisecond; // 62,135,596,800,000

		public static long ToUnixTimeSeconds(this DateTime instance)
		{
			var seconds = instance.ToUniversalTime().Ticks / TimeSpan.TicksPerSecond;
			return seconds - UnixEpochSeconds;
		}

		public static long ToUnixTimeMilliseconds(this DateTime instance)
		{
			var milliseconds = instance.ToUniversalTime().Ticks / TimeSpan.TicksPerMillisecond;
			return milliseconds - UnixEpochMilliseconds;
		}

#if !WINDOWS_UAP
		//These method is not required under UAP, since it's now in the platform
		//https://github.com/dotnet/coreclr/blob/50ef79d48df81635e58ca59386620f0151df6022/src/mscorlib/src/System/DateTimeOffset.cs#L623
		public static long ToUnixTimeSeconds(this DateTimeOffset instance)
		{
			// Truncate sub-second precision before offsetting by the Unix Epoch to avoid 
			// the last digit being off by one for dates that result in negative Unix times. 
			// 
			// For example, consider the DateTimeOffset 12/31/1969 12:59:59.001 +0 
			//   ticks            = 621355967990010000 
			//   ticksFromEpoch   = ticks - UnixEpochTicks                   = -9990000 
			//   secondsFromEpoch = ticksFromEpoch / TimeSpan.TicksPerSecond = 0 
			// 
			// Notice that secondsFromEpoch is rounded *up* by the truncation induced by integer division, 
			// whereas we actually always want to round *down* when converting to Unix time. This happens 
			// automatically for positive Unix time values. Now the example becomes: 
			//   seconds          = ticks / TimeSpan.TicksPerSecond = 62135596799 
			//   secondsFromEpoch = seconds - UnixEpochSeconds      = -1 
			// 
			// In other words, we want to consistently round toward the time 1/1/0001 00:00:00, 
			// rather than toward the Unix Epoch (1/1/1970 00:00:00). 

			var seconds = instance.UtcDateTime.Ticks/TimeSpan.TicksPerSecond;
			return seconds - UnixEpochSeconds;
		}

		public static long ToUnixTimeMilliseconds(this DateTimeOffset instance)
		{
			// Truncate sub-millisecond precision before offsetting by the Unix Epoch to avoid
			// the last digit being off by one for dates that result in negative Unix times
			long milliseconds = instance.UtcDateTime.Ticks / TimeSpan.TicksPerMillisecond;
			return milliseconds - UnixEpochMilliseconds;
		}
#endif

		/// <summary>
		/// Creates a DateTimeOffset from a standard Unix timestamp and offset
		/// </summary>
		/// <param name="seconds">Number of seconds since the 1970/01/01 00:00 UTC</param>
		/// <param name="offset">Offset of the DateTimeOffset</param>
		/// <returns></returns>
		public static DateTimeOffset FromUnixTimeSeconds(long seconds, TimeSpan offset)
		{
			return new DateTimeOffset((seconds + UnixEpochSeconds + (long)offset.TotalSeconds) * TimeSpan.TicksPerSecond, offset);
		}

		/// <summary>
		/// Creates a DateTimeOffset from a standard Unix timestamp and offset
		/// </summary>
		/// <param name="seconds">Number of seconds since the 1970/01/01 00:00 UTC</param>
		/// <param name="offset">Offset of the DateTimeOffset</param>
		/// <returns></returns>
		public static DateTimeOffset FromUnixTimeMilliseconds(long milliseconds, TimeSpan offset)
		{
			return new DateTimeOffset((milliseconds + UnixEpochMilliseconds + (long)offset.TotalMilliseconds) * TimeSpan.TicksPerMillisecond, offset);
		}
	}
}