#nullable enable

using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Uno.Globalization.NumberFormatting;

internal static class MathExtensions
{
	public static int GetLength(this int input)
	{
		if (input == 0)
		{
			return 1;
		}

		return (int)Math.Floor(Math.Log10(Math.Abs(input))) + 1;
	}

	public static double MultiplyByPow10(this double value, int pow10)
	{
		if (double.IsInfinity(value))
		{
			return value;
		}

		if (double.IsNaN(value))
		{
			return value;
		}

		var numberDecimalSeparator = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator;
		var text = value.ToString(CultureInfo.InvariantCulture);
		var indexOfSeperator = text.IndexOf(numberDecimalSeparator, StringComparison.Ordinal);

		if (indexOfSeperator == -1)
		{
			var stringBuilder = new StringBuilder();
			stringBuilder.Append(text);
			stringBuilder.Append('0', pow10);
			return double.Parse(stringBuilder.ToString(), CultureInfo.InvariantCulture);
		}

		var fractionLength = text.Length - indexOfSeperator - numberDecimalSeparator.Length;
		var diff = fractionLength - pow10;

		if (diff <= 0)
		{
			var stringBuilder = new StringBuilder();
			stringBuilder.Append(text);
			stringBuilder.Remove(indexOfSeperator, numberDecimalSeparator.Length);
			stringBuilder.Append('0', -diff);
			return double.Parse(stringBuilder.ToString(), CultureInfo.InvariantCulture);
		}
		else
		{
			var stringBuilder = new StringBuilder();
			stringBuilder.Append(text, 0, indexOfSeperator);
			stringBuilder.Append(text, indexOfSeperator + numberDecimalSeparator.Length, pow10);
			stringBuilder.Append(numberDecimalSeparator);

			var startIndex = indexOfSeperator + numberDecimalSeparator.Length + pow10;
			stringBuilder.Append(text, startIndex, text.Length - startIndex);

			return double.Parse(stringBuilder.ToString(), CultureInfo.InvariantCulture);
		}
	}
}
