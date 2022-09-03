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
			AppendIndented("{", current);
			AppendLine();

			return new DisposableAction(() =>
			{
				CurrentLevel -= count;
				AppendIndented("}", current);
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
			AppendIndented(text, CurrentLevel);
		}

		private void AppendIndented(string text, int indentCount)
		{
			_stringBuilder.Append(new string('\t', indentCount)); // TODO: Which is faster? this? or looping and appending a single tab each iteration?
			_stringBuilder.Append(text);
		}

		public void AppendFormatIndented(IFormatProvider formatProvider, string text, params object[] replacements)
		{
			_stringBuilder.Append(new string('\t', CurrentLevel)); // TODO: Which is faster? this? or looping and appending a single tab each iteration?
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
		/// Appends the given string, *without* appending a newline at the end.
		/// </summary>
		/// <param name="text">The string to append.</param>
		/// <remarks>
		/// Even though this method seems like it appends a newline, it doesn't. To append a
		/// newline, call <see cref="AppendLine()"/> after this method.
		/// </remarks>
		public void AppendMultiLineIndented(string text)
		{
			_stringBuilder.Append(text.Indent(CurrentLevel));
		}

		public override string ToString()
		{
			return _stringBuilder.ToString();
		}
	}

}
