using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Windows.Globalization.NumberFormatting
{
	public partial class DecimalFormatter : INumberFormatterOptions, INumberFormatter, INumberFormatter2, INumberParser, ISignificantDigitsOption, INumberRounderOption, ISignedZeroOption
	{
		private readonly NumeralSystemTranslator _translator;

		public DecimalFormatter()
		{
			_translator = new NumeralSystemTranslator();
		}

		public bool IsDecimalPointAlwaysDisplayed { get; set; }

		public int IntegerDigits { get; set; } = 1;

		public bool IsGrouped { get; set; }

		public string NumeralSystem
		{
			get => _translator.NumeralSystem;
			set => _translator.NumeralSystem = value;
		}

		public IReadOnlyList<string> Languages => _translator.Languages;

		public string ResolvedLanguage => _translator.ResolvedLanguage;

		public int FractionDigits { get; set; } = 2;

		public INumberRounder NumberRounder { get; set; }

		public bool IsZeroSigned { get; set; }

		public int SignificantDigits { get; set; } = 0;

		public string Format(double value) => FormatDouble(value);

		public string FormatDouble(double value)
		{
			if (double.IsNaN(value))
			{
				return "NaN";
			}

			if (double.IsPositiveInfinity(value))
			{
				return "∞";
			}

			if (double.IsNegativeInfinity(value))
			{
				return "-∞";
			}

			if (NumberRounder != null)
			{
				value = NumberRounder.RoundDouble(value);
			}

			bool isNegative = BitConverter.DoubleToInt64Bits(value) < 0;

			if (value == 0d &&
				IntegerDigits == 0 &&
				FractionDigits == 0)
			{
				if (isNegative && IsZeroSigned)
				{
					var r = CultureInfo.InvariantCulture.NumberFormat.NegativeSign + "0";
					return _translator.TranslateNumerals(r);
				}
				else
				{
					return _translator.TranslateNumerals("0");
				}
			}

			bool addMinusSign = isNegative && value == 0;

			var formattedFractionPart = FormatFractionPart(value);
			var formattedIntegerPart = FormatIntegerPart(value);
			var formatted = formattedIntegerPart + formattedFractionPart;

			if (IsDecimalPointAlwaysDisplayed &&
				formattedFractionPart == string.Empty)
			{
				formatted += CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator;
			}

			if (addMinusSign && IsZeroSigned)
			{
				formatted = CultureInfo.InvariantCulture.NumberFormat.NegativeSign + formatted;
			}

			formatted = _translator.TranslateNumerals(formatted);
			return formatted;
		}

		private string FormatIntegerPart(double value)
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

		private string FormatFractionPart(double value)
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

		public double? ParseDouble(string text)
		{
			text = _translator.TranslateBackNumerals(text);

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
	}
}
