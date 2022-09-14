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
using System;
using System.Globalization;
using System.Text;

namespace Uno.Extensions
{
	/// <summary>
	/// A C# code indented builder.
	/// </summary>
	internal sealed class IndentedStringBuilder : IIndentedStringBuilder
	{
		// https://www.meziantou.net/split-a-string-into-lines-without-allocation.htm
		// Must be a ref struct as it contains a ReadOnlySpan<char>
		private ref struct LineSplitEnumerator
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

		private readonly StringBuilder _stringBuilder;

		public int CurrentLevel { get; private set; }

		public IndentedStringBuilder()
			: this(new StringBuilder())
		{
		}

		public IndentedStringBuilder(StringBuilder stringBuilder)
		{
			_stringBuilder = stringBuilder;
		}

		public IDisposable Indent(int count = 1)
		{
			CurrentLevel += count;
			return new DisposableAction(() => CurrentLevel -= count);
		}

		public IDisposable Block(int count = 1)
		{
			var current = CurrentLevel;

			CurrentLevel += count;
			AppendIndented('{', current);
			AppendLine();

			return new DisposableAction(() =>
			{
				CurrentLevel -= count;
				AppendIndented('}', current);
				AppendLine();
			});
		}

		public IDisposable Block(IFormatProvider formatProvider, string pattern, params object[] parameters)
		{
			AppendFormatIndented(formatProvider, pattern, parameters);
			AppendLine();

			return Block();
		}

		public void Append(string text)
		{
			_stringBuilder.Append(text);
		}

		public void AppendIndented(string text)
		{
			_stringBuilder.Append('\t', CurrentLevel);
			_stringBuilder.Append(text);
		}

		private void AppendIndented(char c, int indentCount)
		{
			_stringBuilder.Append('\t', indentCount);
			_stringBuilder.Append(c);
		}

		public void AppendFormatIndented(IFormatProvider formatProvider, string text, params object[] replacements)
		{
			_stringBuilder.Append('\t', CurrentLevel);
			_stringBuilder.AppendFormat(formatProvider, text, replacements);
		}

		/// <summary>
		/// Appends a newline.
		/// </summary>
		public void AppendLine()
		{
			_stringBuilder.AppendLine();
		}

		/// <summary>
		/// Appends the given multi-line string with each line indented per CurentLevel, with a newline at the end.
		/// </summary>
		/// <param name="text">The string to append.</param>
		public void AppendMultiLineIndented(string text)
		{
			// LineSplitEnumerator is a struct so there is no allocation here
			var enumerator = new LineSplitEnumerator(text.AsSpan());
			foreach (var line in enumerator)
			{
				AppendIndented(line.ToString());
				AppendLine();
			}
		}

		public override string ToString()
		{
			return _stringBuilder.ToString();
		}
	}

}
