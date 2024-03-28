using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Windows.UI.Xaml.Documents.TextFormatting
{
	internal static class Unicode
	{
		public static bool IsLineBreak(char c)
		{
			// https://www.unicode.org/standard/reports/tr13/tr13-5.html

			switch (c)
			{
				case '\u000A':
				case '\u000B':
				case '\u000C':
				case '\u000D':
				case '\u0085':
				case '\u2028':
				case '\u2029': // Paragraph separator (should apply paragraph formatting, i.e. paragraph spacing/indentation on new line, unlike other line
							   // breaks - could matter if/when Paragraph.TextIndent/RichTextBlock.TextIndent is implemented (UWP/WinUI conformance to this
							   // behavior was not tested).
					return true;
				default:
					return false;
			}
		}

		// TODO: Implement more of the Unicode Line Breaking Algorithm: https://www.unicode.org/reports/tr14/
		//
		// This is a simple 95% implementation for latin-based langauges that closely matches UWP behavior, but is missing asian/hebrew and more obscure rules
		// (i.e. soft-hyphens - did not test if UWP/WinUI supports those). Not sure which version we want to use here, since UWP/WPF appear to be using
		// different variations on pre-2010 versions of the spec (which makes sense), so a modern implementation will behave "better" but vary significantly
		// from UWP in many ways like:
		// - PO: Postfix Numeric (XB): there is no break opportunity in "(12.00) %"
		// - PR: Prefix Numeric (XA): there is no break opportunity in "$ (100.00)"
		// - Differing behavior for determining numeric context for hyphen word break opportunities
		//
		// Curious to see if WinUI uses a more modern implementation or same one UWP does. If it uses the old one, do we prefer matching behavior more closely
		// with UWP/WinUI or using the "better"/more modern algorithm versions?

		public static bool HasWordBreakOpportunityBefore(ReadOnlySpan<char> s, int i)
		{
			char c = s[i];

			switch (c)
			{
				case '\u2014': // Em dash
					return true;
				default:

					// WPF behavior - commented out because UWP does not word break on opening parenthesis. Did not test how WinUI behaves yet.
					// return char.GetUnicodeCategory(c) == UnicodeCategory.OpenPunctuation;

					return false;
			}
		}

		public static bool HasWordBreakOpportunityAfter(ReadOnlySpan<char> s, int i)
		{
			char c = s[i];

			switch (c)
			{
				case '-':
					return !NumericContextFollows(s, i);

				// WPF behavior, not present in UWP. Did not test how WinUI behaves yet.
				//case '/':

				// Hyphens:
				case '\u058A':
				case '\u2010':
				case '\u2012':
				case '\u2013':

				case '\u2014': // Em dash
					return true;

				// Non-breaking spaces:
				case '\u00A0':
				case '\u202F':
				case '\u2007':
				case '\u2060':
					return false;

				default:
					// WPF behavior - commented out because UWP does not word break on closing parenthesis. Did not test how WinUI behaves yet.
					// if (char.GetUnicodeCategory(c) == UnicodeCategory.ClosePunctuation)
					//     return true;

					return char.IsWhiteSpace(c) && char.GetUnicodeCategory(c) != UnicodeCategory.SpacingCombiningMark;
			}
		}

		private static bool NumericContextFollows(ReadOnlySpan<char> s, int i)
		{
			// Matches observed UWP behavior in most cases:

			if (++i >= s.Length)
			{
				return false;
			}

			char c = s[i];

			switch (c)
			{
				case '+':
				case '-':
				case '.':
				case ',':
				case '~':
				case '#':
				case '%':
				case '^':
				case '&':
				case '*':
				case '/':
				case '\\':
				case '|':
					return true;
				default:
					return char.IsDigit(c) || char.GetUnicodeCategory(c) == UnicodeCategory.CurrencySymbol;
			}
		}
	}
}
