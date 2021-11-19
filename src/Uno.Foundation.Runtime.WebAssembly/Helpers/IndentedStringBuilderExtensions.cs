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
using System.Globalization;
using System.Text;

namespace Uno.Foundation.Runtime.WebAssembly.Helpers
{
    internal static class IndentedStringBuilderExtensions
    {
		public static IndentedStringBuilder AsIndented(this StringBuilder builder)
		{
			return new IndentedStringBuilder(builder);
		}

		public static void AppendLine(this IndentedStringBuilder builder, IFormatProvider formatProvider, string pattern, params object[] replacements)
		{
			builder.AppendFormat(formatProvider, pattern, replacements);
			builder.AppendLine();
		}

		public static void AppendLine(this IndentedStringBuilder builder, IFormatProvider formatProvider, int indentLevel, string pattern, params object[] replacements)
		{
			builder.AppendFormat(formatProvider, pattern.Indent(indentLevel), replacements);
			builder.AppendLine();
		}

		public static void AppendLineInvariant(this IndentedStringBuilder builder, string pattern, params object[] replacements)
		{
			builder.AppendLine(CultureInfo.InvariantCulture, pattern, replacements);
		}

		public static void AppendLineInvariant(this IndentedStringBuilder builder, int indentLevel, string pattern, params object[] replacements)
		{
			builder.AppendLine(CultureInfo.InvariantCulture, indentLevel, pattern, replacements);
		}

		public static void AppendFormatInvariant(this IndentedStringBuilder builder, string pattern, params object[] replacements)
		{
			builder.AppendFormat(CultureInfo.InvariantCulture, pattern, replacements);
		}

		public static IDisposable BlockInvariant(this IndentedStringBuilder builder, string pattern, params object[] parameters)
		{
			return builder.Block(CultureInfo.InvariantCulture, pattern, parameters);
		}
	}
}
