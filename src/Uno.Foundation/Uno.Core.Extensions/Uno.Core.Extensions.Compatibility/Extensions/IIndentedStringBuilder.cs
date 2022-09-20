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

namespace Uno.Extensions
{
	internal interface IIndentedStringBuilder
	{
		/// <summary>
		/// Gets the current indentation level
		/// </summary>
		int CurrentLevel { get; }

		/// <summary>
		/// Appends text using the current indentation level
		/// </summary>
		/// <param name="text"></param>
		void Append(string text);

		/// <summary>
		/// Appends a line using the current indentation level 
		/// </summary>
		void AppendLine();

		/// <summary>
		/// Writes the provided text and adds line using the current indentation level 
		/// </summary>
		void AppendMultiLineIndented(string text);

		/// <summary>
		/// Creates an indentation block
		/// </summary>
		/// <param name="count">The indentation level of the new block.</param>
		/// <returns>A disposable that will close the block</returns>
		IDisposable Block(int count = 1);

		/// <summary>
		/// Creates an indentation block, e.g. using a C# curly braces.
		/// </summary>
		/// <returns>A disposable that will close the block</returns>
		IDisposable Block(IFormatProvider formatProvider, string pattern, params object[] parameters);

		/// <summary>
		/// Adds an indentation 
		/// </summary>
		/// <param name="count"></param>
		/// <returns></returns>
		IDisposable Indent(int count = 1);

		/// <summary>
		/// Provides a string representing the complete builder.
		/// </summary>
		string ToStringAndFree();

		public void AppendIndented(string text);

		public void AppendIndented(ReadOnlySpan<char> text);

		void AppendFormatIndented(IFormatProvider formatProvider, string text, params object[] replacements);
	}
}
