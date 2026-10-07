// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\inc\TVDiag.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using System.Globalization;
using System.Text;
using Uno.Foundation.Logging;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// TVDiag — logging helpers for TableView code.
//
// DbgLogF is #ifdef DBG-gated so retail builds drop both the call and any format-string cost.
//
// LogRetailF is retail-visible. Use ONLY where the control silently swallows something an app
// author would need to diagnose, and where the alternative is behaviour that is indistinguishable
// at runtime from "nothing happened": a consumer event handler that threw, or an edit cancel that
// could not restore the pre-edit value. These are error paths, so the cost is not on any hot path.
//
// PII rule: the %s formatter MUST wrap only HRESULT helpers or exception strings — NEVER cell
// values, column headers, or any user data.
internal static partial class TVDiag
{
	// TODO Uno: C++ namespace `details`, capitalized because an all-lowercase type name is CS8981.
	internal static partial class Details
	{
		// wchar_t buffer[1024]{} - StringCchVPrintfW truncates to ARRAYSIZE(buffer) - 1 characters.
		private const int c_bufferSize = 1024;

		private static readonly Logger s_log = typeof(TVDiag).Log();

		internal static void EmitV(string format, object?[] args)
		{
			var buffer = StringCchVPrintfW(c_bufferSize, format, args);
			// TODO Uno: OutputDebugStringW has no Uno equivalent; the message goes to the Uno logger at Debug level.
			// Original C++:
			// OutputDebugStringW(buffer);
			// OutputDebugStringW(L"\n");
			if (s_log.IsEnabled(LogLevel.Debug))
			{
				s_log.Debug(buffer);
			}
		}

		// TODO Uno: StringCchVPrintfW has no .NET equivalent. This formats the printf conversions the
		// Tabular call sites use (%ls, %s, %d, %u, %X, ...) with flags, width and precision, and
		// truncates like the fixed-size C++ buffer.
		private static string StringCchVPrintfW(int bufferSize, string format, object?[] args)
		{
			StringBuilder builder = new();
			var argIndex = 0;

			for (var i = 0; i < format.Length; i++)
			{
				var c = format[i];
				if (c != '%')
				{
					builder.Append(c);
					continue;
				}

				if (i + 1 < format.Length && format[i + 1] == '%')
				{
					builder.Append('%');
					i++;
					continue;
				}

				// Flags
				var leftAlign = false;
				var zeroPad = false;
				var plusSign = false;
				var spaceSign = false;
				var alternate = false;
				var j = i + 1;
				for (; j < format.Length; j++)
				{
					var flag = format[j];
					if (flag == '-')
					{
						leftAlign = true;
					}
					else if (flag == '0')
					{
						zeroPad = true;
					}
					else if (flag == '+')
					{
						plusSign = true;
					}
					else if (flag == ' ')
					{
						spaceSign = true;
					}
					else if (flag == '#')
					{
						alternate = true;
					}
					else
					{
						break;
					}
				}

				// Width
				var width = 0;
				if (j < format.Length && format[j] == '*')
				{
					width = argIndex < args.Length ? Convert.ToInt32(args[argIndex++], CultureInfo.InvariantCulture) : 0;
					if (width < 0)
					{
						leftAlign = true;
						width = -width;
					}
					j++;
				}
				else
				{
					while (j < format.Length && char.IsDigit(format[j]))
					{
						width = (width * 10) + (format[j] - '0');
						j++;
					}
				}

				// Precision
				var precision = -1;
				if (j < format.Length && format[j] == '.')
				{
					j++;
					precision = 0;
					if (j < format.Length && format[j] == '*')
					{
						precision = argIndex < args.Length ? Convert.ToInt32(args[argIndex++], CultureInfo.InvariantCulture) : 0;
						j++;
					}
					else
					{
						while (j < format.Length && char.IsDigit(format[j]))
						{
							precision = (precision * 10) + (format[j] - '0');
							j++;
						}
					}
				}

				// Length modifiers (h, hh, l, ll, z, j, t, L, w, I, I32, I64) do not change the managed formatting.
				while (j < format.Length && "hlzjtLwI".Contains(format[j]))
				{
					if (format[j] == 'I' && j + 2 < format.Length && ((format[j + 1] == '3' && format[j + 2] == '2') || (format[j + 1] == '6' && format[j + 2] == '4')))
					{
						j += 2;
					}
					j++;
				}

				if (j >= format.Length)
				{
					builder.Append(format, i, format.Length - i);
					break;
				}

				var conversion = format[j];
				var arg = argIndex < args.Length ? args[argIndex] : null;
				argIndex++;

				string text;
				switch (conversion)
				{
					case 's':
					case 'S':
						text = arg?.ToString() ?? "(null)";
						if (precision >= 0 && text.Length > precision)
						{
							text = text.Substring(0, precision);
						}
						zeroPad = false;
						break;
					case 'c':
					case 'C':
						text = new string(arg is char ch ? ch : (char)Convert.ToUInt16(arg, CultureInfo.InvariantCulture), 1);
						zeroPad = false;
						break;
					case 'd':
					case 'i':
						{
							var value = Convert.ToInt64(arg, CultureInfo.InvariantCulture);
							text = FormatInteger(value < 0 ? (ulong)(-(value + 1)) + 1 : (ulong)value, 10, false, precision);
							if (value < 0)
							{
								text = "-" + text;
							}
							else if (plusSign)
							{
								text = "+" + text;
							}
							else if (spaceSign)
							{
								text = " " + text;
							}
						}
						break;
					case 'u':
						text = FormatInteger(ToUInt64(arg), 10, false, precision);
						break;
					case 'x':
					case 'X':
						{
							var value = ToUInt64(arg);
							text = FormatInteger(value, 16, conversion == 'X', precision);
							if (alternate && value != 0)
							{
								text = (conversion == 'X' ? "0X" : "0x") + text;
							}
						}
						break;
					case 'o':
						text = FormatInteger(ToUInt64(arg), 8, false, precision);
						break;
					case 'p':
						text = ToUInt64(arg).ToString("X16", CultureInfo.InvariantCulture);
						zeroPad = false;
						break;
					case 'f':
					case 'F':
					case 'e':
					case 'E':
					case 'g':
					case 'G':
						{
							var value = Convert.ToDouble(arg, CultureInfo.InvariantCulture);
							var managedFormat = (conversion is 'f' or 'F' ? "F" : new string(conversion, 1)) + (precision >= 0 ? precision : 6).ToString(CultureInfo.InvariantCulture);
							text = value.ToString(managedFormat, CultureInfo.InvariantCulture);
							if (value >= 0 && plusSign)
							{
								text = "+" + text;
							}
						}
						break;
					default:
						// Unknown conversion: emit it verbatim.
						text = format.Substring(i, j - i + 1);
						argIndex--;
						break;
				}

				if (text.Length < width)
				{
					if (leftAlign)
					{
						text = text.PadRight(width);
					}
					else if (zeroPad && precision < 0)
					{
						var signLength = text.Length > 0 && (text[0] == '-' || text[0] == '+' || text[0] == ' ') ? 1 : 0;
						text = text.Insert(signLength, new string('0', width - text.Length));
					}
					else
					{
						text = text.PadLeft(width);
					}
				}

				builder.Append(text);
				i = j;
			}

			// The C++ buffer keeps a terminating null, so at most bufferSize - 1 characters survive.
			if (builder.Length > bufferSize - 1)
			{
				builder.Length = bufferSize - 1;
			}

			return builder.ToString();
		}

		private static ulong ToUInt64(object? arg) => arg switch
		{
			null => 0,
			sbyte v => unchecked((ulong)v),
			short v => unchecked((ulong)v),
			int v => unchecked((uint)v),
			long v => unchecked((ulong)v),
			nint v => unchecked((ulong)v),
			_ => Convert.ToUInt64(arg, CultureInfo.InvariantCulture),
		};

		private static string FormatInteger(ulong value, int radix, bool upper, int precision)
		{
			var text = radix switch
			{
				16 => value.ToString(upper ? "X" : "x", CultureInfo.InvariantCulture),
				8 => Convert.ToString(unchecked((long)value), 8),
				_ => value.ToString(CultureInfo.InvariantCulture),
			};

			if (precision == 0 && value == 0)
			{
				return "";
			}

			return precision > text.Length ? text.PadLeft(precision, '0') : text;
		}
	}

	// Intentionally NOT DBG-gated. See the header comment for when this is appropriate.
	internal static void LogRetailF(string format, params object?[] args)
	{
		try
		{
			Details.EmitV(format, args);
		}
		catch
		{
			// TODO Uno: the C++ function is noexcept; a formatting failure must not escape.
		}
	}

#if DEBUG
	internal static void DbgLogF(string format, params object?[] args)
	{
		Details.EmitV(format, args);
	}
#else
	internal static void DbgLogF(string format, params object?[] args)
	{
		// no-op in retail
	}
#endif
}
