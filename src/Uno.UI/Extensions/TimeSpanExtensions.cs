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
    }
}
