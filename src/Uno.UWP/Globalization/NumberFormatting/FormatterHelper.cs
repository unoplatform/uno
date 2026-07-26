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
		/// The <see cref="NumberFormatInfo"/> used to format/parse the decimal separator, group
		/// separator and group sizes, and negative sign.
		/// </summary>
		/// <remarks>
		/// This intentionally defaults to <see cref="CultureInfo.InvariantCulture"/> (ASCII '.'/',' punctuation).
		/// <see cref="NumeralSystemTranslator"/> assumes this punctuation is present in the formatted text so it
		/// can translate it (e.g. to Arabic-Indic separators) without double-localizing it; callers that resolve
		/// a locale-specific <see cref="NumberFormatInfo"/> (e.g. for decimal-comma locales) must only do so for
		/// numeral systems the translator does not itself localize the punctuation for.
		///
		/// <see cref="AppendFormatIntegerPart"/>'s custom numeric picture format string (e.g. "00,0") correctly
		/// honors this <see cref="NumberFormatInfo"/>'s <see cref="NumberFormatInfo.NumberGroupSizes"/> when
		/// formatted via <see cref="StringBuilder.AppendFormat(IFormatProvider, string, object)"/>, including
		/// non-uniform grouping (e.g. "en-IN" lakh/crore grouping, group sizes {3, 2}) - see
		/// Given_DecimalFormatter.When_NonUniformGroupSizeLocale_Then_GroupsUsingLocaleGroupSizes. However,
		/// <see cref="HasInvalidGroupSize"/> (used by <see cref="ParseDouble"/>) only ever consults
		/// <see cref="NumberFormatInfo.NumberGroupSizes"/>[0] and validates every gap between separators against
		/// that single size, so parsing text with non-uniform grouping may incorrectly reject (or, per its
		/// off-by-one comparison, incorrectly accept) some otherwise-valid input. This is a pre-existing
		/// FormatterHelper limitation, unrelated to locale resolution, and out of scope here.
		/// </remarks>
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
			var numberFormat = NumberFormat;
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
