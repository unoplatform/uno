#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Windows.Globalization.DateTimeFormatting;

// We are kinda combining a lexer and a parser here as the grammar is simple enough.
internal sealed class PatternParser
{
	private int _index;
	private string _pattern;

	public bool Completed => _index >= _pattern.Length;

	public PatternParser(string pattern)
	{
		_pattern = pattern;
	}

	public PatternRootNode Parse()
	{
		PatternLiteralTextNode? prefixLiteralText = ParseLiteralText();
		PatternDateTimeNode dateTimeNode = ParseDateTimePattern();
		object? suffix = TryParseLiteralTextOrPattern();
		if (suffix is null)
		{
			return new PatternRootNode(prefixLiteralText, dateTimeNode, (PatternLiteralTextNode?)null);
		}
		else if (suffix is PatternLiteralTextNode suffixLiteralText)
		{
			return new PatternRootNode(prefixLiteralText, dateTimeNode, suffixLiteralText);
		}
		else if (suffix is PatternRootNode suffixPattern)
		{
			return new PatternRootNode(prefixLiteralText, dateTimeNode, suffixPattern);
		}
		else
		{
			throw new InvalidOperationException("Unreachable.");
		}
	}

	private object? TryParseLiteralTextOrPattern()
	{
		PatternLiteralTextNode? prefixLiteralText = ParseLiteralText();

		if (Completed)
		{
			return prefixLiteralText;
		}

		PatternDateTimeNode dateTimeNode = ParseDateTimePattern();
		object? suffix = TryParseLiteralTextOrPattern();
		if (suffix is null)
		{
			return new PatternRootNode(prefixLiteralText, dateTimeNode, (PatternLiteralTextNode?)null);
		}
		else if (suffix is PatternLiteralTextNode suffixLiteralText)
		{
			return new PatternRootNode(prefixLiteralText, dateTimeNode, suffixLiteralText);
		}
		else if (suffix is PatternRootNode suffixPattern)
		{
			return new PatternRootNode(prefixLiteralText, dateTimeNode, suffixPattern);
		}
		else
		{
			throw new InvalidOperationException("Unreachable.");
		}
	}

	// <datetime-pattern> ::= <era> | <year> | <month> | <day> | <dayofweek> |
	//                        <period> | <hour> | <minute> | <second> | <timezone>
	private PatternDateTimeNode ParseDateTimePattern()
	{
		if (Completed)
		{
			throw new ArgumentException($"Failed to parse date time pattern. Already reached the end of the pattern '{_pattern}'.");
		}

		if (TryParseEra(out var era))
		{
			return era;
		}
		else if (TryParseYear(out var year))
		{
			return year;
		}
		else if (TryParseMonth(out var month))
		{
			return month;
		}
		else if (TryParseDay(out var day))
		{
			return day;
		}
		else if (TryParseDayOfWeek(out var dayOfWeek))
		{
			return dayOfWeek;
		}
		else if (TryParsePeriod(out var period))
		{
			return period;
		}
		else if (TryParseHour(out var hour))
		{
			return hour;
		}
		else if (TryParseMinute(out var minute))
		{
			return minute;
		}
		else if (TryParseSecond(out var second))
		{
			return second;
		}
		else if (TryParseTimeZone(out var timeZone))
		{
			return timeZone;
		}

		throw new ArgumentException($"Failed to parse date time pattern component for pattern '{_pattern}'.");
	}

	private bool TryParseEra([NotNullWhen(true)] out PatternEraNode? era)
	{
		if (_pattern[_index] != '{')
		{
			era = null;
			return false;
		}

		if (IsWordAfterOpenBrace(_index, "era.abbreviated"))
		{
			_index += "{era.abbreviated".Length;
			int? idealLength = TryGetIdealLengthAndAdvance();
			ExpectCharacterAndAdvance('}');

			era = new PatternEraNode(idealLength);
			return true;
		}

		era = null;
		return false;
	}

	private bool TryParseYear([NotNullWhen(true)] out PatternYearNode? year)
	{
		if (_pattern[_index] != '{')
		{
			year = null;
			return false;
		}

		if (IsWordAfterOpenBrace(_index, "year.full"))
		{
			_index += "{year.full".Length;
			int? idealLength = TryGetIdealLengthAndAdvance();
			ExpectCharacterAndAdvance('}');
			year = new PatternYearNode(PatternYearNode.YearKind.Full, idealLength);
			return true;
		}
		else if (IsWordAfterOpenBrace(_index, "year.abbreviated"))
		{
			_index += "{year.abbreviated".Length;
			int? idealLength = TryGetIdealLengthAndAdvance();
			ExpectCharacterAndAdvance('}');
			year = new PatternYearNode(PatternYearNode.YearKind.Abbreviated, idealLength);
			return true;
		}

		year = null;
		return false;
	}

	private bool TryParseMonth([NotNullWhen(true)] out PatternMonthNode? month)
	{
		if (_pattern[_index] != '{')
		{
			month = null;
			return false;
		}

		if (IsWordAfterOpenBrace(_index, "month.full"))
		{
			_index += "{month.full".Length;
			ExpectCharacterAndAdvance('}');
			month = new PatternMonthNode(PatternMonthNode.MonthKind.Full);
			return true;
		}
		else if (IsWordAfterOpenBrace(_index, "month.solo.full"))
		{
			_index += "{month.solo.full".Length;
			ExpectCharacterAndAdvance('}');
			month = new PatternMonthNode(PatternMonthNode.MonthKind.SoloFull);
			return true;
		}
		else if (IsWordAfterOpenBrace(_index, "month.abbreviated"))
		{
			_index += "{month.abbreviated".Length;
			int? idealLength = TryGetIdealLengthAndAdvance();
			ExpectCharacterAndAdvance('}');
			month = new PatternMonthNode(PatternMonthNode.MonthKind.Abbreviated, idealLength);
			return true;
		}
		else if (IsWordAfterOpenBrace(_index, "month.solo.abbreviated"))
		{
			_index += "{month.solo.abbreviated".Length;
			int? idealLength = TryGetIdealLengthAndAdvance();
			ExpectCharacterAndAdvance('}');
			month = new PatternMonthNode(PatternMonthNode.MonthKind.SoloAbbreviated, idealLength);
			return true;
		}
		else if (IsWordAfterOpenBrace(_index, "month.integer"))
		{
			_index += "{month.integer".Length;
			int? idealLength = TryGetIdealLengthAndAdvance();
			ExpectCharacterAndAdvance('}');
			month = new PatternMonthNode(PatternMonthNode.MonthKind.Integer, idealLength);
			return true;
		}

		month = null;
		return false;
	}

	private bool TryParseDayOfWeek([NotNullWhen(true)] out PatternDayOfWeekNode? dayOfWeek)
	{
		if (_pattern[_index] != '{')
		{
			dayOfWeek = null;
			return false;
		}

		if (IsWordAfterOpenBrace(_index, "dayofweek.full"))
		{
			_index += "{dayofweek.full".Length;
			ExpectCharacterAndAdvance('}');
			dayOfWeek = new PatternDayOfWeekNode(PatternDayOfWeekNode.DayOfWeekKind.Full);
			return true;
		}
		else if (IsWordAfterOpenBrace(_index, "dayofweek.solo.full"))
		{
			_index += "{dayofweek.solo.full".Length;
			ExpectCharacterAndAdvance('}');
			dayOfWeek = new PatternDayOfWeekNode(PatternDayOfWeekNode.DayOfWeekKind.SoloFull);
			return true;
		}
		else if (IsWordAfterOpenBrace(_index, "dayofweek.abbreviated"))
		{
			_index += "{dayofweek.abbreviated".Length;
			int? idealLength = TryGetIdealLengthAndAdvance();
			ExpectCharacterAndAdvance('}');
			dayOfWeek = new PatternDayOfWeekNode(PatternDayOfWeekNode.DayOfWeekKind.Abbreviated, idealLength);
			return true;
		}
		else if (IsWordAfterOpenBrace(_index, "dayofweek.solo.abbreviated"))
		{
			_index += "{dayofweek.solo.abbreviated".Length;
			int? idealLength = TryGetIdealLengthAndAdvance();
			ExpectCharacterAndAdvance('}');
			dayOfWeek = new PatternDayOfWeekNode(PatternDayOfWeekNode.DayOfWeekKind.SoloAbbreviated, idealLength);
			return true;
		}

		dayOfWeek = null;
		return false;
	}

	private bool TryParseDay([NotNullWhen(true)] out PatternDayNode? day)
	{
		if (_pattern[_index] != '{')
		{
			day = null;
			return false;
		}

		if (IsWordAfterOpenBrace(_index, "day.integer"))
		{
			_index += "{day.integer".Length;
			int? idealLength = TryGetIdealLengthAndAdvance();
			ExpectCharacterAndAdvance('}');

			day = new PatternDayNode(idealLength);
			return true;
		}

		day = null;
		return false;
	}

	private bool TryParsePeriod([NotNullWhen(true)] out PatternPeriodNode? period)
	{
		if (_pattern[_index] != '{')
		{
			period = null;
			return false;
		}

		if (IsWordAfterOpenBrace(_index, "period.abbreviated"))
		{
			_index += "{period.abbreviated".Length;
			int? idealLength = TryGetIdealLengthAndAdvance();
			ExpectCharacterAndAdvance('}');

			period = new PatternPeriodNode(idealLength);
			return true;
		}

		period = null;
		return false;
	}

	private bool TryParseHour([NotNullWhen(true)] out PatternHourNode? hour)
	{
		if (_pattern[_index] != '{')
		{
			hour = null;
			return false;
		}

		if (IsWordAfterOpenBrace(_index, "hour.integer"))
		{
			_index += "{hour.integer".Length;
			int? idealLength = TryGetIdealLengthAndAdvance();
			ExpectCharacterAndAdvance('}');

			hour = new PatternHourNode(idealLength);
			return true;
		}

		hour = null;
		return false;
	}

	private bool TryParseMinute([NotNullWhen(true)] out PatternMinuteNode? minute)
	{
		if (_pattern[_index] != '{')
		{
			minute = null;
			return false;
		}

		if (IsWordAfterOpenBrace(_index, "minute.integer"))
		{
			_index += "{minute.integer".Length;
			int? idealLength = TryGetIdealLengthAndAdvance();
			ExpectCharacterAndAdvance('}');

			minute = new PatternMinuteNode(idealLength);
			return true;
		}

		minute = null;
		return false;
	}

	private bool TryParseSecond([NotNullWhen(true)] out PatternSecondNode? second)
	{
		if (_pattern[_index] != '{')
		{
			second = null;
			return false;
		}

		if (IsWordAfterOpenBrace(_index, "second.integer"))
		{
			_index += "{second.integer".Length;
			int? idealLength = TryGetIdealLengthAndAdvance();
			ExpectCharacterAndAdvance('}');

			second = new PatternSecondNode(idealLength);
			return true;
		}

		second = null;
		return false;
	}

	private bool TryParseTimeZone([NotNullWhen(true)] out PatternTimeZoneNode? timeZone)
	{
		if (_pattern[_index] != '{')
		{
			timeZone = null;
			return false;
		}

		if (IsWordAfterOpenBrace(_index, "timezone.full"))
		{
			_index += "{timezone.full".Length;
			ExpectCharacterAndAdvance('}');

			timeZone = new PatternTimeZoneNode(PatternTimeZoneNode.TimeZoneKind.Full);
			return true;
		}
		else if (IsWordAfterOpenBrace(_index, "timezone.abbreviated"))
		{
			_index += "{timezone.abbreviated".Length;
			int? idealLength = TryGetIdealLengthAndAdvance();
			ExpectCharacterAndAdvance('}');

			timeZone = new PatternTimeZoneNode(PatternTimeZoneNode.TimeZoneKind.Full, idealLength);
			return true;
		}

		timeZone = null;
		return false;
	}

	private void ExpectCharacterAndAdvance(char c)
	{
		if (_index >= _pattern.Length ||
			_pattern[_index] != c)
		{
			throw new ArgumentException($"Expected character '{c}' at index {_index} of pattern '{_pattern}'.");
		}

		_index++;
	}

	private int? TryGetIdealLengthAndAdvance()
	{
		if (_pattern[_index] != '(' ||
			_index + 2 >= _pattern.Length ||
			_pattern[_index + 2] != ')')
		{
			return null;
		}

		char idealLengthChar = _pattern[_index + 1];
		_index += 3;
		if (idealLengthChar >= '1' && idealLengthChar <= '9')
		{
			return idealLengthChar - '0';
		}

		throw new ArgumentException($"The character '{idealLengthChar}' is not a valid ideal-length digit");
	}

	private PatternLiteralTextNode? ParseLiteralText()
	{
		// Keep looking for "{openbrace}", "{closebrace}", or any character that is not "{" or "}".
		StringBuilder builder = new();
		int i = _index;
		while (i < _pattern.Length)
		{
			if (_pattern[i] == '{')
			{
				if (IsOpenBraceLiteral(i))
				{
					builder.Append('{');
					i += "{openbrace}".Length;
					continue;
				}
				else if (IsCloseBraceLiteral(i))
				{
					builder.Append('}');
					i += "{closebrace}".Length;
					continue;
				}
				else
				{
					// NOT a literal.
					break;
				}
			}
			else if (_pattern[i] != '}')
			{
				// Not '{' and not '}', so part of the literal text.
				builder.Append(_pattern[i]);
				i++;
			}
		}

		_index = i;
		return builder.Length == 0 ? null : new PatternLiteralTextNode(builder.ToString());
	}

	private bool IsWordAfterOpenBrace(int i, string word)
	{
		// i should be pointing at the index of '{'.
		// We want to return true if we are at "{word".
		// Note that we don't check for the "}" here
		// This is because some callsites will continue parsing something between word and "}".
		Debug.Assert(_pattern[i] == '{');

		int wordLength = word.Length;
		if (i + wordLength >= _pattern.Length)
		{
			return false;
		}

		return _pattern.AsSpan().Slice(i + 1, wordLength).SequenceEqual(word);
	}

	private bool IsOpenBraceLiteral(int i)
	{
		int openBraceLength = "openbrace".Length;
		return IsWordAfterOpenBrace(i, "openbrace") &&
			i + openBraceLength + 1 < _pattern.Length &&
			_pattern[i + openBraceLength + 1] == '}';
	}

	private bool IsCloseBraceLiteral(int i)
	{
		int closeBraceLength = "closebrace".Length;
		return IsWordAfterOpenBrace(i, "closebrace") &&
			i + closeBraceLength + 1 < _pattern.Length &&
			_pattern[i + closeBraceLength + 1] == '}';
	}
}
