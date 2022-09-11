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
using System.Collections.Generic;
using System.Text;

namespace Uno.Foundation.Runtime.WebAssembly.Helpers
{
	/// <summary>
	/// A C# code indented builder.
	/// </summary>
	internal class IndentedStringBuilder
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

		public virtual IDisposable Indent(int count = 1)
		{
			CurrentLevel += count;
			return new DisposableAction(() => CurrentLevel -= count);
		}

		public virtual IDisposable Block(int count = 1)
		{
			var current = CurrentLevel;

			CurrentLevel += count;
			Append("{".Indent(current));
			AppendLine();

			return new DisposableAction(() =>
			{
				CurrentLevel -= count;
				Append("}".Indent(current));
				AppendLine();
			});
		}

		public virtual IDisposable Block(IFormatProvider formatProvider, string pattern, params object[] parameters)
		{
			AppendFormat(formatProvider, pattern, parameters);
			AppendLine();

			return Block();
		}

		public virtual void Append(string text)
		{
			_stringBuilder.Append(text);
		}

		public virtual void AppendFormat(IFormatProvider formatProvider, string pattern, params object[] replacements)
		{
			_stringBuilder.AppendFormat(formatProvider, pattern.Indent(CurrentLevel), replacements);
		}

		/// <summary>
		/// Appends a newline.
		/// </summary>
		/// <remarks>
		/// This method presents correct behavior, as opposed to its <see cref="AppendLine(String)"/>
		/// overload. Therefore, this method should be used whenever a newline is desired.
		/// </remarks>
		public virtual void AppendLine()
		{
			_stringBuilder.AppendLine();
		}

		/// <summary>
		/// Appends the given string, *without* appending a newline at the end.
		/// </summary>
		/// <param name="text">The string to append.</param>
		/// <remarks>
		/// Even though this method seems like it appends a newline, it doesn't. To append a
		/// newline, call <see cref="AppendLine()"/> after this method, as the parameterless
		/// overload has the correct behavior.
		/// </remarks>
		public virtual void AppendLine(string text)
		{
			_stringBuilder.Append(text.Indent(CurrentLevel));
		}

		public override string ToString()
		{
			return _stringBuilder.ToString();
		}
	}
}
