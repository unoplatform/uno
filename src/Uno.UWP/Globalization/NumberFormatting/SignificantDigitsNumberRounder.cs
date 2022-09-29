#nullable disable

using System;
using Uno;
using Uno.Globalization.NumberFormatting;

namespace Windows.Globalization.NumberFormatting
{
	public partial class SignificantDigitsNumberRounder : INumberRounder
	{
		private uint significantDigits = 1;
		private RoundingAlgorithm roundingAlgorithm = RoundingAlgorithm.RoundHalfUp;

		public uint SignificantDigits
		{
			get => significantDigits;
			set
			{
				if (value == 0)
				{
					throw new ArgumentException("The parameter is incorrect");
				}

				significantDigits = value;
			}
		}

		public RoundingAlgorithm RoundingAlgorithm
		{
			get => roundingAlgorithm;
			set
			{
				if (value == RoundingAlgorithm.None)
				{
					throw new ArgumentException("The parameter is incorrect");
				}

				roundingAlgorithm = value;
			}
		}

		public SignificantDigitsNumberRounder()
		{
		}

		[NotImplemented]
		public int RoundInt32(int value)
		{
			throw new global::System.NotImplementedException("The member int SignificantDigitsNumberRounder.RoundInt32(int value) is not implemented in Uno.");
		}

		[NotImplemented]
		public uint RoundUInt32(uint value)
		{
			throw new global::System.NotImplementedException("The member uint SignificantDigitsNumberRounder.RoundUInt32(uint value) is not implemented in Uno.");
		}

		[NotImplemented]
		public long RoundInt64(long value)
		{
			throw new global::System.NotImplementedException("The member long SignificantDigitsNumberRounder.RoundInt64(long value) is not implemented in Uno.");
		}

		[NotImplemented]
		public ulong RoundUInt64(ulong value)
		{
			throw new global::System.NotImplementedException("The member ulong SignificantDigitsNumberRounder.RoundUInt64(ulong value) is not implemented in Uno.");
		}

		public float RoundSingle(float value)
		{
			return (float)Math.Round(value, (int)SignificantDigits, MidpointRounding.AwayFromZero);
		}

		public double RoundDouble(double value)
		{
			if (double.IsNaN(value) ||
				double.IsInfinity(value))
			{
				return double.NaN;
			}

			var integerPart = (int)Math.Truncate(value);
			var integerPartLength = (uint)integerPart.GetLength();
			var diffLength = SignificantDigits - integerPartLength;

			if (SignificantDigits < integerPartLength)
			{
				diffLength = integerPartLength - SignificantDigits;
				var pow10 = Math.Pow(10, diffLength);
				value /= pow10;
				value = Rounder.Round(value, 0, RoundingAlgorithm);
				value *= pow10;
				return value;
			}

			return Rounder.Round(value, (int)diffLength, RoundingAlgorithm);
		}
	}
}
