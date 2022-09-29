#nullable disable

using System;
using System.Collections.Generic;
using System.Text;
using Foundation;

namespace Uno.UI.Extensions
{
	public static class NSLocaleExtensions
	{
		public static bool Has24HourTimeFormat(this NSLocale locale)
		{
			var formatter = new NSDateFormatter();
			formatter.Locale = locale;
			formatter.DateStyle = NSDateFormatterStyle.None;
			formatter.TimeStyle = NSDateFormatterStyle.Short;
			var dateString = NSDateFormatter.ToLocalizedString(DateTime.Now.ToNSDate(), NSDateFormatterStyle.None, NSDateFormatterStyle.Short);

			return !dateString.Contains(formatter.AMSymbol) && !dateString.Contains(formatter.PMSymbol);
		}
	}
}
