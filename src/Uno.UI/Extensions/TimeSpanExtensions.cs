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
		/// Round timespan to next minute interval
		/// </summary>
		/// <param name="interval">minute interval</param>
		/// <returns>Rounded timespan</returns>
		internal static TimeSpan RoundToNextMinuteInterval(this TimeSpan time, int interval)
		{
			var hours = time.Hours;
			var minutes = time.Minutes;
			var mod = minutes % interval;

			var roundedMinutes = mod == 0 ? minutes : minutes - mod + interval;
			minutes = roundedMinutes == 0 ? 0 : roundedMinutes % 60;

			var extraHour = roundedMinutes == 0 ? 0 : roundedMinutes / 60;
			hours = extraHour + hours;

			return new TimeSpan(hours, minutes, 0);
		}
	}
}
