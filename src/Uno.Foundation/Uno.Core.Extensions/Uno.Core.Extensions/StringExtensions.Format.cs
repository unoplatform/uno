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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Uno.Extensions
{
	internal static partial class StringExtensions
	{
		/// <summary>
		/// Improves upon <see cref="string.Format(string, object[])"/> to allow a 4th and 5th
		/// group in numerical custom formats, for values 1 and -1. See <see cref="Format(IFormatProvider, string, object[])"/>
		/// for details.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static string Format(string format, params object[] args)
		{
			return Format(CultureInfo.CurrentUICulture, format, args);
		}

		/// <summary>
		/// Improves upon <see cref="string.Format(IFormatProvider, string, object[])"/> to allow a 4th and 5th
		/// group in numerical custom formats, for values 1 and -1. Just like the 3rd group, which applies to value 0,
		/// these groups will get used if the first group (positive) or second group (negative) would display the same
		/// string as if 1 or -1 was the argument. For example, given the en-US culture, the "{0:C;C;broke;a buck}"
		/// format would display "$1.42" for value 1.42, display "broke" for values 0, -0.004 or 0.003, and display
		/// "a buck" for values 0.995, 1 or 1.0025.
		/// </summary>
		/// <param name="provider"></param>
		/// <param name="format"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static string Format(IFormatProvider provider, string format, params object[] args)
		{
			var finalBuilder = new StringBuilder();

			var isEscaping = false;

			// We build both the deconstructed parts of a group and the original.
			// The deconstructed version does not include the opening and closing accolades, nor the colon or semi-colon separators.
			var groupBuilders = new List<StringBuilder>();
			var currentGroupBuilder = default(StringBuilder);
			var originalGroupBuilder = new StringBuilder();

			for (int position = 0; position < format.Length; position++)
			{
				var builder = (currentGroupBuilder == null) ? finalBuilder : originalGroupBuilder;

				if (isEscaping)
				{
					builder.Append(format[position]);
					currentGroupBuilder?.Append(format[position]);

					isEscaping = false;
				}
				else
				{
					switch (format[position])
					{
						case '\\':
							isEscaping = true;
							builder.Append('\\');
							currentGroupBuilder?.Append('\\');
							break;

						case '{':
							if ((position < format.Length - 1) && (format[position + 1] == '{'))
							{
								// Escaped {{.
								isEscaping = true;
								builder.Append('{');
								currentGroupBuilder?.Append('{');
							}
							else
							{
								if (currentGroupBuilder != null)
								{
									throw new ArgumentException("Invalid group format. Nested opening accolades.");
								}

								// No { in deconstructed groups.
								groupBuilders.Add(currentGroupBuilder = new StringBuilder());
								originalGroupBuilder.Append('{');
							}
							break;

						case '}':
							if ((position < format.Length - 1) && (format[position + 1] == '}'))
							{
								// Escaped }}.
								isEscaping = true;
								builder.Append('}');
								currentGroupBuilder?.Append('}');
							}
							else
							{
								if (currentGroupBuilder == null)
								{
									// string.Format does not tolerate this.
									throw new ArgumentException("Format string contains an orphan closing accolade.");
								}
								else
								{
									// No } in deconstructed groups.
									builder.Append('}');

									// We're now ready to output that group.
									if (groupBuilders.Count > 4)
									{
										// We have the "1" or "-1" formatters.
										finalBuilder.Append(
											FormatGroup(
												provider,
												builder.ToString(),
												groupBuilders
													.Select(group => group.ToString())
													.ToArray(),
												args));
									}
									else
									{
										finalBuilder.Append(string.Format(provider, builder.ToString(), args));
									}
								}

								originalGroupBuilder.Clear();
								groupBuilders.Clear();
								currentGroupBuilder = null;
							}
							break;

						case ':':
							builder.Append(':');

							if (currentGroupBuilder != null)
							{
								// We have a first section after the index placeholder.
								groupBuilders.Add(currentGroupBuilder = new StringBuilder());
							}
							break;

						case ';':
							builder.Append(';');

							if (currentGroupBuilder != null)
							{
								// We have a new section in the group.
								groupBuilders.Add(currentGroupBuilder = new StringBuilder());
							}
							break;

						default:
							builder.Append(format[position]);
							currentGroupBuilder?.Append(format[position]);
							break;
					}
				}
			}

			if (originalGroupBuilder.Length > 0)
			{
				// We let string.Format handle this situation. This ensures we throw the same exceptions, 
				// if any, or return the same value.
				finalBuilder.Append(string.Format(provider, originalGroupBuilder.ToString(), args));
			}

			return finalBuilder.ToString();
		}

		private static string FormatGroup(IFormatProvider provider, string originalFormat, string[] deconstructedFormat, object[] args)
		{
			// deconstructedFormat[0] contains the index placeholder.
			// deconstructedFormat[1+] contain the sections.
			if (deconstructedFormat.Length < 5)
			{
				throw new ArgumentException("Do not call this method with formats that do not include a fourth or fifth section, for values 1 or -1.");
			}

			// We let those two lines throw if they have to.
			var position = int.Parse(deconstructedFormat[0]);
			var value = args[position];

			if (deconstructedFormat.Length >= 5)
			{
				// The 4th section is for "value == 1"
				if (IsOne(provider, $"{{0:{deconstructedFormat[1]}}}", value))
				{
					return string.Format(provider, $"{{0:{deconstructedFormat[4]}}}", value);
				}
			}

			if (deconstructedFormat.Length >= 6)
			{
				// An empty 5th group reverts to using the 4th.
				if (deconstructedFormat[5].Length == 0)
				{
					deconstructedFormat[5] = deconstructedFormat[4];
				}

				// The 5th section is for "value == -1".
				if (IsOne(provider, $"{{0:{deconstructedFormat[2]}}}", value, true))
				{
					return string.Format(provider, $"{{0:{deconstructedFormat[5]}}}", value);
				}
			}

			// string.Format tolerates the presence of extra sections. That's why it's safe to use the original format.
			return string.Format(provider, originalFormat, args);
		}

		private static bool IsOne(IFormatProvider provider, string format, object value, bool isNegative = false)
		{
			try
			{
				// Since groups can round items, we determine if we have one item by comparing the formatted value and formattting "1" or "-1".
				var formattedValue = string.Format(provider, format, value);
				var formattedOne = string.Format(provider, format, isNegative ? -1 : 1);

				if (formattedOne.Equals(formattedValue, StringComparison.Ordinal))
				{
					return true;
				}
			}
			catch { }

			return false;
		}
	}
}
