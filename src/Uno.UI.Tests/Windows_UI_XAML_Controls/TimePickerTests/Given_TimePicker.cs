using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Uno.UI.Tests.TimePickerTests;

[TestClass]
public class Given_TimePicker
{
	[TestMethod]
	public void When_Time_Or_SelectedTime_Changes()
	{
		var log = string.Empty;
		var timePicker = new TimePicker();
		Assert.AreEqual(new TimeSpan(-1), timePicker.Time);
		Assert.IsNull(timePicker.SelectedTime);
		timePicker.SelectedTimeChanged += TimePicker_SelectedTimeChanged;
		timePicker.TimeChanged += TimePicker_TimeChanged;

		timePicker.SelectedTime = new TimeSpan(8, 13, 0);
		Assert.AreEqual(new TimeSpan(8, 13, 0), timePicker.Time);
		Assert.AreEqual(new TimeSpan(8, 13, 0), timePicker.SelectedTime);

		timePicker.SelectedTime = null;
		Assert.AreEqual(new TimeSpan(-1), timePicker.Time);
		Assert.IsNull(timePicker.SelectedTime);

		timePicker.Time = new TimeSpan(8, 17, 49);
		Assert.AreEqual(new TimeSpan(8, 17, 0), timePicker.Time);
		Assert.AreEqual(new TimeSpan(8, 17, 0), timePicker.SelectedTime);

		timePicker.SelectedTime = null;
		Assert.AreEqual(new TimeSpan(-1), timePicker.Time);
		Assert.IsNull(timePicker.SelectedTime);

		timePicker.SelectedTime = new TimeSpan(8, 17, 49);
		Assert.AreEqual(new TimeSpan(8, 17, 0), timePicker.Time);
		Assert.AreEqual(new TimeSpan(8, 17, 49), timePicker.SelectedTime);
		Assert.AreEqual("""

			TimeChanged changed from -00:00:00.0000001 to 08:13:00
			SelectedTimeChanged changed from <null> to 08:13:00

			TimeChanged changed from 08:13:00 to -00:00:00.0000001
			SelectedTimeChanged changed from 08:13:00 to <null>

			TimeChanged changed from 08:17:49 to 08:17:00
			SelectedTimeChanged changed from 08:17:49 to 08:17:00

			TimeChanged changed from 08:17:00 to -00:00:00.0000001
			SelectedTimeChanged changed from 08:17:00 to <null>

			TimeChanged changed from 08:17:49 to 08:17:00
			SelectedTimeChanged changed from 08:17:49 to 08:17:00

			""", log);

		void TimePicker_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
		{
			log += $"\r\nTimeChanged changed from {e.OldTime} to {e.NewTime}\r\n";
		}

		void TimePicker_SelectedTimeChanged(TimePicker sender, TimePickerSelectedValueChangedEventArgs e)
		{
			log += $"SelectedTimeChanged changed from {e.OldTime?.ToString() ?? "<null>"} to {e.NewTime?.ToString() ?? "<null>"}\r\n";
		}
	}
}
