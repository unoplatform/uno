using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.Extensions
{
	public static class TimeSpanExtensions
	{
		public static string ToXamlString(this TimeSpan timeSpan, IFormatProvider provider)
		{
			var builder = new StringBuilder();

			if (timeSpan.Days > 0)
			{
				builder.AppendFormat(provider, "{0}.", timeSpan.Days);
			}

			builder.AppendFormat(provider, "{0:D2}:{1:D2}:{2:d2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);

			if (timeSpan.Milliseconds > 0)
			{
				builder.AppendFormat(provider, ".{0:D3}", timeSpan.Milliseconds);
			}

			return builder.ToString();
		}

		/// <summary>
		/// Round timespan to previous minute interval
		/// input time seconds are ignored
		/// </summary>
		/// <param name="interval">minute interval</param>
		/// <returns>Rounded timespan</returns>
		internal static TimeSpan RoundToPreviousMinuteInterval(this TimeSpan time, int interval)
		{
			if (interval > 0 && time != TimeSpan.Zero)
			{
				var roundedMinutes = Math.Floor(Math.Truncate(time.TotalMinutes) / interval) * interval;
				return TimeSpan.FromMinutes(roundedMinutes);
			}

			return time;
		}

		/// <summary>
		/// Round timespan to next minute interval
		/// input time seconds are ignored
		/// </summary>
		/// <param name="interval">minute interval</param>
		/// <returns>Rounded timespan</returns>
		internal static TimeSpan RoundToNextMinuteInterval(this TimeSpan time, int interval)
		{
			if (interval > 0 && time != TimeSpan.Zero)
			{
				var roundedMinutes = Math.Ceiling(Math.Truncate(time.TotalMinutes) / interval) * interval;
				return TimeSpan.FromMinutes(roundedMinutes);
			}

			return time;
		}

		/// <summary>
		/// Round timespan to minute interval
		/// input time seconds are ignored
		/// </summary>
		/// <param name="interval">minute interval</param>
		/// <returns>Rounded timespan</returns>
		internal static TimeSpan RoundToMinuteInterval(this TimeSpan time, int interval)
		{
			if (interval > 0 && time != TimeSpan.Zero)
			{
				var roundedMinutes = Math.Round(Math.Truncate(time.TotalMinutes) / interval) * interval;
				return TimeSpan.FromMinutes(roundedMinutes);
			}

			return time;
		}

		/// <summary>
		/// Normalize TimeSpan between 0 and 24h inclusive
		/// </summary>
		/// <returns>Normalized TimeSpan</returns>
		internal static TimeSpan NormalizeToDay(this TimeSpan value)
		{
			var day = TimeSpan.FromDays(1);

			while (value < TimeSpan.Zero)
			{
				value += day;
			}

			while (value > day)
			{
				value -= day;
			}

			return value;
		}
	}
}
