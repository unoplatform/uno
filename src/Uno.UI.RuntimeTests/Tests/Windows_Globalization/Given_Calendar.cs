using System;
using Windows.Globalization;

namespace Uno.UI.RuntimeTests.Tests.Windows_Globalization;

[TestClass]
public class Given_Calendar
{
	[TestMethod]
	public void When_Julian_Calendar()
	{
		// It's very important that this test doesn't crash, because that's what CalendarView does when using Julian calendar.
		// NOTE: Running this test on WinUI will crash!!
		// However, the crash happens when transitioning from Windows.Foundation.DateTime to System.DateTime (due to negative ticks).
		// So, the same code in C++ won't crash, which is why we care about this test as CalendarView is ported from C++.
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Julian, ClockIdentifiers.TwelveHour);
		calendar.SetToMin();

		// Doesn't match Windows, but to avoid crashes with CalendarView
		Assert.AreEqual(new DateTime(2, 1, 1), calendar.GetDateTime().Date);

		Assert.AreEqual(3, calendar.Day);

		calendar.Day = 1;
		Assert.AreEqual(1, calendar.Day);
	}

	[TestMethod]
	public void When_Julian_Calendar_Day_Name()
	{
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Julian, ClockIdentifiers.TwelveHour);
		calendar.SetDateTime(new DateTimeOffset(new DateTime(2024, 2, 29)));

		Assert.AreEqual("16", calendar.DayAsString());
		Assert.AreEqual(DateTimeKind.Unspecified, calendar.GetDateTime().DateTime.Kind);
	}

	[TestMethod]
	[DataRow("JulianCalendar", "16", -1)]
	[DataRow("JulianCalendar", "16", 0)]
	[DataRow("JulianCalendar", "15", 1)]
	[DataRow("GregorianCalendar", "29", -1)]
	[DataRow("GregorianCalendar", "29", 0)]
	[DataRow("GregorianCalendar", "28", 1)]
#if RUNTIME_NATIVE_AOT
	[Ignore("DataRowAttribute.GetData() wraps data in an extra array under NativeAOT; not yet understood why.")]
#endif  // RUNTIME_NATIVE_AOT
	public void When_Calendar_Unspecified_DateTimeKind_Different_Offsets(string identifier, string expectedDayAsString, double offset)
	{
		if (TimeZoneInfo.Local.IsDaylightSavingTime(DateTimeOffset.Now))
		{
			// Don't attempt to change the specific datetime below to DateTime.Now or something similar. This test
			// tackles a very specific regression and changes might not capture the problem.
			Assert.Inconclusive("Local timezones with daylight saving can crash the DateTimeOffset constructor.");
		}

		offset += DateTimeOffset.Now.Offset.TotalHours;

		var calendar = new Calendar(new[] { "en-US" }, identifier, ClockIdentifiers.TwelveHour);
		var dateTime = new DateTimeOffset(new DateTime(2024, 2, 29, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.FromHours(offset));
		calendar.SetDateTime(dateTime);

		Assert.AreEqual(expectedDayAsString, calendar.DayAsString());
		Assert.IsTrue(dateTime.Equals(calendar.GetDateTime()));
		Assert.AreEqual(DateTimeKind.Unspecified, calendar.GetDateTime().DateTime.Kind);
		Assert.AreEqual(DateTimeOffset.Now.Offset, calendar.GetDateTime().Offset);
	}

	[TestMethod]
	[DataRow("JulianCalendar", "16")]
	[DataRow("GregorianCalendar", "29")]
	public void When_Calendar_Local_DateTimeKind(string identifier, string expectedDayAsString)
	{
		if (TimeZoneInfo.Local.IsDaylightSavingTime(DateTimeOffset.Now))
		{
			// Don't attempt to change the specific datetime below to DateTime.Now or something similar. This test
			// tackles a very specific regression and changes might not capture the problem.
			Assert.Inconclusive("Local timezones with daylight saving can crash the DateTimeOffset constructor.");
		}
		var calendar = new Calendar(new[] { "en-US" }, identifier, ClockIdentifiers.TwelveHour);
		var offset = DateTimeOffset.Now.Offset;
		var dateTime = new DateTimeOffset(new DateTime(2024, 2, 29, 0, 0, 0, DateTimeKind.Local), offset);
		calendar.SetDateTime(dateTime);

		Assert.AreEqual(expectedDayAsString, calendar.DayAsString());
		Assert.IsTrue(dateTime.Equals(calendar.GetDateTime()));
		Assert.AreEqual(DateTimeKind.Unspecified, calendar.GetDateTime().DateTime.Kind);
		Assert.AreEqual(DateTimeOffset.Now.Offset, calendar.GetDateTime().Offset);
	}

	[TestMethod]
	[DataRow("JulianCalendar", "16")]
	[DataRow("GregorianCalendar", "29")]
	public void When_Calendar_Utc_DateTimeKind(string identifier, string expectedDayAsString)
	{
		if (TimeZoneInfo.Local.IsDaylightSavingTime(DateTimeOffset.Now))
		{
			// Don't attempt to change the specific datetime below to DateTime.Now or something similar. This test
			// tackles a very specific regression and changes might not capture the problem.
			Assert.Inconclusive("Local timezones with daylight saving fail the offset assert.");
		}

		var calendar = new Calendar(new[] { "en-US" }, identifier, ClockIdentifiers.TwelveHour);
		var dateTime = new DateTimeOffset(new DateTime(2024, 2, 29, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);
		calendar.SetDateTime(dateTime);

		Assert.AreEqual(expectedDayAsString, calendar.DayAsString());
		Assert.IsTrue(dateTime.Equals(calendar.GetDateTime()));
		Assert.AreEqual(DateTimeKind.Unspecified, calendar.GetDateTime().DateTime.Kind);
		Assert.AreEqual(DateTimeOffset.Now.Offset, calendar.GetDateTime().Offset);
	}

	[TestMethod]
	public void When_Switch_Period()
	{
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
		calendar.Period = 1;
		calendar.Hour = 1;
		Assert.AreEqual(1, calendar.Hour);
		Assert.AreEqual(1, calendar.Period);
		var dateTime = calendar.GetDateTime();
		Assert.AreEqual(1, dateTime.Hour);

		calendar.Period = 2;
		Assert.AreEqual(1, calendar.Hour);
		Assert.AreEqual(2, calendar.Period);
		dateTime = calendar.GetDateTime();
		Assert.AreEqual(13, dateTime.Hour);

		calendar.Hour = 11;
		calendar.Period = 1;
		Assert.AreEqual(11, calendar.Hour);
		Assert.AreEqual(1, calendar.Period);
		dateTime = calendar.GetDateTime();
		Assert.AreEqual(11, dateTime.Hour);

		calendar.Period = 2;
		Assert.AreEqual(11, calendar.Hour);
		Assert.AreEqual(2, calendar.Period);
		dateTime = calendar.GetDateTime();
		Assert.AreEqual(23, dateTime.Hour);

		calendar.Hour = 12;
		calendar.Period = 1;
		Assert.AreEqual(12, calendar.Hour);
		Assert.AreEqual(1, calendar.Period);
		dateTime = calendar.GetDateTime();
		Assert.AreEqual(0, dateTime.Hour);

		calendar.Period = 2;
		Assert.AreEqual(12, calendar.Hour);
		Assert.AreEqual(2, calendar.Period);
		dateTime = calendar.GetDateTime();
		Assert.AreEqual(12, dateTime.Hour);
	}

	[TestMethod]
	public void When_TwentyFourHour_Invalid_Hours()
	{
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Gregorian, ClockIdentifiers.TwentyFourHour);
		Assert.ThrowsExactly<ArgumentException>(() => calendar.Hour = -1);
		Assert.ThrowsExactly<ArgumentException>(() => calendar.Hour = 24);
	}

	[TestMethod]
	public void When_TwentyFourHour_Invalid_Period()
	{
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Gregorian, ClockIdentifiers.TwentyFourHour);
		Assert.ThrowsExactly<ArgumentException>(() => calendar.Period = 2);
	}

	[TestMethod]
	public void When_TwelveHour_Invalid_Period()
	{
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
		Assert.ThrowsExactly<ArgumentException>(() => calendar.Period = 0);
		Assert.ThrowsExactly<ArgumentException>(() => calendar.Period = 3);
	}

	[TestMethod]
	public void When_TwentyFourHour()
	{
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Gregorian, ClockIdentifiers.TwentyFourHour);
		for (int hour = 0; hour <= 23; hour++)
		{
			calendar.Hour = hour;
			Assert.AreEqual(hour, calendar.Hour);
			var dateTime = calendar.GetDateTime();
			Assert.AreEqual(hour, dateTime.Hour);
		}
	}

	[TestMethod]
	public void When_TwelveHour_Invalid_Hours()
	{
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
		Assert.ThrowsExactly<ArgumentException>(() => calendar.Hour = 0);
		for (int hour = 13; hour <= 23; hour++)
		{
			Assert.ThrowsExactly<ArgumentException>(() => calendar.Hour = hour);
		}
	}

	[TestMethod]
	public void When_TwelveHour_First_Period()
	{
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
		calendar.Period = 1;
		for (int hour = 1; hour <= 12; hour++)
		{
			calendar.Hour = hour;
			Assert.AreEqual(hour, calendar.Hour);
			Assert.AreEqual(1, calendar.Period);
			var dateTime = calendar.GetDateTime();
			if (hour == 12)
			{
				Assert.AreEqual(0, dateTime.Hour);
			}
			else
			{
				Assert.AreEqual(hour, dateTime.Hour);
			}
		}
	}

	[TestMethod]
	public void When_TwelveHour_Second_Period()
	{
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
		calendar.Period = 2;
		for (int hour = 1; hour <= 12; hour++)
		{
			calendar.Hour = hour;
			Assert.AreEqual(hour, calendar.Hour);
			Assert.AreEqual(2, calendar.Period);
			var dateTime = calendar.GetDateTime();
			if (hour == 12)
			{
				Assert.AreEqual(12, dateTime.Hour);
			}
			else
			{
				Assert.AreEqual(hour + 12, dateTime.Hour);
			}
		}
	}
}
