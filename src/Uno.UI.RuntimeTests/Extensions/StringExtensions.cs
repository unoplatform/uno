using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Uno.UI.RuntimeTests.Extensions;

public static class StringExtensions
{
	/// <summary>
	/// Split a string by a separator, while ignoring certain regex pattern to be splitted.
	/// </summary>
	/// <param name="input"></param>
	/// <param name="separator"></param>
	/// <param name="ignoredPattern"></param>
	/// <param name="options"></param>
	/// <remarks>This is typically used to split separator-seperator string that contains brackets.</remarks>
	/// <returns></returns>
	public static string[] SplitWithIgnore(this string input, char separator, string ignoredPattern, StringSplitOptions options = StringSplitOptions.None)
	{
		var ignores = Regex.Matches(input, ignoredPattern);

		var shards = new List<string>();
		for (int i = 0; i < input.Length; i++)
		{
			var nextSeparator = input.IndexOf(separator, i);

			// find the next separator, if we are within the ignored pattern
			while (nextSeparator != -1 && ignores.FirstOrDefault(x => InRange(x, nextSeparator)) is { } enclosingIgnore)
			{
				nextSeparator = enclosingIgnore.Index + enclosingIgnore.Length is { } afterIgnore && afterIgnore < input.Length
					? input.IndexOf(separator, afterIgnore)
					: -1;
			}

			if (nextSeparator != -1)
			{
				shards.Add(input.Substring(i, nextSeparator - i));
				i = nextSeparator;

				// skip multiple continuous spaces
				while (options.HasFlag(StringSplitOptions.RemoveEmptyEntries) && i + 1 < input.Length && input[i + 1] == separator) i++;
			}
			else
			{
				shards.Add(input.Substring(i));
				break;
			}
		}

		if (options.HasFlag(StringSplitOptions.TrimEntries))
		{
			return shards.Select(x => x.Trim()).ToArray();
		}
		else
		{
			return shards.ToArray();
		}

		bool InRange(Match x, int index) => x.Index <= index && index < (x.Index + x.Length);
	}
}
