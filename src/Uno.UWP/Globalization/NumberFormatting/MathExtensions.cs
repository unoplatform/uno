#nullable disable

using System;
using System.Globalization;
using System.Linq;

namespace Windows.Globalization.NumberFormatting
{
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
				var part = new string(Enumerable.Repeat('0', pow10).ToArray());
				return double.Parse($"{text}{part}", CultureInfo.InvariantCulture);
			}

			var fractionLength = text.Length - indexOfSeperator - numberDecimalSeparator.Length;
			var diff = fractionLength - pow10;

			if (diff <= 0)
			{
				var part = new string(Enumerable.Repeat('0', -diff).ToArray());
				return double.Parse($"{text.Remove(indexOfSeperator, numberDecimalSeparator.Length)}{part}", CultureInfo.InvariantCulture);
			}

			var integerPart = text.Substring(0, indexOfSeperator);
			var integerNewPart = text.Substring(indexOfSeperator + numberDecimalSeparator.Length, pow10);
			var newFractionPart = text.Substring(indexOfSeperator + numberDecimalSeparator.Length + pow10);
			return double.Parse($"{integerPart}{integerNewPart}{numberDecimalSeparator}{newFractionPart}", CultureInfo.InvariantCulture);
		}
	}
}
