using System;
using Windows.Globalization;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls;

internal partial class NativeTimePickerFlyout : TimePickerFlyout
{
	protected TimeSpan GetCurrentTime()
	{
		var calendar = new Calendar();
		calendar.SetToNow();
		var currentDateTime = calendar.GetDateTime();
		return new TimeSpan(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second);
	}
}
