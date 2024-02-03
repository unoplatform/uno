// ******************************************************************
// Copyright � 2015-2018 Uno Platform Inc. All rights reserved.
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
using System.Diagnostics.CodeAnalysis;

#endregion

namespace Uno.Extensions
{
	public delegate void SpanAction<T, in TArg>(Span<T> span, TArg arg);

	internal static partial class StringExtensions
	{
		// https://www.meziantou.net/split-a-string-into-lines-without-allocation.htm
		// Must be a ref struct as it contains a ReadOnlySpan<char>
		public ref struct LineSplitEnumerator
		{
			private ReadOnlySpan<char> _str;

			public LineSplitEnumerator(ReadOnlySpan<char> str)
			{
				_str = str;
				Current = default;
			}

			// Needed to be compatible with the foreach operator
			public LineSplitEnumerator GetEnumerator() => this;

			public bool MoveNext()
			{
				var span = _str;
				if (span.Length == 0) // Reach the end of the string
					return false;

				var index = span.IndexOfAny('\r', '\n');
				if (index == -1) // The string is composed of only one line
				{
					_str = ReadOnlySpan<char>.Empty; // The remaining string is an empty string
					Current = span;
					return true;
				}

				if (index < span.Length - 1 && span[index] == '\r')
				{
					// Try to consume the '\n' associated to the '\r'
					var next = span[index + 1];
					if (next == '\n')
					{
						Current = span.Slice(0, index);
						_str = span.Slice(index + 2);
						return true;
					}
				}

				Current = span.Slice(0, index);
				_str = span.Slice(index + 1);
				return true;
			}

			public ReadOnlySpan<char> Current { get; private set; }
		}

		public static LineSplitEnumerator SplitLines(this string instance)
		{
			return new LineSplitEnumerator(instance.AsSpan());
		}

		public static LineSplitEnumerator SplitLines(this ReadOnlySpan<char> instance)
		{
			return new LineSplitEnumerator(instance);
		}

		public static bool IsNullOrEmpty([NotNullWhen(false)] this string instance)
		{
			return string.IsNullOrEmpty(instance);
		}

		public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string instance)
		{
			return string.IsNullOrWhiteSpace(instance);
		}

		/// <summary>
		/// Check if the specified string occurs in the current System.String object. A parameter specifies the type of search to use for the specified string.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="value">The string to seek.</param>
		/// <param name="comparisonType">One of the enumeration values that specifies the rules for the search.</param>
		/// <returns></returns>
		public static bool Contains(this string instance, string value, StringComparison comparisonType)
			=> instance.IndexOf(value, comparisonType) >= 0;


		//TODO Filter Where HasValue()
		public static string JoinBy(this IEnumerable<string> items, string joinBy)
		{
			return string.Join(joinBy, items);
		}

		public static string InvariantCultureFormat(this string instance, params object[] array)
		{
			return string.Format(CultureInfo.InvariantCulture, instance, array);
		}

		/// <summary>
		/// Removes all leading occurrences of <see cref="trimText"/> from the current System.String object
		/// </summary>
		/// <param name="trimText">A string to remove</param>
		/// <returns>The string that remains after all occurrences of the <see cref="trimText"/> are removed from the start of the current string.</returns>
		public static string TrimStart(this string source, string trimText, bool ignoreCase = false)
		{
			if (!string.IsNullOrEmpty(trimText) && source.StartsWith(trimText, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
			{
				return source.Substring(trimText.Length);
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
				return source.Substring(0, source.Length - trimText.Length);
			}
			else
			{
				return source;
			}
		}

		public static string Create<TState>(int length, TState state, SpanAction<char, TState> action)
		{
			scoped Span<char> buffer;

			if (length <= 128)
				buffer = stackalloc char[128];
			else
				buffer = new char[length];

			action(buffer.Slice(0, length), state);
			return buffer.Slice(0, length).ToString();
		}
	}
}
