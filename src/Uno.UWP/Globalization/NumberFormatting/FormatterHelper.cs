#nullable enable

using System;
using System.Globalization;
using System.Text;
using Windows.Globalization.NumberFormatting;

namespace Uno.Globalization.NumberFormatting
{
	internal partial class FormatterHelper : ISignificantDigitsOption, ISignedZeroOption
	{
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
				text = "NaN";
				return false;
			}

			if (double.IsPositiveInfinity(value))
			{
				text = "∞";
				return false;
			}

			if (double.IsNegativeInfinity(value))
			{
				text = "-∞";
				return false;
			}

			text = "";
			return true;
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

			stringBuilder.Append('0', IntegerDigits);

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
			var integerPart = (int)Math.Truncate(value);

			if (integerPart == 0 &&
				IntegerDigits == 0)
			{
				return;
			}

			var formatBuilder = StringBuilderCache.Acquire();

			if (IsGrouped)
			{
				formatBuilder.Append("{0:");
				formatBuilder.Append('0', IntegerDigits - 1);
				formatBuilder.Append(",0}");
			}
			else
			{
				formatBuilder.Append("{0:D");
				formatBuilder.Append(IntegerDigits);
				formatBuilder.Append('}');
			}

			var format = StringBuilderCache.GetStringAndRelease(formatBuilder);
			stringBuilder.AppendFormat(NumberFormat, format, integerPart);
		}

		private void AppendFormatFractionPart(double value, StringBuilder stringBuilder)
		{
			var numberDecimalSeparator = NumberFormat.NumberDecimalSeparator;

			var integerPart = (int)Math.Truncate(value);
			var integerPartLen = integerPart.GetLength();
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
				NumberStyles.AllowThousands |
				NumberStyles.AllowExponent,
				NumberFormat, out double value))
			{
				return null;
			}

			if (value == 0 &&
				text.IndexOf(NumberFormat.NegativeSign, StringComparison.Ordinal) != -1)
			{
				return -0d;
			}

			return value;
		}
	}
}
