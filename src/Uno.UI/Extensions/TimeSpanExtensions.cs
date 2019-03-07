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
		/// </summary>
		/// <param name="interval">minute interval</param>
		/// <returns>Rounded timespan</returns>
		internal static TimeSpan RoundToPreviousMinuteInterval(this TimeSpan time, int interval)
		{
			if (interval > 0 && time != TimeSpan.Zero)
			{
				var roundedMinutes = Math.Floor(time.TotalMinutes / interval) * interval;
				return TimeSpan.FromMinutes(roundedMinutes);
			}

			return time;
		}

		/// <summary>
		/// Round timespan to next minute interval
		/// </summary>
		/// <param name="interval">minute interval</param>
		/// <returns>Rounded timespan</returns>
		internal static TimeSpan RoundToNextMinuteInterval(this TimeSpan time, int interval)
		{
			if (interval > 0 && time != TimeSpan.Zero)
			{
				var roundedMinutes = Math.Ceiling(time.TotalMinutes / interval) * interval;
				return TimeSpan.FromMinutes(roundedMinutes);
			}

			return time;
		}

		/// <summary>
		/// Round timespan to minute interval
		/// </summary>
		/// <param name="interval">minute interval</param>
		/// <returns>Rounded timespan</returns>
		internal static TimeSpan RoundToMinuteInterval(this TimeSpan time, int interval)
		{
			if (interval > 0 && time != TimeSpan.Zero)
			{
				var roundedMinutes = Math.Round(time.TotalMinutes / interval) * interval;
				return TimeSpan.FromMinutes(roundedMinutes);
			}

			return time;
		}
	}
}
