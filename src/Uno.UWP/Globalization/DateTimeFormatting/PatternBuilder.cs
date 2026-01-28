#nullable enable

using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Windows.Globalization.DateTimeFormatting;

/// <summary>
/// Builds DateTimeFormatter patterns from templates, applying clock and calendar overrides.
/// </summary>
internal static partial class PatternBuilder
{
	/// <summary>
	/// Transforms a .NET time pattern to respect the specified clock setting.
	/// For example, if a 12-hour clock is requested but the culture uses 24-hour format,
	/// this will convert "HH:mm" to "h:mm tt".
	/// </summary>
	/// <param name="pattern">The original .NET DateTime format pattern.</param>
	/// <param name="clock">The clock identifier (12HourClock or 24HourClock).</param>
	/// <param name="culture">The culture for getting AM/PM designators.</param>
	/// <returns>The transformed pattern.</returns>
	public static string TransformPatternForClock(string pattern, string clock, CultureInfo culture)
	{
		if (string.IsNullOrEmpty(pattern))
		{
			return pattern;
		}

		bool isTwelveHour = clock == ClockIdentifiers.TwelveHour;
		bool patternHas24Hour = pattern.Contains('H');
		bool patternHas12Hour = pattern.Contains('h');
		bool patternHasPeriod = pattern.Contains('t');

		// If the clock setting matches the pattern, no transformation needed
		if (isTwelveHour && patternHas12Hour && patternHasPeriod)
		{
			return pattern;
		}
		if (!isTwelveHour && patternHas24Hour && !patternHasPeriod)
		{
			return pattern;
		}

		// Need to transform the pattern
		var result = new StringBuilder(pattern.Length + 10);
		bool inQuote = false;
		bool hourFound = false;
		int hourEndIndex = -1;

		for (int i = 0; i < pattern.Length; i++)
		{
			char c = pattern[i];

			// Track quoted sections (literal text)
			if (c == '\'')
			{
				inQuote = !inQuote;
				result.Append(c);
				continue;
			}

			if (inQuote)
			{
				result.Append(c);
				continue;
			}

			// Handle hour patterns
			if (c == 'H' || c == 'h')
			{
				hourFound = true;
				// Count consecutive H or h
				int count = 1;
				while (i + count < pattern.Length && (pattern[i + count] == 'H' || pattern[i + count] == 'h'))
				{
					count++;
				}

				if (isTwelveHour)
				{
					// Convert to 12-hour format
					result.Append(count >= 2 ? "hh" : "h");
				}
				else
				{
					// Convert to 24-hour format
					result.Append(count >= 2 ? "HH" : "H");
				}

				hourEndIndex = result.Length;
				i += count - 1; // Skip the rest of the H/h sequence
				continue;
			}

			// Handle period/AM-PM designator
			if (c == 't')
			{
				// Count consecutive t
				int count = 1;
				while (i + count < pattern.Length && pattern[i + count] == 't')
				{
					count++;
				}

				if (isTwelveHour)
				{
					// Keep the period designator for 12-hour clock
					result.Append(count >= 2 ? "tt" : "t");
				}
				// For 24-hour clock, skip the period designator (don't append)

				i += count - 1; // Skip the rest of the t sequence
				continue;
			}

			result.Append(c);
		}

		// If 12-hour clock and no period designator exists in the pattern, add one
		if (isTwelveHour && hourFound && !patternHasPeriod)
		{
			// Insert period designator after the time portion
			// Find a good place to insert it (after minutes/seconds if present)
			var resultStr = result.ToString();
			int insertPos = FindPeriodInsertPosition(resultStr);
			if (insertPos >= 0)
			{
				result.Insert(insertPos, " tt");
			}
			else
			{
				result.Append(" tt");
			}
		}

		// Clean up any double spaces that might have been created
		var finalResult = result.ToString();
		while (finalResult.Contains("  "))
		{
			finalResult = finalResult.Replace("  ", " ");
		}

		return finalResult.Trim();
	}

	/// <summary>
	/// Finds the best position to insert a period (AM/PM) designator in a time pattern.
	/// </summary>
	private static int FindPeriodInsertPosition(string pattern)
	{
		// Look for the end of the time portion (after seconds, or minutes, or hours)
		int lastTimeChar = -1;
		bool inQuote = false;

		for (int i = 0; i < pattern.Length; i++)
		{
			char c = pattern[i];

			if (c == '\'')
			{
				inQuote = !inQuote;
				continue;
			}

			if (inQuote)
			{
				continue;
			}

			// Track the last time-related character
			if (c == 's' || c == 'm' || c == 'h' || c == 'H')
			{
				lastTimeChar = i;
			}
		}

		if (lastTimeChar >= 0)
		{
			// Find the end of the consecutive time characters
			while (lastTimeChar + 1 < pattern.Length)
			{
				char next = pattern[lastTimeChar + 1];
				if (next == 's' || next == 'm' || next == 'h' || next == 'H')
				{
					lastTimeChar++;
				}
				else
				{
					break;
				}
			}
			return lastTimeChar + 1;
		}

		return -1;
	}

	/// <summary>
	/// Determines if a .NET DateTime pattern represents a 12-hour format.
	/// </summary>
	public static bool IsPattern12Hour(string pattern)
	{
		bool inQuote = false;
		for (int i = 0; i < pattern.Length; i++)
		{
			char c = pattern[i];
			if (c == '\'')
			{
				inQuote = !inQuote;
				continue;
			}
			if (inQuote) continue;

			if (c == 'h') return true;
			if (c == 'H') return false;
		}
		return false;
	}

	/// <summary>
	/// Determines if a .NET DateTime pattern contains an hour component.
	/// </summary>
	public static bool PatternContainsHour(string pattern)
	{
		bool inQuote = false;
		for (int i = 0; i < pattern.Length; i++)
		{
			char c = pattern[i];
			if (c == '\'')
			{
				inQuote = !inQuote;
				continue;
			}
			if (inQuote) continue;

			if (c == 'h' || c == 'H') return true;
		}
		return false;
	}
}
