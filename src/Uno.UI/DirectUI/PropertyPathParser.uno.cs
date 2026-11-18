#nullable enable

using System;

namespace DirectUI;

partial class PropertyPathParser
{
	private static string GetSegment(string source, int start, int length)
		=> start == 0 && length == source.Length ? source : source.Substring(start, length);

	// WinUI uses the CRT's C-locale wide-digit table, not the newer Unicode tables in .NET.
	// Its classification includes digits that _wtoi cannot convert.
	private static bool IsWideDigit(char c)
		=> c is >= '\u0030' and <= '\u0039'
			or >= '\u00B2' and <= '\u00B3'
			or '\u00B9'
			or >= '\u0660' and <= '\u0669'
			or >= '\u06F0' and <= '\u06F9'
			or >= '\u07C0' and <= '\u07C9'
			or >= '\u0966' and <= '\u096F'
			or >= '\u09E6' and <= '\u09EF'
			or >= '\u0A66' and <= '\u0A6F'
			or >= '\u0AE6' and <= '\u0AEF'
			or >= '\u0B66' and <= '\u0B6F'
			or >= '\u0BE6' and <= '\u0BEF'
			or >= '\u0C66' and <= '\u0C6F'
			or >= '\u0CE6' and <= '\u0CEF'
			or >= '\u0D66' and <= '\u0D6F'
			or >= '\u0E50' and <= '\u0E59'
			or >= '\u0ED0' and <= '\u0ED9'
			or >= '\u0F20' and <= '\u0F29'
			or >= '\u1040' and <= '\u1049'
			or >= '\u1090' and <= '\u1099'
			or >= '\u17E0' and <= '\u17E9'
			or >= '\u1810' and <= '\u1819'
			or >= '\u1946' and <= '\u194F'
			or >= '\u19D0' and <= '\u19D9'
			or >= '\u1B50' and <= '\u1B59'
			or >= '\u1BB0' and <= '\u1BB9'
			or >= '\u1C40' and <= '\u1C49'
			or >= '\u1C50' and <= '\u1C59'
			or >= '\uA620' and <= '\uA629'
			or >= '\uA8D0' and <= '\uA8D9'
			or >= '\uA900' and <= '\uA909'
			or >= '\uAA50' and <= '\uAA59'
			or >= '\uFF10' and <= '\uFF19';

	// Conversion ranges from Windows SDK 10.0.26100.0, ucrt/inc/corecrt_internal_strtox.h,
	// wide_character_to_digit. Values outside these ranges terminate _wtoi's conversion.
	private static int GetWideDigitValue(char c)
		=> c switch
		{
			>= '\u0030' and <= '\u0039' => c - '\u0030',
			>= '\u0660' and <= '\u0669' => c - '\u0660',
			>= '\u06F0' and <= '\u06F9' => c - '\u06F0',
			>= '\u0966' and <= '\u096F' => c - '\u0966',
			>= '\u09E6' and <= '\u09EF' => c - '\u09E6',
			>= '\u0A66' and <= '\u0A6F' => c - '\u0A66',
			>= '\u0AE6' and <= '\u0AEF' => c - '\u0AE6',
			>= '\u0B66' and <= '\u0B6F' => c - '\u0B66',
			>= '\u0C66' and <= '\u0C6F' => c - '\u0C66',
			>= '\u0CE6' and <= '\u0CEF' => c - '\u0CE6',
			>= '\u0D66' and <= '\u0D6F' => c - '\u0D66',
			>= '\u0E50' and <= '\u0E59' => c - '\u0E50',
			>= '\u0ED0' and <= '\u0ED9' => c - '\u0ED0',
			>= '\u0F20' and <= '\u0F29' => c - '\u0F20',
			>= '\u1040' and <= '\u1049' => c - '\u1040',
			>= '\u17E0' and <= '\u17E9' => c - '\u17E0',
			>= '\u1810' and <= '\u1819' => c - '\u1810',
			>= '\uFF10' and <= '\uFF19' => c - '\uFF10',
			_ => -1,
		};

	private static int ParseIntIndexer(ReadOnlySpan<char> szIndex)
	{
		var index = 0;
		foreach (var c in szIndex)
		{
			var digit = GetWideDigitValue(c);
			if (digit < 0)
			{
				break;
			}

			if (index > (int.MaxValue - digit) / 10)
			{
				return int.MaxValue;
			}

			index = index * 10 + digit;
		}

		return index;
	}
}
