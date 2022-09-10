using System;
using System.Globalization;
using System.Linq;
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

			text = null;
			return true;
		}

		public string FormatZero(double value)
		{
			var result = FormatZeroCore();
			var isNegative = BitConverter.DoubleToInt64Bits(value) < 0;

			if (IsZeroSigned && isNegative)
			{
				result = $"{CultureInfo.InvariantCulture.NumberFormat.NegativeSign}{result}";
			}

			return result;
		}

		public string FormatZeroCore()
		{
			if (FractionDigits == 0 &&
				IntegerDigits == 0)
			{
				return "0";
			}

			var integerPart = new string('0', IntegerDigits);
			var fractionPart = new string('0', FractionDigits);

			if (!IsDecimalPointAlwaysDisplayed &&
				FractionDigits == 0)
			{
				return integerPart;
			}

			return $"{integerPart}{CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator}{fractionPart}";
		}

		public string FormatDoubleCore(double value)
		{
			var formattedFractionPart = FormatFractionPart(value);
			var formattedIntegerPart = FormatIntegerPart(value);
			var formatted = formattedIntegerPart + formattedFractionPart;

			if (IsDecimalPointAlwaysDisplayed &&
				formattedFractionPart == string.Empty)
			{
				formatted += CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator;
			}

			return formatted;
		}

		public string FormatIntegerPart(double value)
		{
			var integerPart = (int)Math.Truncate(value);

			if (integerPart == 0 &&
				IntegerDigits == 0)
			{
				return string.Empty;
			}
			else if (IsGrouped)
			{
				var zeros = new string(Enumerable.Repeat('0', IntegerDigits - 1).ToArray());
				var format = string.Concat(zeros, ",0");
				return integerPart.ToString(format, CultureInfo.InvariantCulture);
			}
			else
			{
				return integerPart.ToString($"D{IntegerDigits}", CultureInfo.InvariantCulture);
			}
		}

		public string FormatFractionPart(double value)
		{
			var numberDecimalSeparator = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator;

			var integerPart = (int)Math.Truncate(value);
			var integerPartLen = integerPart.GetLength();
			var fractionDigits = Math.Max(FractionDigits, SignificantDigits - integerPartLen);
			var rounded = Math.Round(value, fractionDigits, MidpointRounding.AwayFromZero);
			var needZeros = value == rounded;
			var formattedFractionPart = needZeros ? value.ToString($"F{fractionDigits}", CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture);
			var indexOfDecimalSeperator = formattedFractionPart.LastIndexOf(numberDecimalSeparator, StringComparison.Ordinal);

			if (indexOfDecimalSeperator == -1)
			{
				formattedFractionPart = string.Empty;
			}
			else
			{
				formattedFractionPart = formattedFractionPart.Substring(indexOfDecimalSeperator);
			}

			return formattedFractionPart;
		}

		public bool HasInvalidGroupSize(string text)
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

		public double? ParseDoubleCore(string text)
		{
			if (text.IndexOf(" ", StringComparison.Ordinal) != -1)
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
