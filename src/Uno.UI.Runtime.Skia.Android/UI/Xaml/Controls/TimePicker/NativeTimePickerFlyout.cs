using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Globalization;

namespace Microsoft.UI.Xaml.Controls;

internal partial class NativeTimePickerFlyout
{
	internal const long DEFAULT_TIME_TICKS = -1;

	protected TimeSpan GetCurrentTime()
	{
		var calendar = new Calendar();
		calendar.SetToNow();
		var currentDateTime = calendar.GetDateTime();
		return new TimeSpan(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second);
	}
}
