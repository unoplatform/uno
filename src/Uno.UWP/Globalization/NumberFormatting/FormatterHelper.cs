#nullable enable

using System;
using System.Globalization;
using System.Text;
using Windows.Globalization.NumberFormatting;

namespace Uno.Globalization.NumberFormatting
{
	internal partial class FormatterHelper : ISignificantDigitsOption, ISignedZeroOption
	{
		// WinRT formats (and parses back) these exact symbols for every locale, independently of the
		// NaN/infinity symbols reported by the locale data.
		private const string NaNSymbol = "NaN";
		private const string PositiveInfinitySymbol = "∞";
		private const string NegativeInfinitySymbol = "-∞";

		// 2^63, the first magnitude a long can no longer represent.
		private const double MaxExactLongMagnitude = 9223372036854775808d;

		public FormatterHelper()
		{
		}

		/// <summary>
		/// Gets or sets the format used for punctuation, grouping, and signs.
		/// It remains invariant when <see cref="NumeralSystemTranslator"/> localizes punctuation.
		/// </summary>
		public NumberFormatInfo NumberFormat { get; set; } = CultureInfo.InvariantCulture.NumberFormat;

		public bool IsDecimalPointAlwaysDisplayed { get; set; }

		public int IntegerDigits { get; set; } = 1;

		public bool IsGrouped { get; set; }

		public int FractionDigits { get; set; } = 2;

		public bool IsZeroSigned { get; set; }

		public int SignificantDigits { get; set; }

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
		public static bool TryParseSpecialValue(string text, out double value)
		{
			switch (text)
			{
				case NaNSymbol:
					value = double.NaN;
					return true;
				case PositiveInfinitySymbol:
					value = double.PositiveInfinity;
					return true;
				case NegativeInfinitySymbol:
					value = double.NegativeInfinity;
					return true;
				default:
					value = 0d;
					return false;
			}
		}

		public void AppendFormatZero(double value, StringBuilder stringBuilder)
		{
			var isNegative = value.IsNegative();

			if (IsZeroSigned && isNegative)
			{
				stringBuilder.Append(NumberFormat.NegativeSign);
			}

			AppendFormatZero(stringBuilder);
		}

		public void AppendFormatZero(StringBuilder stringBuilder)
		{
			if (FractionDigits == 0 &&
				IntegerDigits == 0)
			{
				stringBuilder.Append('0');
			}

			// Zero goes through the same picture as any other value so that grouping and the
			// locale group sizes still apply to the padded integer digits.
			AppendFormatIntegerPart(0d, stringBuilder);

			if (!IsDecimalPointAlwaysDisplayed &&
				FractionDigits == 0)
			{
				return;
			}

			stringBuilder.Append(NumberFormat.NumberDecimalSeparator);
			stringBuilder.Append('0', FractionDigits);
		}

		public void AppendFormatDouble(double value, StringBuilder stringBuilder)
		{
			AppendFormatIntegerPart(value, stringBuilder);
			AppendFormatFractionPart(value, stringBuilder);
		}

		private void AppendFormatIntegerPart(double value, StringBuilder stringBuilder)
		{
			// Truncate first: a custom numeric picture rounds, and only the integer digits belong here.
			var integerPart = Math.Truncate(value);

			if (integerPart == 0 &&
				IntegerDigits == 0)
			{
				// The picture would emit nothing, so the sign of a value such as -0.5 has to be carried over.
				if (value < 0)
				{
					stringBuilder.Append(NumberFormat.NegativeSign);
				}

				return;
			}

			var formatBuilder = StringBuilderCache.Acquire();
			formatBuilder.Append("{0:");

			if (IsGrouped)
			{
				// A "," only acts as the group separator when it sits between two digit placeholders,
				// so the leading "#" is required for IntegerDigits == 1.
				formatBuilder.Append("#,");
			}

			if (IntegerDigits == 0)
			{
				formatBuilder.Append('#');
			}
			else
			{
				formatBuilder.Append('0', IntegerDigits);
			}

			formatBuilder.Append('}');

			var format = StringBuilderCache.GetStringAndRelease(formatBuilder);

			// A custom picture applied to a double rounds to 15 significant digits, so route the
			// integer part through long whenever it fits to keep every digit of values such as 2^53.
			// Zero stays on the double path so that the sign of -0 survives (for example "-0.50").
			var formattable = integerPart != 0 && Math.Abs(integerPart) < MaxExactLongMagnitude
				? (object)(long)integerPart
				: integerPart;

			stringBuilder.AppendFormat(NumberFormat, format, formattable);
		}

		private void AppendFormatFractionPart(double value, StringBuilder stringBuilder)
		{
			var numberDecimalSeparator = NumberFormat.NumberDecimalSeparator;

			var integerPartLen = value.GetIntegerDigitCount();
			var fractionDigits = Math.Max(FractionDigits, SignificantDigits - integerPartLen);
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

		public double? ParseDouble(string text)
		{
			if (!string.IsNullOrEmpty(NumberFormat.PositiveSign) &&
				text.StartsWith(NumberFormat.PositiveSign, StringComparison.Ordinal))
			{
				return null;
			}

			if (HasInvalidGroupSize(text))
			{
				return null;
			}

			if (!double.TryParse(text,
				NumberStyles.AllowLeadingSign |
				NumberStyles.AllowDecimalPoint |
				NumberStyles.AllowThousands,
				NumberFormat, out double value))
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
	}
}
