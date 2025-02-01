#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

namespace Windows.Globalization.DateTimeFormatting;

// We are kinda combining a lexer and a parser here as the grammar is simple enough.
internal sealed class TemplateParser
{
	private int _index;
	private string _template;

	public bool Completed => _index >= _template.Length;

	public DateTimeTemplateInfo Info { get; } = new();

	public TemplateParser(string template)
	{
		_template = template;
	}

	private TemplateRootNode EnsureCompleted(TemplateRootNode root)
	{
		if (!Completed)
		{
			throw new ArgumentException($"Failed to parse '{_template}'. Couldn't consume all characters. Stopped at index '{_index}'.");
		}

		root.Traverse(Info);
		return root;
	}

	public TemplateRootNode Parse()
	{
		_ = SkipWhitespace();
		if (TryParseDate(out var date))
		{
			bool hasWhitespace = SkipWhitespace();
			if (hasWhitespace && date is TemplateSpecificDateNode or TemplateRelativeDateNode &&
				TryParseTime(out var time))
			{
				_ = SkipWhitespace();
				return EnsureCompleted(new TemplateRootNode(date, time));
			}

			return EnsureCompleted(new TemplateRootNode(date));
		}
		else if (TryParseTime(out var time))
		{
			bool hasWhitespace = SkipWhitespace();
			if (hasWhitespace)
			{
				if (TryParseSpecificDate(out var specificDate))
				{
					_ = SkipWhitespace();
					return EnsureCompleted(new TemplateRootNode(time, date));
				}
				else if (TryParseRelativeDate(out var relativeDate))
				{
					_ = SkipWhitespace();
					return EnsureCompleted(new TemplateRootNode(time, date));
				}
			}

			return EnsureCompleted(new TemplateRootNode(time));
		}

		throw new ArgumentException($"Failed to parse date time template '{_template}'");
	}

	private bool TryParseTime([NotNullWhen(true)] out TemplateTimeNode? time)
	{
		var originalIndex = _index;
		if (TryParseLongTime(out var longTime))
		{
			time = new TemplateTimeNode(longTime);
			return true;
		}
		else if (TryParseShortTime(out var shortTime))
		{
			time = new TemplateTimeNode(shortTime);
			return true;
		}
		else if (TryParseTimeZone(out var timeZone))
		{
			if (SkipWhitespace() && TryParseHour(out var hour))
			{
				time = new TemplateTimeNode(timeZone, hour);
				return true;
			}
		}
		else if (TryParseHour(out var hour))
		{
			int originalIndexAfterHour = _index;
			if (SkipWhitespace() && TryParseTimeZone(out timeZone))
			{
				time = new TemplateTimeNode(hour, timeZone);
				return true;
			}

			_index = originalIndexAfterHour;
			time = new TemplateTimeNode(hour);
			return true;
		}

		_index = originalIndex;
		time = null;
		return false;
	}

	private bool TryParseLongTime([NotNullWhen(true)] out TemplateLongTimeNode? longTime)
	{
		if (IsWordAt(_index, "longtime"))
		{
			_index += "longtime".Length;
			longTime = TemplateLongTimeNode.DefaultLongTimeInstance;
			return true;
		}

		if (TryParseFourComponentsSeparatedByWhitespaceInAnyOrder<TemplateTimeZoneNode, TemplateHourNode, TemplateMinuteNode, TemplateSecondNode, TemplateNode, TemplateLongTimeNode>(
			TryParseTimeZone,
			TryParseHour,
			TryParseMinute,
			TryParseSecond,
			static (first, second, third, forth) => new TemplateLongTimeNode(first, second, third, forth),
			out longTime))
		{
			return true;
		}

		if (TryParseThreeComponentsSeparatedByWhitespaceInAnyOrder<TemplateHourNode, TemplateMinuteNode, TemplateSecondNode, TemplateNode, TemplateLongTimeNode>(
			TryParseHour,
			TryParseMinute,
			TryParseSecond,
			static (first, second, third) => new TemplateLongTimeNode(first, second, third),
			out longTime))
		{
			return true;
		}

		return false;
	}

	private bool TryParseShortTime([NotNullWhen(true)] out TemplateShortTimeNode? shortTime)
	{
		if (IsWordAt(_index, "shorttime"))
		{
			_index += "shorttime".Length;
			shortTime = TemplateShortTimeNode.DefaultShortTimeInstance;
			return true;
		}

		if (TryParseThreeComponentsSeparatedByWhitespaceInAnyOrder<TemplateTimeZoneNode, TemplateHourNode, TemplateMinuteNode, TemplateNode, TemplateShortTimeNode>(
			TryParseTimeZone,
			TryParseHour,
			TryParseMinute,
			static (first, second, third) => new TemplateShortTimeNode(first, second, third),
			out shortTime))
		{
			return true;
		}

		return TryParseTwoComponentsSeparatedByWhitespaceInAnyOrder<TemplateHourNode, TemplateMinuteNode, TemplateNode, TemplateShortTimeNode>(
			TryParseHour,
			TryParseMinute,
			static (first, second) => new TemplateShortTimeNode(first, second),
			out shortTime);
	}

	private bool TryParseDate([NotNullWhen(true)] out TemplateDateNode? date)
	{
		// The order of calls here matter.
		if (TryParseSpecificDate(out var specificDate))
		{
			date = specificDate;
			return true;
		}
		else if (TryParseRelativeDate(out var relativeDate))
		{
			date = relativeDate;
			return true;
		}
		else if (TryParseMonthYear(out var monthYear))
		{
			date = monthYear;
			return true;
		}
		else if (TryParseYear(out var year))
		{
			date = year;
			return true;
		}
		else if (TryParseMonth(out var month))
		{
			date = month;
			return true;
		}
		else if (TryParseDay(out var day))
		{
			date = day;
			return true;
		}

		date = null;
		return false;
	}

	private bool TryParseMonthYear([NotNullWhen(true)] out TemplateMonthYearNode? monthYear)
	{
		return TryParseTwoComponentsSeparatedByWhitespaceInAnyOrder<TemplateMonthNode, TemplateYearNode, TemplateNode, TemplateMonthYearNode>(
			TryParseMonth,
			TryParseYear,
			static (first, second) => new TemplateMonthYearNode(first, second),
			out monthYear);
	}

	private bool TryParseRelativeDate([NotNullWhen(true)] out TemplateRelativeDateNode? relativeDate)
	{
		if (TryParseDayOfWeek(out var dayOfWeek))
		{
			relativeDate = dayOfWeek;
			return true;
		}
		else if (TryParseMonthDay(out var monthDay))
		{
			relativeDate = monthDay;
			return true;
		}
		else if (TryParseRelativeLongDate(out var relativeLongDate))
		{
			relativeDate = relativeLongDate;
			return true;
		}

		relativeDate = null;
		return false;
	}

	private bool TryParseRelativeLongDate([NotNullWhen(true)] out TemplateRelativeLongDateNode? date)
	{
		return TryParseThreeComponentsSeparatedByWhitespaceInAnyOrder<TemplateMonthNode, TemplateDayNode, TemplateDayOfWeekNode, TemplateDateNode, TemplateRelativeLongDateNode>(
			TryParseMonth,
			TryParseDay,
			TryParseDayOfWeek,
			static (first, second, third) => new TemplateRelativeLongDateNode(first, second, third),
			out date);
	}

	private bool TryParseMonthDay([NotNullWhen(true)] out TemplateMonthDayNode? date)
	{
		return TryParseTwoComponentsSeparatedByWhitespaceInAnyOrder<TemplateMonthNode, TemplateDayNode, TemplateDateNode, TemplateMonthDayNode>(
			TryParseMonth,
			TryParseDay,
			static (first, second) => new TemplateMonthDayNode(first, second),
			out date
			);
	}

	private bool TryParseSpecificDate([NotNullWhen(true)] out TemplateSpecificDateNode? date)
	{
		if (TryParseLongDate(out var longDate))
		{
			date = longDate;
			return true;
		}
		else if (TryParseShortDate(out var shortDate))
		{
			date = shortDate;
			return true;
		}

		date = null;
		return false;
	}

	private bool TryParseLongDate([NotNullWhen(true)] out TemplateLongDateNode? longDate)
	{
		if (IsWordAt(_index, "longdate"))
		{
			_index += "longdate".Length;
			longDate = TemplateLongDateNode.DefaultLongDateInstance;
			return true;
		}

		return TryParseFourComponentsSeparatedByWhitespaceInAnyOrder<TemplateYearNode, TemplateMonthNode, TemplateDayNode, TemplateDayOfWeekNode, TemplateDateNode, TemplateLongDateNode>(
			TryParseYear,
			TryParseMonth,
			TryParseDay,
			TryParseDayOfWeek,
			static (first, second, third, forth) => new TemplateLongDateNode(first, second, third, forth),
			out longDate);
	}

	private bool TryParseShortDate([NotNullWhen(true)] out TemplateShortDateNode? shortDate)
	{
		if (IsWordAt(_index, "shortdate"))
		{
			_index += "shortdate".Length;
			shortDate = TemplateShortDateNode.DefaultShortDateInstance;
			return true;
		}

		return TryParseThreeComponentsSeparatedByWhitespaceInAnyOrder<TemplateMonthNode, TemplateDayNode, TemplateYearNode, TemplateDateNode, TemplateShortDateNode>(
			TryParseMonth,
			TryParseDay,
			TryParseYear,
			static (first, second, third) => new TemplateShortDateNode(first, second, third),
			out shortDate);
	}

	private bool TryParseYear([NotNullWhen(true)] out TemplateYearNode? year)
	{
		if (IsWordAt(_index, "year.full"))
		{
			_index += "year.full".Length;
			year = TemplateYearNode.FullInstance;
			return true;
		}
		else if (IsWordAt(_index, "year.abbreviated"))
		{
			_index += "year.abbreviated".Length;
			year = TemplateYearNode.AbbreviatedInstance;
			return true;
		}
		else if (IsWordAt(_index, "year"))
		{
			_index += "year".Length;
			year = TemplateYearNode.NormalInstance;
			return true;
		}

		year = null;
		return false;
	}

	private bool TryParseMonth([NotNullWhen(true)] out TemplateMonthNode? month)
	{
		if (IsWordAt(_index, "month.full"))
		{
			_index += "month.full".Length;
			month = TemplateMonthNode.FullInstance;
			return true;
		}
		else if (IsWordAt(_index, "month.abbreviated"))
		{
			_index += "month.abbreviated".Length;
			month = TemplateMonthNode.AbbreviatedInstance;
			return true;
		}
		else if (IsWordAt(_index, "month.numeric"))
		{
			_index += "month.numeric".Length;
			month = TemplateMonthNode.NumericInstance;
			return true;
		}
		else if (IsWordAt(_index, "month"))
		{
			_index += "month".Length;
			month = TemplateMonthNode.NormalInstance;
			return true;
		}

		month = null;
		return false;
	}

	private bool TryParseDay([NotNullWhen(true)] out TemplateDayNode? day)
	{
		if (IsWordAt(_index, "day"))
		{
			_index += "day".Length;
			day = TemplateDayNode.Instance;
			return true;
		}

		day = null;
		return false;
	}

	private bool TryParseDayOfWeek([NotNullWhen(true)] out TemplateDayOfWeekNode? dayOfWeek)
	{
		if (IsWordAt(_index, "dayofweek.full"))
		{
			_index += "dayofweek.full".Length;
			dayOfWeek = TemplateDayOfWeekNode.FullInstance;
			return true;
		}
		else if (IsWordAt(_index, "dayofweek.abbreviated"))
		{
			_index += "dayofweek.abbreviated".Length;
			dayOfWeek = TemplateDayOfWeekNode.AbbreviatedInstance;
			return true;
		}
		else if (IsWordAt(_index, "dayofweek"))
		{
			_index += "dayofweek".Length;
			dayOfWeek = TemplateDayOfWeekNode.NormalInstance;
			return true;
		}

		dayOfWeek = null;
		return false;
	}

	private bool TryParseHour([NotNullWhen(true)] out TemplateHourNode? hour)
	{
		if (IsWordAt(_index, "hour"))
		{
			_index += "hour".Length;
			hour = TemplateHourNode.Instance;
			return true;
		}

		hour = null;
		return false;
	}

	private bool TryParseMinute([NotNullWhen(true)] out TemplateMinuteNode? minute)
	{
		if (IsWordAt(_index, "minute"))
		{
			_index += "minute".Length;
			minute = TemplateMinuteNode.Instance;
			return true;
		}

		minute = null;
		return false;
	}

	private bool TryParseSecond([NotNullWhen(true)] out TemplateSecondNode? second)
	{
		if (IsWordAt(_index, "second"))
		{
			_index += "second".Length;
			second = TemplateSecondNode.Instance;
			return true;
		}

		second = null;
		return false;
	}

	private bool TryParseTimeZone([NotNullWhen(true)] out TemplateTimeZoneNode? timeZone)
	{
		if (IsWordAt(_index, "timezone.full"))
		{
			_index += "timezone.full".Length;
			timeZone = TemplateTimeZoneNode.FullInstance;
			return true;
		}
		else if (IsWordAt(_index, "timezone.abbreviated"))
		{
			_index += "timezone.abbreviated".Length;
			timeZone = TemplateTimeZoneNode.AbbreviatedInstance;
			return true;
		}
		else if (IsWordAt(_index, "timezone"))
		{
			_index += "timezone".Length;
			timeZone = TemplateTimeZoneNode.NormalInstance;
			return true;
		}

		timeZone = null;
		return false;
	}

	private bool SkipWhitespace()
	{
		bool hasWhitespace = false;
		while (_index < _template.Length && _template[_index] == ' ')
		{
			hasWhitespace = true;
			_index++;
		}

		return hasWhitespace;
	}

	private bool IsWordAt(int i, string word)
	{
		int wordLength = word.Length;
		if (i + wordLength > _template.Length)
		{
			return false;
		}

		return _template.AsSpan().Slice(i, wordLength).SequenceEqual(word);
	}

	internal delegate bool TryParseDelegate<T>([NotNullWhen(true)] out T? result);

	private bool TryParseThreeComponentsSeparatedByWhitespaceInExactOrder<T1, T2, T3, TCommon, TResult>(
		TryParseDelegate<T1> tryParseFirst,
		TryParseDelegate<T2> tryParseSecond,
		TryParseDelegate<T3> tryParseThird,
		Func<TCommon, TCommon, TCommon, TResult> resultCreator,
		[NotNullWhen(true)] out TResult? result
		)
		where T1 : TCommon
		where T2 : TCommon
		where T3 : TCommon
		where TResult : class
		where TCommon : class
	{
		int originalIndex = _index;
		if (tryParseFirst(out var first))
		{
			if (SkipWhitespace())
			{
				if (tryParseSecond(out var second))
				{
					if (SkipWhitespace() && tryParseThird(out var third))
					{
						result = resultCreator(first, second, third);
						return true;
					}
				}
				else if (tryParseThird(out var third))
				{
					if (SkipWhitespace() && tryParseSecond(out second))
					{
						result = resultCreator(first, third, second);
						return true;
					}
				}
			}
		}

		_index = originalIndex;
		result = null;
		return false;
	}

	private bool TryParseThreeComponentsSeparatedByWhitespaceInAnyOrder<T1, T2, T3, TCommon, TResult>(
		TryParseDelegate<T1> tryParseFirst,
		TryParseDelegate<T2> tryParseSecond,
		TryParseDelegate<T3> tryParseThird,
		Func<TCommon, TCommon, TCommon, TResult> resultCreator,
		[NotNullWhen(true)] out TResult? result
		)
		where T1 : TCommon
		where T2 : TCommon
		where T3 : TCommon
		where TResult : class
		where TCommon : class
	{
		return
			TryParseThreeComponentsSeparatedByWhitespaceInExactOrder(tryParseFirst, tryParseSecond, tryParseThird, resultCreator, out result) ||
			TryParseThreeComponentsSeparatedByWhitespaceInExactOrder(tryParseFirst, tryParseThird, tryParseSecond, resultCreator, out result) ||
			TryParseThreeComponentsSeparatedByWhitespaceInExactOrder(tryParseSecond, tryParseFirst, tryParseThird, resultCreator, out result) ||
			TryParseThreeComponentsSeparatedByWhitespaceInExactOrder(tryParseSecond, tryParseThird, tryParseFirst, resultCreator, out result) ||
			TryParseThreeComponentsSeparatedByWhitespaceInExactOrder(tryParseThird, tryParseFirst, tryParseSecond, resultCreator, out result) ||
			TryParseThreeComponentsSeparatedByWhitespaceInExactOrder(tryParseThird, tryParseSecond, tryParseFirst, resultCreator, out result);
	}

	private bool TryParseThreeComponentsSeparatedByWhitespaceInAnyOrder<T1, T2, T3, TCommon, TResult>(
		TCommon firstArgument,
		TryParseDelegate<T1> tryParseFirst,
		TryParseDelegate<T2> tryParseSecond,
		TryParseDelegate<T3> tryParseThird,
		Func<TCommon, TCommon, TCommon, TCommon, TResult> resultCreator,
		[NotNullWhen(true)] out TResult? result
		)
		where T1 : TCommon
		where T2 : TCommon
		where T3 : TCommon
		where TResult : class
		where TCommon : class
	{
		return
			TryParseThreeComponentsSeparatedByWhitespaceInExactOrder<T1, T2, T3, TCommon, TResult>(tryParseFirst, tryParseSecond, tryParseThird, (first, second, third) => resultCreator(firstArgument, first, second, third), out result) ||
			TryParseThreeComponentsSeparatedByWhitespaceInExactOrder<T1, T3, T2, TCommon, TResult>(tryParseFirst, tryParseThird, tryParseSecond, (first, second, third) => resultCreator(firstArgument, first, second, third), out result) ||
			TryParseThreeComponentsSeparatedByWhitespaceInExactOrder<T2, T1, T3, TCommon, TResult>(tryParseSecond, tryParseFirst, tryParseThird, (first, second, third) => resultCreator(firstArgument, first, second, third), out result) ||
			TryParseThreeComponentsSeparatedByWhitespaceInExactOrder<T2, T3, T1, TCommon, TResult>(tryParseSecond, tryParseThird, tryParseFirst, (first, second, third) => resultCreator(firstArgument, first, second, third), out result) ||
			TryParseThreeComponentsSeparatedByWhitespaceInExactOrder<T3, T1, T2, TCommon, TResult>(tryParseThird, tryParseFirst, tryParseSecond, (first, second, third) => resultCreator(firstArgument, first, second, third), out result) ||
			TryParseThreeComponentsSeparatedByWhitespaceInExactOrder<T3, T2, T1, TCommon, TResult>(tryParseThird, tryParseSecond, tryParseFirst, (first, second, third) => resultCreator(firstArgument, first, second, third), out result);
	}

	private bool TryParseTwoComponentsSeparatedByWhitespaceInExactOrder<T1, T2, TCommon, TResult>(
		TryParseDelegate<T1> tryParseFirst,
		TryParseDelegate<T2> tryParseSecond,
		Func<TCommon, TCommon, TResult> resultCreator,
		[NotNullWhen(true)] out TResult? result
		)
		where T1 : TCommon
		where T2 : TCommon
		where TResult : class
		where TCommon : class
	{
		int originalIndex = _index;
		if (tryParseFirst(out var first) && SkipWhitespace() && tryParseSecond(out var second))
		{
			result = resultCreator(first, second);
			return true;
		}

		_index = originalIndex;
		result = null;
		return false;
	}

	private bool TryParseTwoComponentsSeparatedByWhitespaceInAnyOrder<T1, T2, TCommon, TResult>(
		TryParseDelegate<T1> tryParseFirst,
		TryParseDelegate<T2> tryParseSecond,
		Func<TCommon, TCommon, TResult> resultCreator,
		[NotNullWhen(true)] out TResult? result
		)
		where T1 : TCommon
		where T2 : TCommon
		where TResult : class
		where TCommon : class
	{
		return
			TryParseTwoComponentsSeparatedByWhitespaceInExactOrder(tryParseFirst, tryParseSecond, resultCreator, out result) ||
			TryParseTwoComponentsSeparatedByWhitespaceInExactOrder(tryParseSecond, tryParseFirst, resultCreator, out result);
	}

	private bool TryParseFourComponentsSeparatedByWhitespaceInAnyOrder<T1, T2, T3, T4, TCommon, TResult>(
		TryParseDelegate<T1> tryParseFirst,
		TryParseDelegate<T2> tryParseSecond,
		TryParseDelegate<T3> tryParseThird,
		TryParseDelegate<T4> tryParseForth,
		Func<TCommon, TCommon, TCommon, TCommon, TResult> resultCreator,
		[NotNullWhen(true)] out TResult? result
		)
		where T1 : TCommon
		where T2 : TCommon
		where T3 : TCommon
		where T4 : TCommon
		where TResult : class
		where TCommon : class
	{
		int originalIndex = _index;
		if (tryParseFirst(out var first))
		{
			if (SkipWhitespace() &&
				TryParseThreeComponentsSeparatedByWhitespaceInAnyOrder(first, tryParseSecond, tryParseThird, tryParseForth, resultCreator, out result))
			{
				return true;
			}
		}
		else if (tryParseSecond(out var second))
		{
			if (SkipWhitespace() &&
				TryParseThreeComponentsSeparatedByWhitespaceInAnyOrder(second, tryParseFirst, tryParseThird, tryParseForth, resultCreator, out result))
			{
				return true;
			}
		}
		if (tryParseThird(out var third))
		{
			if (SkipWhitespace() &&
				TryParseThreeComponentsSeparatedByWhitespaceInAnyOrder(third, tryParseFirst, tryParseSecond, tryParseForth, resultCreator, out result))
			{
				return true;
			}
		}
		if (tryParseForth(out var forth))
		{
			if (SkipWhitespace() &&
				TryParseThreeComponentsSeparatedByWhitespaceInAnyOrder(forth, tryParseFirst, tryParseSecond, tryParseThird, resultCreator, out result))
			{
				return true;
			}
		}

		_index = originalIndex;
		result = null;
		return false;
	}
}
