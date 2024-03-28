#nullable enable
using System;
using System.Globalization;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml.Media.Animation;
using Windows.Web.Syndication;
using WG = Windows.Globalization;

namespace Uno.UI.Tests.Windows_Globalization
{
	[TestClass]
	public class When_Calendar
	{
		[TestMethod]
		public void When_DateTimeOffset_Is_Next_Day_If_Converted_To_Utc()
		{
			var calendar = new WG.Calendar();
			var offset = new DateTimeOffset(year: 2023, month: 5, day: 1, hour: 21, minute: 0, second: 0, TimeSpan.FromHours(-5));
			calendar.SetDateTime(offset);
			Assert.AreEqual(2, calendar.Day);
			Assert.AreEqual(2, offset.UtcDateTime.Day);

			var comparer = new DirectUI.DateComparer();
			comparer.SetCalendarForComparison(calendar);
			var result = comparer.CompareDay(offset, new DateTimeOffset(year: 2023, month: 5, day: 1, hour: 4, minute: 0, second: 0, TimeSpan.FromHours(0)));
			Assert.AreEqual(1, result);
		}

		[TestMethod]
		public void When_Gregorian_FixedDate()
		{
			Validate(
				culture: "en-US",
				calendar: WG.CalendarIdentifiers.Gregorian,
				clock: WG.ClockIdentifiers.TwentyFourHour,
				new DateTimeOffset(2020, 01, 02, 03, 04, 05, 0, TimeSpan.Zero),
				year: 2020,
				month: 01,
				day: 02,
				hours: 03,
				minutes: 04,
				seconds: 05,
				milliseconds: 0,
				offsetInSeconds: 0,
				dayOfWeek: DayOfWeek.Thursday,
				era: 1,
				firstDayInThisMonth: 1,
				firstEra: 1,
				firstHourInThisPeriod: 0,
				firstMinuteInThisHour: 0,
				firstMonthInThisYear: 1,
				firstPeriodInThisDay: 1,
				firstSecondInThisMinute: 0,
				firstYearInThisEra: 1,
				isDaylightSavingTime: false,
				lastDayInThisMonth: 31,
				lastEra: 1,
				lastHourInThisPeriod: 23,
				lastMinuteInThisHour: 59,
				lastMonthInThisYear: 12,
				lastPeriodInThisDay: 1,
				lastSecondInThisMinute: 59,
				lastYearInThisEra: 9999,
				numberOfEras: 1,
				numberOfHoursInThisPeriod: 24,
				numberOfMinutesInThisHour: 60,
				numberOfDaysInThisMonth: 31,
				numberOfMonthsInThisYear: 12,
				numberOfPeriodsInThisDay: 1,
				numberOfSecondsInThisMinute: 60,
				numberOfYearsInThisEra: 9998,
				numeralSystem: "",
				period: 1);

			Validate(
				culture: "en-US",
				calendar: WG.CalendarIdentifiers.Gregorian,
				clock: WG.ClockIdentifiers.TwentyFourHour,
				new DateTimeOffset(2020, 08, 02, 03, 04, 05, 00, TimeSpan.Zero),
				year: 2020,
				month: 08,
				day: 02,
				hours: 03,
				minutes: 04,
				seconds: 05,
				milliseconds: 0,
				offsetInSeconds: 0,
				dayOfWeek: DayOfWeek.Sunday,
				era: 1,
				firstDayInThisMonth: 1,
				firstEra: 1,
				firstHourInThisPeriod: 0,
				firstMinuteInThisHour: 0,
				firstMonthInThisYear: 1,
				firstPeriodInThisDay: 1,
				firstSecondInThisMinute: 0,
				firstYearInThisEra: 1,
				isDaylightSavingTime: true,
				lastDayInThisMonth: 31,
				lastEra: 1,
				lastHourInThisPeriod: 23,
				lastMinuteInThisHour: 59,
				lastMonthInThisYear: 12,
				lastPeriodInThisDay: 1,
				lastSecondInThisMinute: 59,
				lastYearInThisEra: 9999,
				numberOfEras: 1,
				numberOfHoursInThisPeriod: 24,
				numberOfMinutesInThisHour: 60,
				numberOfDaysInThisMonth: 31,
				numberOfMonthsInThisYear: 12,
				numberOfPeriodsInThisDay: 1,
				numberOfSecondsInThisMinute: 60,
				numberOfYearsInThisEra: 9998,
				numeralSystem: "",
				period: 1);
		}

		[TestMethod]
		public void When_Gregorian_FixedDate_TwelveHours()
		{
			Validate(
				culture: "en-US",
				calendar: WG.CalendarIdentifiers.Gregorian,
				clock: WG.ClockIdentifiers.TwelveHour,
				new DateTimeOffset(2020, 01, 02, 23, 04, 05, 00, TimeSpan.Zero),
				year: 2020,
				month: 01,
				day: 02,
				hours: 11,
				minutes: 04,
				seconds: 05,
				milliseconds: 0,
				offsetInSeconds: 0,
				dayOfWeek: DayOfWeek.Thursday,
				era: 1,
				firstDayInThisMonth: 1,
				firstEra: 1,
				firstHourInThisPeriod: 12,
				firstMinuteInThisHour: 0,
				firstMonthInThisYear: 1,
				firstPeriodInThisDay: 1,
				firstSecondInThisMinute: 0,
				firstYearInThisEra: 1,
				isDaylightSavingTime: false,
				lastDayInThisMonth: 31,
				lastEra: 1,
				lastHourInThisPeriod: 11,
				lastMinuteInThisHour: 59,
				lastMonthInThisYear: 12,
				lastPeriodInThisDay: 2,
				lastSecondInThisMinute: 59,
				lastYearInThisEra: 9999,
				numberOfEras: 1,
				numberOfHoursInThisPeriod: 12,
				numberOfMinutesInThisHour: 60,
				numberOfDaysInThisMonth: 31,
				numberOfMonthsInThisYear: 12,
				numberOfPeriodsInThisDay: 2,
				numberOfSecondsInThisMinute: 60,
				numberOfYearsInThisEra: 9998,
				numeralSystem: "",
				period: 2);

			Validate(
				culture: "en-US",
				calendar: WG.CalendarIdentifiers.Gregorian,
				clock: WG.ClockIdentifiers.TwelveHour,
				new DateTimeOffset(2020, 08, 02, 23, 04, 05, 00, TimeSpan.Zero),
				year: 2020,
				month: 08,
				day: 02,
				hours: 11,
				minutes: 04,
				seconds: 05,
				milliseconds: 0,
				offsetInSeconds: 0,
				dayOfWeek: DayOfWeek.Sunday,
				era: 1,
				firstDayInThisMonth: 1,
				firstEra: 1,
				firstHourInThisPeriod: 12,
				firstMinuteInThisHour: 0,
				firstMonthInThisYear: 1,
				firstPeriodInThisDay: 1,
				firstSecondInThisMinute: 0,
				firstYearInThisEra: 1,
				isDaylightSavingTime: true,
				lastDayInThisMonth: 31,
				lastEra: 1,
				lastHourInThisPeriod: 11,
				lastMinuteInThisHour: 59,
				lastMonthInThisYear: 12,
				lastPeriodInThisDay: 2,
				lastSecondInThisMinute: 59,
				lastYearInThisEra: 9999,
				numberOfEras: 1,
				numberOfHoursInThisPeriod: 12,
				numberOfMinutesInThisHour: 60,
				numberOfDaysInThisMonth: 31,
				numberOfMonthsInThisYear: 12,
				numberOfPeriodsInThisDay: 2,
				numberOfSecondsInThisMinute: 60,
				numberOfYearsInThisEra: 9998,
				numeralSystem: "",
				period: 2);
		}

		[TestMethod]
		public void When_Gregorian_FixedDate_AddSecond()
		{
			Validate(
				culture: "en-US",
				calendar: WG.CalendarIdentifiers.Gregorian,
				clock: WG.ClockIdentifiers.TwelveHour,
				new DateTimeOffset(2020, 01, 02, 23, 04, 05, 00, TimeSpan.Zero),
				year: 2020,
				month: 01,
				day: 02,
				hours: 11,
				minutes: 04,
				seconds: 06,
				milliseconds: 0,
				offsetInSeconds: 0,
				dayOfWeek: DayOfWeek.Thursday,
				era: 1,
				firstDayInThisMonth: 1,
				firstEra: 1,
				firstHourInThisPeriod: 12,
				firstMinuteInThisHour: 0,
				firstMonthInThisYear: 1,
				firstPeriodInThisDay: 1,
				firstSecondInThisMinute: 0,
				firstYearInThisEra: 1,
				isDaylightSavingTime: false,
				lastDayInThisMonth: 31,
				lastEra: 1,
				lastHourInThisPeriod: 11,
				lastMinuteInThisHour: 59,
				lastMonthInThisYear: 12,
				lastPeriodInThisDay: 2,
				lastSecondInThisMinute: 59,
				lastYearInThisEra: 9999,
				numberOfEras: 1,
				numberOfHoursInThisPeriod: 12,
				numberOfMinutesInThisHour: 60,
				numberOfDaysInThisMonth: 31,
				numberOfMonthsInThisYear: 12,
				numberOfPeriodsInThisDay: 2,
				numberOfSecondsInThisMinute: 60,
				numberOfYearsInThisEra: 9998,
				numeralSystem: "",
				period: 2,
				c => c.AddSeconds(1)
			);
		}

		[TestMethod]
		public void When_Gregorian_FixedDate_AddMinute()
		{
			Validate(
				culture: "en-US",
				calendar: WG.CalendarIdentifiers.Gregorian,
				clock: WG.ClockIdentifiers.TwelveHour,
				new DateTimeOffset(2020, 01, 02, 23, 04, 05, 00, TimeSpan.Zero),
				year: 2020,
				month: 01,
				day: 02,
				hours: 11,
				minutes: 05,
				seconds: 05,
				milliseconds: 0,
				offsetInSeconds: 0,
				dayOfWeek: DayOfWeek.Thursday,
				era: 1,
				firstDayInThisMonth: 1,
				firstEra: 1,
				firstHourInThisPeriod: 12,
				firstMinuteInThisHour: 0,
				firstMonthInThisYear: 1,
				firstPeriodInThisDay: 1,
				firstSecondInThisMinute: 0,
				firstYearInThisEra: 1,
				isDaylightSavingTime: false,
				lastDayInThisMonth: 31,
				lastEra: 1,
				lastHourInThisPeriod: 11,
				lastMinuteInThisHour: 59,
				lastMonthInThisYear: 12,
				lastPeriodInThisDay: 2,
				lastSecondInThisMinute: 59,
				lastYearInThisEra: 9999,
				numberOfEras: 1,
				numberOfHoursInThisPeriod: 12,
				numberOfMinutesInThisHour: 60,
				numberOfDaysInThisMonth: 31,
				numberOfMonthsInThisYear: 12,
				numberOfPeriodsInThisDay: 2,
				numberOfSecondsInThisMinute: 60,
				numberOfYearsInThisEra: 9998,
				numeralSystem: "",
				period: 2,
				c => c.AddMinutes(1)
			);
		}

		[TestMethod]
		public void When_Gregorian_FixedDate_AddHour()
		{
			Validate(
				culture: "en-US",
				calendar: WG.CalendarIdentifiers.Gregorian,
				clock: WG.ClockIdentifiers.TwelveHour,
				new DateTimeOffset(2020, 01, 02, 23, 04, 05, 00, TimeSpan.Zero),
				year: 2020,
				month: 01,
				day: 03,
				hours: 12,
				minutes: 04,
				seconds: 05,
				milliseconds: 0,
				offsetInSeconds: 0,
				dayOfWeek: DayOfWeek.Friday,
				era: 1,
				firstDayInThisMonth: 1,
				firstEra: 1,
				firstHourInThisPeriod: 12,
				firstMinuteInThisHour: 0,
				firstMonthInThisYear: 1,
				firstPeriodInThisDay: 1,
				firstSecondInThisMinute: 0,
				firstYearInThisEra: 1,
				isDaylightSavingTime: false,
				lastDayInThisMonth: 31,
				lastEra: 1,
				lastHourInThisPeriod: 11,
				lastMinuteInThisHour: 59,
				lastMonthInThisYear: 12,
				lastPeriodInThisDay: 2,
				lastSecondInThisMinute: 59,
				lastYearInThisEra: 9999,
				numberOfEras: 1,
				numberOfHoursInThisPeriod: 12,
				numberOfMinutesInThisHour: 60,
				numberOfDaysInThisMonth: 31,
				numberOfMonthsInThisYear: 12,
				numberOfPeriodsInThisDay: 2,
				numberOfSecondsInThisMinute: 60,
				numberOfYearsInThisEra: 9998,
				numeralSystem: "",
				period: 1,
				c => c.AddHours(1)
			);
		}

		[TestMethod]
		public void When_Gregorian_FixedDate_AddDay()
		{
			Validate(
				culture: "en-US",
				calendar: WG.CalendarIdentifiers.Gregorian,
				clock: WG.ClockIdentifiers.TwelveHour,
				new DateTimeOffset(2020, 01, 02, 23, 04, 05, 00, TimeSpan.Zero),
				year: 2020,
				month: 01,
				day: 03,
				hours: 11,
				minutes: 04,
				seconds: 05,
				milliseconds: 0,
				offsetInSeconds: 0,
				dayOfWeek: DayOfWeek.Friday,
				era: 1,
				firstDayInThisMonth: 1,
				firstEra: 1,
				firstHourInThisPeriod: 12,
				firstMinuteInThisHour: 0,
				firstMonthInThisYear: 1,
				firstPeriodInThisDay: 1,
				firstSecondInThisMinute: 0,
				firstYearInThisEra: 1,
				isDaylightSavingTime: false,
				lastDayInThisMonth: 31,
				lastEra: 1,
				lastHourInThisPeriod: 11,
				lastMinuteInThisHour: 59,
				lastMonthInThisYear: 12,
				lastPeriodInThisDay: 2,
				lastSecondInThisMinute: 59,
				lastYearInThisEra: 9999,
				numberOfEras: 1,
				numberOfHoursInThisPeriod: 12,
				numberOfMinutesInThisHour: 60,
				numberOfDaysInThisMonth: 31,
				numberOfMonthsInThisYear: 12,
				numberOfPeriodsInThisDay: 2,
				numberOfSecondsInThisMinute: 60,
				numberOfYearsInThisEra: 9998,
				numeralSystem: "",
				period: 2,
				c => c.AddDays(1)
			);
		}

		[TestMethod]
		public void When_Gregorian_FixedDate_AddMonth()
		{
			Validate(
				culture: "en-US",
				calendar: WG.CalendarIdentifiers.Gregorian,
				clock: WG.ClockIdentifiers.TwelveHour,
				new DateTimeOffset(2020, 01, 02, 23, 04, 05, 00, TimeSpan.Zero),
				year: 2020,
				month: 02,
				day: 02,
				hours: 11,
				minutes: 04,
				seconds: 05,
				milliseconds: 0,
				offsetInSeconds: 0,
				dayOfWeek: DayOfWeek.Sunday,
				era: 1,
				firstDayInThisMonth: 1,
				firstEra: 1,
				firstHourInThisPeriod: 12,
				firstMinuteInThisHour: 0,
				firstMonthInThisYear: 1,
				firstPeriodInThisDay: 1,
				firstSecondInThisMinute: 0,
				firstYearInThisEra: 1,
				isDaylightSavingTime: false,
				lastDayInThisMonth: 29,
				lastEra: 1,
				lastHourInThisPeriod: 11,
				lastMinuteInThisHour: 59,
				lastMonthInThisYear: 12,
				lastPeriodInThisDay: 2,
				lastSecondInThisMinute: 59,
				lastYearInThisEra: 9999,
				numberOfEras: 1,
				numberOfHoursInThisPeriod: 12,
				numberOfMinutesInThisHour: 60,
				numberOfDaysInThisMonth: 29,
				numberOfMonthsInThisYear: 12,
				numberOfPeriodsInThisDay: 2,
				numberOfSecondsInThisMinute: 60,
				numberOfYearsInThisEra: 9998,
				numeralSystem: "",
				period: 2,
				c => c.AddMonths(1)
			);
		}

		[TestMethod]
		public void When_Gregorian_FixedDate_AddYear()
		{
			Validate(
				culture: "en-US",
				calendar: WG.CalendarIdentifiers.Gregorian,
				clock: WG.ClockIdentifiers.TwelveHour,
				new DateTimeOffset(2020, 01, 02, 23, 04, 05, 00, TimeSpan.Zero),
				year: 2021,
				month: 01,
				day: 02,
				hours: 11,
				minutes: 04,
				seconds: 05,
				milliseconds: 0,
				offsetInSeconds: 0,
				dayOfWeek: DayOfWeek.Saturday,
				era: 1,
				firstDayInThisMonth: 1,
				firstEra: 1,
				firstHourInThisPeriod: 12,
				firstMinuteInThisHour: 0,
				firstMonthInThisYear: 1,
				firstPeriodInThisDay: 1,
				firstSecondInThisMinute: 0,
				firstYearInThisEra: 1,
				isDaylightSavingTime: false,
				lastDayInThisMonth: 31,
				lastEra: 1,
				lastHourInThisPeriod: 11,
				lastMinuteInThisHour: 59,
				lastMonthInThisYear: 12,
				lastPeriodInThisDay: 2,
				lastSecondInThisMinute: 59,
				lastYearInThisEra: 9999,
				numberOfEras: 1,
				numberOfHoursInThisPeriod: 12,
				numberOfMinutesInThisHour: 60,
				numberOfDaysInThisMonth: 31,
				numberOfMonthsInThisYear: 12,
				numberOfPeriodsInThisDay: 2,
				numberOfSecondsInThisMinute: 60,
				numberOfYearsInThisEra: 9998,
				numeralSystem: "",
				period: 2,
				c => c.AddYears(1)
			);
		}

		[TestMethod]
		[DataRow(2020, "Thursday")]
		[DataRow(2021, "Saturday")]
		public void When_Gregorian_FixedDate_Format_12h(int year, string dayOfWeek)
		{
			ValidateFormat(culture: "en-US",
				  calendar: WG.CalendarIdentifiers.Gregorian,
				  clock: WG.ClockIdentifiers.TwelveHour,
				  date: new DateTimeOffset(year, 01, 02, 23, 04, 05, 00, TimeSpan.Zero),
				  yearAsPaddedString: year.ToString(CultureInfo.InvariantCulture),
				  yearAsString: year.ToString(CultureInfo.InvariantCulture),
				  monthAsPaddedNumericString: "01",
				  monthAsSoloString: "Jan",
				  monthAsString: "Jan",
				  monthAsNumericString: "1",
				  dayAsPaddedString: "02",
				  dayAsString: "2",
				  hourAsPaddedString: "11",
				  hourAsString: "11",
				  minuteAsPaddedString: "04",
				  minuteAsString: "4",
				  secondAsPaddedString: "05",
				  secondAsString: "5",
				  nanosecondAsPaddedString: "00",
				  nanosecondAsString: "0",
				  dayOfWeekAsSoloString: dayOfWeek,
				  dayOfWeekAsString: dayOfWeek);
		}

		[TestMethod]
		[DataRow(2020, "Thursday")]
		[DataRow(2021, "Saturday")]
		public void When_Gregorian_FixedDate_Format_24h(int year, string dayOfWeek)
		{
			ValidateFormat(culture: "en-US",
				  calendar: WG.CalendarIdentifiers.Gregorian,
				  clock: WG.ClockIdentifiers.TwentyFourHour,
				  date: new DateTimeOffset(year, 01, 02, 23, 04, 05, 00, TimeSpan.Zero),
				  yearAsPaddedString: year.ToString(CultureInfo.InvariantCulture),
				  yearAsString: year.ToString(CultureInfo.InvariantCulture),
				  monthAsPaddedNumericString: "01",
				  monthAsSoloString: "Jan",
				  monthAsString: "Jan",
				  monthAsNumericString: "1",
				  dayAsPaddedString: "02",
				  dayAsString: "2",
				  hourAsPaddedString: "23",
				  hourAsString: "23",
				  minuteAsPaddedString: "04",
				  minuteAsString: "4",
				  secondAsPaddedString: "05",
				  secondAsString: "5",
				  nanosecondAsPaddedString: "00",
				  nanosecondAsString: "0",
				  dayOfWeekAsSoloString: dayOfWeek,
				  dayOfWeekAsString: dayOfWeek);
		}

		private void Validate(
			string culture,
			string calendar,
			string clock,
			DateTimeOffset date,
			int year,
			int month,
			int day,
			int hours,
			int minutes,
			int seconds,
			int milliseconds,
			int offsetInSeconds,
			DayOfWeek dayOfWeek,
			int era,
			int firstDayInThisMonth,
			int firstEra,
			int firstHourInThisPeriod,
			int firstMinuteInThisHour,
			int firstMonthInThisYear,
			int firstPeriodInThisDay,
			int firstSecondInThisMinute,
			int firstYearInThisEra,
			bool isDaylightSavingTime,
			int lastDayInThisMonth,
			int lastEra,
			int lastHourInThisPeriod,
			int lastMinuteInThisHour,
			int lastMonthInThisYear,
			int lastPeriodInThisDay,
			int lastSecondInThisMinute,
			int lastYearInThisEra,
			int numberOfEras,
			int numberOfHoursInThisPeriod,
			int numberOfMinutesInThisHour,
			int numberOfDaysInThisMonth,
			int numberOfMonthsInThisYear,
			int numberOfPeriodsInThisDay,
			int numberOfSecondsInThisMinute,
			int numberOfYearsInThisEra,
			string numeralSystem,
			int period,
			Action<WG.Calendar>? mutator = null
		)
		{
			var SUT = new WG.Calendar(new[] { culture }, calendar, clock);

			SUT.SetDateTime(date);

			mutator?.Invoke(SUT);

			using (new AssertionScope("Calendar Properties"))
			{
				SUT.Day.Should().Be(day, nameof(day));
				SUT.Month.Should().Be(month, nameof(month));
				SUT.Year.Should().Be(year, nameof(year));
				SUT.Hour.Should().Be(hours, nameof(hours));
				SUT.Minute.Should().Be(minutes, nameof(minutes));
				SUT.Second.Should().Be(seconds, nameof(seconds));
				SUT.Nanosecond.Should().Be(milliseconds * 1000, nameof(milliseconds));
				SUT.DayOfWeek.Should().Be(dayOfWeek, nameof(dayOfWeek));
				SUT.Era.Should().Be(era, nameof(era));
				SUT.FirstDayInThisMonth.Should().Be(firstDayInThisMonth, nameof(firstDayInThisMonth));
				SUT.FirstEra.Should().Be(firstEra, nameof(firstEra));
				SUT.FirstHourInThisPeriod.Should().Be(firstHourInThisPeriod, nameof(firstHourInThisPeriod));
				SUT.FirstMinuteInThisHour.Should().Be(firstMinuteInThisHour, nameof(firstMinuteInThisHour));
				SUT.FirstMonthInThisYear.Should().Be(firstMonthInThisYear, nameof(firstMonthInThisYear));
				SUT.FirstPeriodInThisDay.Should().Be(firstPeriodInThisDay, nameof(firstPeriodInThisDay));
				SUT.FirstSecondInThisMinute.Should().Be(firstSecondInThisMinute, nameof(firstSecondInThisMinute));
				SUT.FirstYearInThisEra.Should().Be(firstYearInThisEra, nameof(firstYearInThisEra));
				SUT.Languages.Should().HaveCount(1, "languages count");
				SUT.Languages.Should().HaveElementAt(0, culture, nameof(culture));
				SUT.LastDayInThisMonth.Should().Be(lastDayInThisMonth, nameof(lastDayInThisMonth));
				SUT.LastEra.Should().Be(lastEra, nameof(lastEra));
				SUT.LastHourInThisPeriod.Should().Be(lastHourInThisPeriod, nameof(lastHourInThisPeriod));
				SUT.LastMinuteInThisHour.Should().Be(lastMinuteInThisHour, nameof(lastMinuteInThisHour));
				SUT.LastMonthInThisYear.Should().Be(lastMonthInThisYear, nameof(lastMonthInThisYear));
				SUT.LastPeriodInThisDay.Should().Be(lastPeriodInThisDay, nameof(lastPeriodInThisDay));
				SUT.LastSecondInThisMinute.Should().Be(lastSecondInThisMinute, nameof(lastSecondInThisMinute));
				SUT.LastYearInThisEra.Should().Be(lastYearInThisEra, nameof(lastYearInThisEra));
				SUT.NumberOfDaysInThisMonth.Should().Be(numberOfDaysInThisMonth, nameof(numberOfDaysInThisMonth));
				SUT.NumberOfEras.Should().Be(numberOfEras, nameof(numberOfEras));
				SUT.NumberOfHoursInThisPeriod.Should().Be(numberOfHoursInThisPeriod, nameof(numberOfHoursInThisPeriod));
				SUT.NumberOfMinutesInThisHour.Should().Be(numberOfMinutesInThisHour, nameof(numberOfMinutesInThisHour));
				SUT.NumberOfMonthsInThisYear.Should().Be(numberOfMonthsInThisYear, nameof(numberOfMonthsInThisYear));
				SUT.NumberOfPeriodsInThisDay.Should().Be(numberOfPeriodsInThisDay, nameof(numberOfPeriodsInThisDay));
				SUT.NumberOfSecondsInThisMinute.Should().Be(numberOfSecondsInThisMinute, nameof(numberOfSecondsInThisMinute));
				SUT.NumberOfYearsInThisEra.Should().Be(numberOfYearsInThisEra, nameof(numberOfYearsInThisEra));
				SUT.Period.Should().Be(period, nameof(period));
				SUT.ResolvedLanguage.Should().Be(culture, nameof(culture));

				// Validation is disabled as timezone support is only using the current machine's timezone
				// SUT.IsDaylightSavingTime.Should().Be(isDaylightSavingTime, "isDaylightSavingTime");

				// Not yet supported.
				// SUT.NumeralSystem.Should().Be(numeralSystem, "numeralSystem");
			}
		}

		private void ValidateFormat(
			string culture,
			string calendar,
			string clock,
			DateTimeOffset date,
			string yearAsPaddedString,
			string yearAsString,
			string monthAsPaddedNumericString,
			string monthAsSoloString,
			string monthAsString,
			string monthAsNumericString,
			string dayAsPaddedString,
			string dayAsString,
			string hourAsPaddedString,
			string hourAsString,
			string minuteAsPaddedString,
			string minuteAsString,
			string secondAsPaddedString,
			string secondAsString,
			string nanosecondAsPaddedString,
			string nanosecondAsString,
			string dayOfWeekAsSoloString,
			string dayOfWeekAsString
			)
		{
			var SUT = new WG.Calendar(new[] { culture }, calendar, clock);

			SUT.SetDateTime(date);

			using (new AssertionScope("Calendar Format"))
			{
				SUT.YearAsPaddedString(2).Should().Be(yearAsPaddedString, nameof(yearAsPaddedString));
				SUT.YearAsString().Should().Be(yearAsString, nameof(yearAsString));
				SUT.MonthAsPaddedNumericString(2).Should().Be(monthAsPaddedNumericString, nameof(monthAsPaddedNumericString));
				SUT.MonthAsSoloString().Should().Be(monthAsSoloString, nameof(monthAsSoloString));
				SUT.MonthAsString().Should().Be(monthAsString, nameof(monthAsString));
				SUT.MonthAsNumericString().Should().Be(monthAsNumericString, nameof(monthAsNumericString));
				SUT.DayAsPaddedString(2).Should().Be(dayAsPaddedString, nameof(dayAsPaddedString));
				SUT.DayAsString().Should().Be(dayAsString, nameof(dayAsString));
				SUT.HourAsPaddedString(2).Should().Be(hourAsPaddedString, nameof(hourAsPaddedString));
				SUT.HourAsString().Should().Be(hourAsString, nameof(hourAsString));
				SUT.MinuteAsPaddedString(2).Should().Be(minuteAsPaddedString, nameof(minuteAsPaddedString));
				SUT.MinuteAsString().Should().Be(minuteAsString, nameof(minuteAsString));
				SUT.SecondAsPaddedString(2).Should().Be(secondAsPaddedString, nameof(secondAsPaddedString));
				SUT.SecondAsString().Should().Be(secondAsString, nameof(secondAsString));
				SUT.NanosecondAsPaddedString(2).Should().Be(nanosecondAsPaddedString, nameof(nanosecondAsPaddedString));
				SUT.NanosecondAsString().Should().Be(nanosecondAsString, nameof(nanosecondAsString));
				SUT.DayOfWeekAsSoloString().Should().Be(dayOfWeekAsSoloString, nameof(dayOfWeekAsSoloString));
				SUT.DayOfWeekAsString().Should().Be(dayOfWeekAsString, nameof(dayOfWeekAsString));
			}
		}

		[TestMethod]
		[DynamicData(nameof(CalendarsData))]
		public void TestCalendarLimits(string calendar)
		{
			using var _ = new AssertionScope();

			var sut = new WG.Calendar(new[] { "en" }, WG.CalendarIdentifiers.Japanese, "24HourClock");

			sut.SetToMin();
			CheckLimits($"Min");

			sut.SetToMax();
			CheckLimits($"Max");

			sut.SetToNow();
			CheckLimits($"Max");

			void CheckLimits(string context)
			{
				// Following tests are just to ensure no exceptions are raised
				// by asking those values
				sut.NumberOfEras.Should().BePositive(context);
				sut.Era.Should().BePositive(context);
				sut.FirstMonthInThisYear.Should().BePositive(context);
				sut.NumberOfDaysInThisMonth.Should().BePositive(context);
				sut.DayOfWeek.Should().NotBeNull(context);
				sut.NumberOfEras.Should().BePositive(context);
				sut.FirstEra.Should().BePositive(context);
				sut.LastEra.Should().BePositive(context);
				sut.Period.Should().BePositive(context);
				sut.ResolvedLanguage.Should().NotBeEmpty(context);
			}
		}

		private static System.Collections.Generic.IEnumerable<object[]> CalendarsData
		{
			get
			{
				yield return new object[] { WG.CalendarIdentifiers.Julian };
				yield return new object[] { WG.CalendarIdentifiers.Gregorian };
				yield return new object[] { WG.CalendarIdentifiers.Hebrew };
				yield return new object[] { WG.CalendarIdentifiers.Hijri };
				yield return new object[] { WG.CalendarIdentifiers.Japanese };
				yield return new object[] { WG.CalendarIdentifiers.Korean };
				yield return new object[] { WG.CalendarIdentifiers.Taiwan };
				yield return new object[] { WG.CalendarIdentifiers.Thai };
				yield return new object[] { WG.CalendarIdentifiers.UmAlQura };
				yield return new object[] { WG.CalendarIdentifiers.Persian };
				yield return new object[] { WG.CalendarIdentifiers.ChineseLunar };
				yield return new object[] { WG.CalendarIdentifiers.VietnameseLunar };
				yield return new object[] { WG.CalendarIdentifiers.TaiwanLunar };
				yield return new object[] { WG.CalendarIdentifiers.KoreanLunar };
				yield return new object[] { WG.CalendarIdentifiers.JapaneseLunar };
			}
		}
	}
}

