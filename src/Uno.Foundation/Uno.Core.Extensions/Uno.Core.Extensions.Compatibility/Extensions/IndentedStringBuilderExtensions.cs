#nullable disable

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

namespace Uno.Extensions
{
	internal static class IndentedStringBuilderExtensions
    {
		public static void AppendLineIndented(this IIndentedStringBuilder builder, string pattern)
		{
			builder.AppendIndented(pattern);
			builder.AppendLine();
		}

		public static void AppendLineInvariantIndented(this IIndentedStringBuilder builder, string pattern, params object[] replacements)
		{
			builder.AppendFormatIndented(CultureInfo.InvariantCulture, pattern, replacements);
			builder.AppendLine();
		}

		public static IDisposable Indent(this IIndentedStringBuilder builder, string opening, string closing = null)
		{
			builder.AppendLineIndented(opening);
			var block = builder.Indent();

			return new DisposableAction(() =>
			{
				block.Dispose();
				if (closing != null)
				{
					builder.AppendLineIndented(closing);
				}
			});
		}

		public static IDisposable BlockInvariant(this IIndentedStringBuilder builder, string pattern, params object[] parameters)
		{
			return builder.Block(CultureInfo.InvariantCulture, pattern, parameters);
		}

		public static IDisposable BlockInvariant(this IIndentedStringBuilder builder, string pattern)
		{
			return builder.Block(CultureInfo.InvariantCulture, pattern);
		}
	}
}
