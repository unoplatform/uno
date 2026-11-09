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
				stringBuilder.Append(CultureInfo.InvariantCulture.NumberFormat.NegativeSign);
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

			stringBuilder.Append(CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator);
			stringBuilder.Append('0', FractionDigits);
		}

		public void AppendFormatDouble(double value, StringBuilder stringBuilder)
		{
			// The integer part alone cannot carry the sign of a value in (-1, 0), so it is emitted up front.
			if (value < 0)
			{
				stringBuilder.Append(CultureInfo.InvariantCulture.NumberFormat.NegativeSign);
				value = -value;
			}

			// "F0" gives the exact integer digits of any finite double, beyond the range of int and ulong.
			var integerDigits = Math.Truncate(value).ToString("F0", CultureInfo.InvariantCulture);

			AppendFormatIntegerPart(integerDigits, stringBuilder);
			AppendFormatFractionPart(value, integerDigits.Length, stringBuilder);
		}

		public void AppendFormatInteger(ulong magnitude, bool isNegative, StringBuilder stringBuilder)
		{
			var numberFormat = CultureInfo.InvariantCulture.NumberFormat;

			if (isNegative)
			{
				stringBuilder.Append(numberFormat.NegativeSign);
			}

			var integerDigits = magnitude.ToString(CultureInfo.InvariantCulture);
			AppendIntegerDigits(integerDigits, stringBuilder);

			// An integer has no fraction of its own, so only the requested trailing zeros are shown.
			var integerLength = integerDigits.Length;
			var fractionDigits = Math.Max(FractionDigits, SignificantDigits - integerLength);

			if (fractionDigits > 0)
			{
				stringBuilder.Append(numberFormat.NumberDecimalSeparator);
				stringBuilder.Append('0', fractionDigits);
			}
			else if (IsDecimalPointAlwaysDisplayed)
			{
				stringBuilder.Append(numberFormat.NumberDecimalSeparator);
			}
		}

		private void AppendFormatIntegerPart(string integerDigits, StringBuilder stringBuilder)
		{
			if (integerDigits == "0" &&
				IntegerDigits == 0)
			{
				return;
			}

			AppendIntegerDigits(integerDigits, stringBuilder);
		}

		private void AppendIntegerDigits(string digits, StringBuilder stringBuilder)
		{
			var numberFormat = CultureInfo.InvariantCulture.NumberFormat;

			if (digits.Length < IntegerDigits)
			{
				digits = digits.PadLeft(IntegerDigits, '0');
			}

			if (IsGrouped)
			{
				var groupSize = numberFormat.NumberGroupSizes[0];
				var firstGroupLength = digits.Length % groupSize;
				if (firstGroupLength == 0)
				{
					firstGroupLength = groupSize;
				}

				stringBuilder.Append(digits, 0, firstGroupLength);
				for (var i = firstGroupLength; i < digits.Length; i += groupSize)
				{
					stringBuilder.Append(numberFormat.NumberGroupSeparator);
					stringBuilder.Append(digits, i, groupSize);
				}
			}
			else
			{
				stringBuilder.Append(digits);
			}
		}

		private void AppendFormatFractionPart(double value, int integerPartLen, StringBuilder stringBuilder)
		{
			var numberDecimalSeparator = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator;

			var fractionDigits = Math.Max(FractionDigits, SignificantDigits - integerPartLen);
			var rounded = Math.Round(value, fractionDigits, MidpointRounding.AwayFromZero);
			var needZeros = value == rounded;
			var formattedFractionPart = needZeros ? value.ToString($"F{fractionDigits}", CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture);
			var indexOfDecimalSeperator = formattedFractionPart.LastIndexOf(numberDecimalSeparator, StringComparison.Ordinal);

			if (indexOfDecimalSeperator != -1)
			{
				stringBuilder.Append(formattedFractionPart, indexOfDecimalSeperator, formattedFractionPart.Length - indexOfDecimalSeperator);
			}
			else if (IsDecimalPointAlwaysDisplayed)
			{
				stringBuilder.Append(CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator);
			}
		}

		private bool HasInvalidGroupSize(string text)
		{
			var numberFormat = CultureInfo.InvariantCulture.NumberFormat;
			var decimalSeperatorIndex = text.LastIndexOf(numberFormat.NumberDecimalSeparator, StringComparison.Ordinal);
			var groupSize = numberFormat.NumberGroupSizes[0];
			var groupSeperatorLength = numberFormat.NumberGroupSeparator.Length;
			var groupSeperator = numberFormat.NumberGroupSeparator;

			var preIndex = text.IndexOf(groupSeperator, StringComparison.Ordinal);
			var Index = -1;

			if (preIndex != -1)
			{
				while (preIndex + groupSeperatorLength < text.Length)
				{
					Index = text.IndexOf(groupSeperator, preIndex + groupSeperatorLength, StringComparison.Ordinal);

					if (Index == -1)
					{
						if (decimalSeperatorIndex - preIndex - groupSeperatorLength != groupSize)
						{
							return true;
						}

						break;
					}
					else if (Index - preIndex != groupSize)
					{
						return true;
					}

					preIndex = Index;
				}
			}

			return false;
		}

		public double? ParseDouble(string text)
		{
			if (text.IndexOf(' ') != -1)
			{
				return null;
			}

			if (HasInvalidGroupSize(text))
			{
				return null;
			}

			if (!double.TryParse(text,
				NumberStyles.Float | NumberStyles.AllowThousands,
				CultureInfo.InvariantCulture, out double value))
			{
				return null;
			}

			if (value == 0 &&
				text.IndexOf(CultureInfo.InvariantCulture.NumberFormat.NegativeSign, StringComparison.Ordinal) != -1)
			{
				return -0d;
			}

			return value;
		}
	}
}
