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
using System.Runtime.InteropServices;
using System.Text;

namespace Uno.Extensions
{
	/// <summary>
	/// A C# code indented builder.
	/// </summary>
	internal sealed class IndentedStringBuilder : IIndentedStringBuilder
	{
		public StringBuilder Builder { get; } = new();

		public int CurrentLevel { get; private set; }

		public IndentedStringBuilder()
		{
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
			Builder.Append(text);
		}

		public void AppendIndented(string text)
		{
			Builder.Append('\t', CurrentLevel);
			Builder.Append(text);
		}

		public void AppendIndented(ReadOnlySpan<char> text)
		{
			Builder.Append('\t', CurrentLevel);
			unsafe
			{
				fixed (char* ptr = &MemoryMarshal.GetReference(text))
				{
					Builder.Append(ptr, text.Length);
				}
			}
		}

		private void AppendIndented(char c, int indentCount)
		{
			Builder.Append('\t', indentCount);
			Builder.Append(c);
		}

		public void AppendFormatIndented(IFormatProvider formatProvider, string text, params object[] replacements)
		{
			Builder.Append('\t', CurrentLevel);
			Builder.AppendFormat(formatProvider, text, replacements);
		}

		/// <summary>
		/// Appends a newline.
		/// </summary>
		public void AppendLine()
		{
			Builder.AppendLine();
		}

		/// <summary>
		/// Appends the given multi-line string with each line indented per CurentLevel, with a newline at the end.
		/// </summary>
		/// <param name="text">The string to append.</param>
		public void AppendMultiLineIndented(string text)
		{
			foreach (var line in text.SplitLines())
			{
				AppendIndented(line);
				AppendLine();
			}
		}

		public override string ToString()
		{
			return Builder.ToString();
		}
	}

}
