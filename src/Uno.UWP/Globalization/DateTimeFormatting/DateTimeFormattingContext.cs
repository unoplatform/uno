#nullable enable

using System.Globalization;

namespace Windows.Globalization.DateTimeFormatting;

/// <summary>
/// Context for formatting date/time values, containing all the necessary
/// culture, calendar, clock, and timezone information for pattern-based formatting.
/// </summary>
internal sealed class DateTimeFormattingContext
{
	public DateTimeFormattingContext(
		CultureInfo culture,
		string calendarIdentifier,
		string clockIdentifier,
		string? timeZoneId = null)
	{
		Culture = culture;
		CalendarIdentifier = calendarIdentifier;
		ClockIdentifier = clockIdentifier;
		TimeZoneId = timeZoneId;
		IsTwelveHourClock = clockIdentifier == ClockIdentifiers.TwelveHour;
		Calendar = new Calendar(
			[culture.Name],
			calendarIdentifier,
			clockIdentifier);
	}

	/// <summary>
	/// The culture to use for formatting.
	/// </summary>
	public CultureInfo Culture { get; }

	/// <summary>
	/// The calendar identifier (e.g., GregorianCalendar).
	/// </summary>
	public string CalendarIdentifier { get; }

	/// <summary>
	/// The clock identifier (12HourClock or 24HourClock).
	/// </summary>
	public string ClockIdentifier { get; }

	/// <summary>
	/// Optional timezone ID for formatting.
	/// </summary>
	public string? TimeZoneId { get; }

	/// <summary>
	/// True if the clock is 12-hour, false if 24-hour.
	/// </summary>
	public bool IsTwelveHourClock { get; }

	/// <summary>
	/// A Calendar instance configured with the context settings.
	/// </summary>
	public Calendar Calendar { get; }
}
