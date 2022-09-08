// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
#region Using directives
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

#endregion

namespace Uno.Extensions
{
	internal static partial class StringExtensions
	{
#if (!SILVERLIGHT && !WINDOWS_UWP && HAS_COMPILEDREGEX) || WINDOWS_PHONE || HAS_COMPILEDREGEX
        private static readonly Regex _newLineRegex = new Regex(@"^", RegexOptions.Compiled | RegexOptions.Multiline);
#else
		private static readonly Lazy<Regex> _newLineRegex = new Lazy<Regex>(() => new Regex(@"^", RegexOptions.Multiline));
#endif
		public static bool IsNullOrEmpty(this string instance)
		{
			return string.IsNullOrEmpty(instance);
		}

		public static bool IsNullOrWhiteSpace(this string instance)
		{
			return string.IsNullOrWhiteSpace(instance);
		}

		public static bool HasValue(this string instance)
		{
			return !string.IsNullOrWhiteSpace(instance);
		}

		public static bool HasValueTrimmed(this string instance)
		{
			return !string.IsNullOrWhiteSpace(instance);
		}

		/// <summary>
		/// Check if the specified string occures in the current System.String object. A parameter specifies the type of search to use for the specified string.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="value">The string to seek.</param>
		/// <param name="comparisonType">One of the enumeration values that specifies the rules for the search.</param>
		/// <returns></returns>
		public static bool Contains(this string instance, string value, StringComparison comparisonType)
			=> instance.IndexOf(value, comparisonType) >= 0;

		/// <summary>
		/// Check if every characters in the string is considered as a "Unicode Number". WARNING: READ REMARKS!!
		/// </summary>
		/// <remarks>
		///  In addition to including digits, numbers include characters, fractions, subscripts, superscripts,
		/// Roman numerals, currency numerators, and encircled numbers. This method contrasts with the IsDigit
		/// method, which determines whether a Char is a radix-10 digit.
		/// </remarks>
		public static bool IsNumber(this string instance)
		{
			return instance.Trim().ToCharArray().All(Char.IsNumber);
		}

		/// <summary>
		/// Check if every characters in the string is considered as a "Unicode Decimal Digit". (char 0-9)
		/// </summary>
		/// <remarks>
		/// This contrasts with IsNumber, which determines whether a Char is of any numeric Unicode category.
		/// Numbers include characters such as fractions, subscripts, superscripts, Roman numerals,
		/// currency numerators, encircled numbers, and script-specific digits.
		/// </remarks>
		public static bool IsDigit(this string instance)
		{
			return instance.Trim().ToCharArray().All(Char.IsDigit);
		}

		//TODO Filter Where HasValue()
		public static string JoinBy(this IEnumerable<string> items, string joinBy)
		{
			return string.Join(joinBy, items.ToArray());
		}

		public static string InvariantCultureFormat(this string instance, params object[] array)
		{
			return string.Format(CultureInfo.InvariantCulture, instance, array);
		}

		public static string CurrentCultureFormat(this string instance, params object[] array)
		{
			return string.Format(CultureInfo.CurrentCulture, instance, array);
		}

		/// <summary>
		/// Returns a string that contains a specified number of characters from the left side of a string.
		/// </summary>
		/// <param name="instance"><see cref="System.String"/> expression from which the leftmost characters are returned.</param>
		/// <param name="length"><see cref="System.Int32"/> expression. Numeric expression indicating how many characters to return.</param>
		/// <returns>If zero, a zero-length string ("") is returned. If greater than or equal to the number of characters in value, the complete string is returned.</returns>
		/// <exception cref="System.ArgumentException">length &lt; 0</exception>
		public static string Left(this string instance, int length)
		{
			return instance.LeftRightInternal(length, () => instance.Substring(0, length));
		}

		/// <summary>
		/// Returns a string containing a specified number of characters from the right side of a string.
		/// </summary>
		/// <param name="instance"><see cref="System.String"/> expression from which the rightmost characters are returned.</param>
		/// <param name="length"><see cref="System.Int32"/> expression. Numeric expression indicating how many characters to return.</param>
		/// <returns>If zero, a zero-length string ("") is returned. If greater than or equal to the number of characters in value, the complete string is returned.</returns>
		/// <exception cref="System.ArgumentException">length &lt; 0</exception>
		public static string Right(this string instance, int length)
		{
			return instance.LeftRightInternal(length, () => instance.Substring(instance.Length - length, length));
		}

		/// <summary>
		/// Returns a string that contains a specified number of characters of a string.
		/// </summary>
		/// <param name="instance"><see cref="System.String"/> expression from which the characters are returned.</param>
		/// <param name="length"><see cref="System.Int32"/> expression. Numeric expression indicating how many characters to return.</param>
		/// <param name="predicate">Func <see cref="string"/> expression that returns the substring.</param>
		private static string LeftRightInternal(this string instance, int length, Func<string> predicate)
		{
			if (length < 0)
				throw new ArgumentException("'length' must be greater than zero.", "length");

			if (instance == null || length == 0)
				return string.Empty;

			// return whole value if string length is greater or equal than length parameter, otherwise invoke the result for the value.
			return length >= instance.Length ? instance : predicate.Invoke();
		}

		/// <summary>
		/// Append a chunk at the end of a string
		/// </summary>
		/// <param name="target">target string object</param>
		/// <param name="chunk">Chunk to add</param>
		/// <returns>New string with the chunk at appended at the end.</returns>
		public static string Append(this string target, string chunk)
		{
			return target.Append(chunk, s => true);
		}

		/// <summary>
		/// Append a chunk at the end of a string only if the condition is met.
		/// </summary>
		/// <param name="target">target string object</param>
		/// <param name="chunk">Chunk to add</param>
		/// <param name="condition">Condition to meet for the chunk to be added</param>
		/// <returns>New string with the chunk at appended at the end or original string if condition is not met.</returns>
		public static string Append(this string target, string chunk, Func<string, bool> condition)
		{
			return target + (condition(target) ? chunk : "");
		}

		/// <summary>
		/// Append a chunk at the end of a string only if the string doen't end by it.
		/// </summary>
		/// <param name="target">target string object</param>
		/// <param name="chunk">Chunk to add</param>
		/// <returns>New string with the chunk at appended at the end or the original string if the target already end by chunk.</returns>
		public static string AppendIfMissing(this string target, string chunk)
		{
			return target.Append(chunk, s => !s.EndsWith(chunk, StringComparison.Ordinal));
		}

		/// <summary>
		/// Removes all leading occurrences of <see cref="trimText"/> from the current System.String object
		/// </summary>
		/// <param name="trimText">A string to remove</param>
		/// <returns>The string that remains after all occurrences of the <see cref="trimText"/> are removed from the start of the current string.</returns>
		public static string TrimStart(this string source, string trimText)
		{
			if (!string.IsNullOrEmpty(trimText) && source.StartsWith(trimText, StringComparison.Ordinal))
			{
				return source.Substring(trimText.Length).TrimStart(trimText, StringComparison.Ordinal);
			}
			else
			{
				return source;
			}
		}

		public static string TrimStart(this string source, string trimText, StringComparison comparisonType)
		{
			if (!string.IsNullOrEmpty(trimText) && source.StartsWith(trimText, comparisonType))
			{
				return source.Substring(trimText.Length).TrimStart(trimText, comparisonType);
			}
			else
			{
				return source;
			}
		}

		/// <summary>
		/// Removes all trailing occurrences of <see cref="trimText"/> from the current System.String object
		/// </summary>
		/// <param name="trimText">A string to remove</param>
		/// <returns>The string that remains after all occurrences of the <see cref="trimText"/> are removed from the end of the current string.</returns>
		public static string TrimEnd(this string source, string trimText)
		{
			if (!string.IsNullOrEmpty(trimText) && source.EndsWith(trimText, StringComparison.Ordinal))
			{
				return source.Substring(0, source.Length - trimText.Length).TrimEnd(trimText, StringComparison.Ordinal);
			}
			else
			{
				return source;
			}
		}

		public static string TrimEnd(this string source, string trimText, StringComparison comparisonType)
		{
			if (!string.IsNullOrEmpty(trimText) && source.EndsWith(trimText, comparisonType))
			{
				return source.Substring(0, source.Length - trimText.Length).TrimEnd(trimText, comparisonType);
			}
			else
			{
				return source;
			}
		}

		public static string Indent(this string text, int indentCount = 1)
		{
			return _newLineRegex.Value.Replace(text, new String('\t', indentCount));
		}

		/// <summary>
		/// Uppercases the first character of the string.
		/// If the string is <c>null</c> or <c>""</c> then it returns <c>string.Empty</c>
		/// </summary>
		/// <param name="s">The string.</param>
		/// <returns>The string where the first character is in uppercase or <c>string.Empty</c> if the string is <c>null</c> or <c>""</c></returns>
		public static string UppercaseFirst(this string s)
		{
			if (s.IsNullOrEmpty())
			{
				return string.Empty;
			}

			//Perhaps it's already OK
			var firstChar = s[0];
			if (char.IsUpper(firstChar))
			{
				return s;
			}

			// Return char and concat substring.
			return char.ToUpper(firstChar) + s.Substring(1);
		}

		/// <summary>
		/// Removes diacritics (e.g. accents) from a given string.
		/// </summary>
		/// <param name="s">The string.</param>
		/// <returns>The string without diacritics, e.g. Montréal to Montreal, or <c>string.Empty</c> if the string is <c>null</c> or <c>""</c>.</returns>
		public static string RemoveDiacritics(this string s)
		{
			if (s.IsNullOrEmpty())
			{
				return string.Empty;
			}

			return string.Concat(
				s.Normalize(NormalizationForm.FormD)
				 .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
			)
			.Normalize(NormalizationForm.FormC);
		}
	}
}
