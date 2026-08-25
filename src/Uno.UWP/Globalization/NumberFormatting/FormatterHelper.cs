#nullable enable

using System;
using System.Globalization;
using System.Text;
using Windows.Globalization.NumberFormatting;

namespace Uno.Globalization.NumberFormatting
{
	internal partial class FormatterHelper : ISignificantDigitsOption, ISignedZeroOption
	{
		// WinRT uses the same infinity symbols for every locale, always with an ASCII hyphen, while the
		// NaN symbol follows the locale (see NaNSymbol).
		private const string PositiveInfinitySymbol = "∞";
		private const string ZeroDigits = "0";

		// Doubles round-trip through 17 significant digits, which is the precision WinRT prints.
		private const string RoundTripFormat = "G17";

		private const NumberStyles IntegerStyles = NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands;
		private const NumberStyles DecimalStyles = IntegerStyles | NumberStyles.AllowDecimalPoint;

		public FormatterHelper()
		{
		}

		/// <summary>
		/// Gets or sets the format used for punctuation, grouping, and signs.
		/// It remains invariant when <see cref="NumeralSystemTranslator"/> localizes punctuation.
		/// </summary>
		public NumberFormatInfo NumberFormat { get; set; } = CultureInfo.InvariantCulture.NumberFormat;

		/// <summary>
		/// Gets or sets the NaN symbol of the resolved locale, which is independent of
		/// <see cref="NumberFormat"/> because Arabic-Indic numeral systems format punctuation invariantly.
		/// </summary>
		public string NaNSymbol { get; set; } = "NaN";

		public bool IsDecimalPointAlwaysDisplayed { get; set; }

		public int IntegerDigits { get; set; } = 1;

		public bool IsGrouped { get; set; }

		public int FractionDigits { get; set; } = 2;

		public bool IsZeroSigned { get; set; }

		public int SignificantDigits { get; set; }

		private string NegativeInfinitySymbol => NumberFormat.NegativeSign + PositiveInfinitySymbol;

		public bool TryValidate(double value, out string text)
		{
			if (double.IsNaN(value))
			{
				text = NaNSymbol;
				return false;
			}

			if (double.IsPositiveInfinity(value))
			{
				text = PositiveInfinitySymbol;
				return false;
			}

			if (double.IsNegativeInfinity(value))
			{
				text = NegativeInfinitySymbol;
				return false;
			}

			text = "";
			return true;
		}

		/// <summary>
		/// Parses back the NaN/infinity symbols produced by <see cref="TryValidate"/>.
		/// </summary>
		/// <remarks>
		/// WinRT round-trips these case-sensitively and rejects any sign variation (for example "+∞"
		/// or a U+2212 MINUS SIGN), so the comparison is ordinal and exact.
		/// </remarks>
		public bool TryParseSpecialValue(string text, out double value)
		{
			if (string.Equals(text, NaNSymbol, StringComparison.Ordinal))
			{
				value = double.NaN;
				return true;
			}

			if (string.Equals(text, PositiveInfinitySymbol, StringComparison.Ordinal))
			{
				value = double.PositiveInfinity;
				return true;
			}

			if (string.Equals(text, NegativeInfinitySymbol, StringComparison.Ordinal))
			{
				value = double.NegativeInfinity;
				return true;
			}

			value = 0d;
			return false;
		}

		public void AppendFormatZero(double value, StringBuilder stringBuilder)
		{
			if (IsZeroSigned && value.IsNegative())
			{
				stringBuilder.Append(NumberFormat.NegativeSign);
			}

			AppendFormatZero(stringBuilder);
		}

		public void AppendFormatZero(StringBuilder stringBuilder) =>
			AppendFormatIntegral(false, ZeroDigits, stringBuilder);

		/// <summary>
		/// Formats an integral value given as its sign and its exact magnitude digits.
		/// </summary>
		public void AppendFormatIntegral(bool isNegative, string digits, StringBuilder stringBuilder)
		{
			if (FractionDigits == 0 &&
				IntegerDigits == 0 &&
				IsZero(digits))
			{
				stringBuilder.Append('0');
			}

			AppendIntegerPart(isNegative, digits, stringBuilder);

			var fractionDigits = GetFractionDigits(digits.Length);

			if (!IsDecimalPointAlwaysDisplayed &&
				fractionDigits == 0)
			{
				return;
			}

			stringBuilder.Append(NumberFormat.NumberDecimalSeparator);
			stringBuilder.Append('0', fractionDigits);
		}

		public void AppendFormatDouble(double value, StringBuilder stringBuilder)
		{
			var digits = GetIntegerDigits(Math.Abs(Math.Truncate(value)));

			AppendIntegerPart(value < 0, digits, stringBuilder);
			AppendFormatFractionPart(value, GetFractionDigits(digits.Length), stringBuilder);
		}

		private static bool IsZero(string digits) => digits.Length == 1 && digits[0] == '0';

		private int GetFractionDigits(int integerDigitCount) =>
			Math.Max(FractionDigits, SignificantDigits - integerDigitCount);

		/// <summary>
		/// Gets the exact integer digits of an integral <paramref name="magnitude"/>.
		/// </summary>
		/// <remarks>
		/// A custom numeric picture would round to 15 significant digits and "F0" would print the exact
		/// binary expansion; WinRT prints the 17 significant digits that round-trip a double, padded with
		/// zeros, which is what expanding the round-trip form produces.
		/// </remarks>
		private static string GetIntegerDigits(double magnitude)
		{
			var text = magnitude.ToString(RoundTripFormat, CultureInfo.InvariantCulture);
			var exponentIndex = text.IndexOf('E');

			if (exponentIndex < 0)
			{
				return text;
			}

			var exponent = int.Parse(text.Substring(exponentIndex + 1), CultureInfo.InvariantCulture);
			var mantissa = text.Substring(0, exponentIndex);
			var pointIndex = mantissa.IndexOf('.');
			var digits = pointIndex < 0 ? mantissa : mantissa.Remove(pointIndex, 1);
			var trailingZeros = exponent - (pointIndex < 0 ? 0 : mantissa.Length - pointIndex - 1);

			return trailingZeros <= 0 ? digits : digits + new string('0', trailingZeros);
		}

		private void AppendIntegerPart(bool isNegative, string digits, StringBuilder stringBuilder)
		{
			if (isNegative)
			{
				stringBuilder.Append(NumberFormat.NegativeSign);
			}

			if (IntegerDigits == 0 &&
				IsZero(digits))
			{
				return;
			}

			var padding = IntegerDigits - digits.Length;

			AppendGroupedDigits(padding > 0 ? new string('0', padding) + digits : digits, stringBuilder);
		}

		private void AppendGroupedDigits(string digits, StringBuilder stringBuilder)
		{
			if (!IsGrouped)
			{
				stringBuilder.Append(digits);
				return;
			}

			var groupSizes = NumberFormat.NumberGroupSizes;
			var lastSizeIndex = groupSizes.Length - 1;
			var remaining = digits.Length;
			var sizeIndex = 0;
			var groupCount = 0;

			// Walk the sizes from the least significant end; the last entry repeats and a zero stops grouping.
			while (true)
			{
				var size = groupSizes[Math.Min(sizeIndex, lastSizeIndex)];

				if (size <= 0 ||
					remaining <= size)
				{
					break;
				}

				remaining -= size;
				groupCount++;

				if (sizeIndex < lastSizeIndex)
				{
					sizeIndex++;
				}
			}

			var separator = NumberFormat.NumberGroupSeparator;
			var index = remaining;

			stringBuilder.Append(digits, 0, remaining);

			// The sizes appear mirrored when emitting left to right: the repeating entry first, then the
			// leading entries in reverse order.
			for (var repeat = groupCount - lastSizeIndex; repeat > 0; repeat--)
			{
				stringBuilder.Append(separator);
				stringBuilder.Append(digits, index, groupSizes[lastSizeIndex]);
				index += groupSizes[lastSizeIndex];
			}

			for (var i = Math.Min(groupCount, lastSizeIndex) - 1; i >= 0; i--)
			{
				stringBuilder.Append(separator);
				stringBuilder.Append(digits, index, groupSizes[i]);
				index += groupSizes[i];
			}
		}

		private void AppendFormatFractionPart(double value, int fractionDigits, StringBuilder stringBuilder)
		{
			var numberDecimalSeparator = NumberFormat.NumberDecimalSeparator;

			var rounded = Math.Round(value, fractionDigits, MidpointRounding.AwayFromZero);
			var needZeros = value == rounded;
			var formattedFractionPart = needZeros ? value.ToString($"F{fractionDigits}", NumberFormat) : value.ToString(NumberFormat);
			var indexOfDecimalSeperator = formattedFractionPart.LastIndexOf(numberDecimalSeparator, StringComparison.Ordinal);

			if (indexOfDecimalSeperator != -1)
			{
				stringBuilder.Append(formattedFractionPart, indexOfDecimalSeperator, formattedFractionPart.Length - indexOfDecimalSeperator);
			}
			else if (IsDecimalPointAlwaysDisplayed)
			{
				stringBuilder.Append(NumberFormat.NumberDecimalSeparator);
			}
		}

		private bool HasInvalidGroupSize(string text)
		{
			var groupSeparator = NumberFormat.NumberGroupSeparator;
			if (string.IsNullOrEmpty(groupSeparator) ||
				!text.Contains(groupSeparator, StringComparison.Ordinal))
			{
				return false;
			}

			var decimalSeparatorIndex = text.LastIndexOf(NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal);
			var integerPart = decimalSeparatorIndex >= 0 ? text.Substring(0, decimalSeparatorIndex) : text;
			if (integerPart.StartsWith(NumberFormat.NegativeSign, StringComparison.Ordinal))
			{
				integerPart = integerPart.Substring(NumberFormat.NegativeSign.Length);
			}
			else if (integerPart.StartsWith(NumberFormat.PositiveSign, StringComparison.Ordinal))
			{
				integerPart = integerPart.Substring(NumberFormat.PositiveSign.Length);
			}

			var groups = integerPart.Split([groupSeparator], StringSplitOptions.None);
			var groupSizes = NumberFormat.NumberGroupSizes;
			var groupSizeIndex = 0;

			for (var groupIndex = groups.Length - 1; groupIndex > 0; groupIndex--)
			{
				var expectedSize = groupSizes[Math.Min(groupSizeIndex, groupSizes.Length - 1)];
				if (expectedSize == 0 || groups[groupIndex].Length != expectedSize)
				{
					return true;
				}

				if (groupSizeIndex < groupSizes.Length - 1)
				{
					groupSizeIndex++;
				}
			}

			var leftmostExpectedSize = groupSizes[Math.Min(groupSizeIndex, groupSizes.Length - 1)];
			return groups[0].Length == 0 ||
				leftmostExpectedSize > 0 && groups[0].Length > leftmostExpectedSize;
		}

		/// <summary>
		/// Rejects the space characters .NET's parser accepts interchangeably but WinRT does not.
		/// </summary>
		/// <remarks>
		/// WinRT treats SPACE and NO-BREAK SPACE as the same separator but NARROW NO-BREAK SPACE as a
		/// distinct one, while .NET accepts all three whenever any of them is the group separator.
		/// </remarks>
		private bool HasUnsupportedSpace(string text)
		{
			var groupSeparator = NumberFormat.NumberGroupSeparator;
			var separator = groupSeparator.Length == 1 ? groupSeparator[0] : '\0';
			var allowsSpace = separator is ' ' or '\u00a0';
			var allowsNarrowSpace = separator is '\u202f';

			foreach (var character in text)
			{
				switch (character)
				{
					case ' ':
					case '\u00a0':
						if (!allowsSpace)
						{
							return true;
						}
						break;
					case '\u202f':
						if (!allowsNarrowSpace)
						{
							return true;
						}
						break;
				}
			}

			return false;
		}

		private bool IsParseable(string text) =>
			(string.IsNullOrEmpty(NumberFormat.PositiveSign) ||
				!text.StartsWith(NumberFormat.PositiveSign, StringComparison.Ordinal)) &&
			!HasUnsupportedSpace(text) &&
			!HasInvalidGroupSize(text);

		public double? ParseDouble(string text)
		{
			if (!IsParseable(text))
			{
				return null;
			}

			if (!double.TryParse(text, DecimalStyles, NumberFormat, out double value))
			{
				return null;
			}

			if (double.IsNaN(value) ||
				double.IsInfinity(value))
			{
				// .NET recognizes the NaN/infinity symbols case-insensitively, with surrounding
				// whitespace and regardless of NumberStyles. WinRT only accepts the exact symbols
				// handled by TryParseSpecialValue, so anything reaching here is not a number.
				return null;
			}

			if (value == 0 &&
				text.StartsWith(NumberFormat.NegativeSign, StringComparison.Ordinal))
			{
				return -0d;
			}

			return value;
		}

		public long? ParseInt(string text) =>
			TryGetIntegerText(text, out var integerText) &&
			long.TryParse(integerText, IntegerStyles, NumberFormat, out var value)
				? value
				: null;

		public ulong? ParseUInt(string text) =>
			!text.StartsWith(NumberFormat.NegativeSign, StringComparison.Ordinal) &&
			TryGetIntegerText(text, out var integerText) &&
			ulong.TryParse(integerText, IntegerStyles, NumberFormat, out var value)
				? value
				: null;

		/// <summary>
		/// Extracts the integer part of a text that WinRT would accept as an integer.
		/// </summary>
		/// <remarks>
		/// WinRT accepts a fraction made only of zeros ("42.00" and "-.0" are integers) but rejects any
		/// non-zero fraction digit, even one the conversion to double would round away.
		/// </remarks>
		private bool TryGetIntegerText(string text, out string integerText)
		{
			integerText = "";

			if (!IsParseable(text))
			{
				return false;
			}

			var separator = NumberFormat.NumberDecimalSeparator;
			var separatorIndex = text.LastIndexOf(separator, StringComparison.Ordinal);
			var hasFractionDigit = false;

			if (separatorIndex < 0)
			{
				integerText = text;
			}
			else
			{
				for (var i = separatorIndex + separator.Length; i < text.Length; i++)
				{
					if (text[i] != '0')
					{
						return false;
					}

					hasFractionDigit = true;
				}

				integerText = text.Substring(0, separatorIndex);
			}

			if (!ContainsDigit(integerText))
			{
				if (!hasFractionDigit)
				{
					return false;
				}

				integerText += ZeroDigits;
			}

			return true;
		}

		private static bool ContainsDigit(string text)
		{
			foreach (var character in text)
			{
				if (character is >= '0' and <= '9')
				{
					return true;
				}
			}

			return false;
		}
	}
}
