using System;
using Windows.Globalization;

namespace Uno.UI.RuntimeTests.Tests.Windows_Globalization;

[TestClass]
public class Given_Calendar
{
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
		Assert.ThrowsException<ArgumentException>(() => calendar.Hour = -1);
		Assert.ThrowsException<ArgumentException>(() => calendar.Hour = 24);
	}

	[TestMethod]
	public void When_TwentyFourHour_Invalid_Period()
	{
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Gregorian, ClockIdentifiers.TwentyFourHour);
		Assert.ThrowsException<ArgumentException>(() => calendar.Period = 2);
	}

	[TestMethod]
	public void When_TwelveHour_Invalid_Period()
	{
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
		Assert.ThrowsException<ArgumentException>(() => calendar.Period = 0);
		Assert.ThrowsException<ArgumentException>(() => calendar.Period = 3);
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
		Assert.ThrowsException<ArgumentException>(() => calendar.Hour = 0);
		for (int hour = 13; hour <= 23; hour++)
		{
			Assert.ThrowsException<ArgumentException>(() => calendar.Hour = hour);
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

	[TestMethod]
	public void When_AddDays_Should_Respect_Daylight_Saving_Time()
	{
		// 25/09/1000 01:00:00 +03:00
		var dateTime = new DateTimeOffset(new DateTime(1000, 9, 25, 1, 0, 0, DateTimeKind.Unspecified), TimeSpan.FromHours(3));

		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Julian, ClockIdentifiers.TwelveHour);
		calendar.SetDateTime(dateTime);
		calendar.AddDays(1);

		var dateTimePlusDay = calendar.GetDateTime();

		// 26/09/1000 01:00:00 +02:00
		Assert.AreEqual(new DateTimeOffset(new DateTime(1000, 9, 26, 1, 0, 0, DateTimeKind.Unspecified), TimeSpan.FromHours(2)), dateTimePlusDay);
		Assert.AreNotEqual(dateTime.AddDays(1), dateTimePlusDay);
	}
}
