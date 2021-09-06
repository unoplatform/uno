using System;
using Uno;

namespace Windows.Globalization.NumberFormatting
{
	public  partial class SignificantDigitsNumberRounder : INumberRounder
	{
		private uint significantDigits = 1;
		private RoundingAlgorithm roundingAlgorithm = RoundingAlgorithm.RoundHalfUp;

        public uint SignificantDigits
        {
            get => significantDigits; 
            set
            {
                if (value == 0)
                    throw new ArgumentException("The parameter is incorrect");

                significantDigits = value;
            }
        }

		public RoundingAlgorithm RoundingAlgorithm
        {
            get => roundingAlgorithm; 
            set
            {
                if (value == RoundingAlgorithm.None)
                    throw new ArgumentException("The parameter is incorrect");

                roundingAlgorithm = value;
            }
        }

		public SignificantDigitsNumberRounder() 
		{
		}

		[NotImplemented]
		public int RoundInt32( int value)
		{
			throw new global::System.NotImplementedException("The member int SignificantDigitsNumberRounder.RoundInt32(int value) is not implemented in Uno.");
		}

		[NotImplemented]
		public uint RoundUInt32( uint value)
		{
			throw new global::System.NotImplementedException("The member uint SignificantDigitsNumberRounder.RoundUInt32(uint value) is not implemented in Uno.");
		}

		[NotImplemented]
		public long RoundInt64( long value)
		{
			throw new global::System.NotImplementedException("The member long SignificantDigitsNumberRounder.RoundInt64(long value) is not implemented in Uno.");
		}

		[NotImplemented]
		public  ulong RoundUInt64( ulong value)
		{
			throw new global::System.NotImplementedException("The member ulong SignificantDigitsNumberRounder.RoundUInt64(ulong value) is not implemented in Uno.");
		}

		public  float RoundSingle( float value)
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
            var integerPartLength = (uint)GetIntegerLength(integerPart);
            var diffLength = SignificantDigits - integerPartLength;

            if (SignificantDigits < integerPartLength)
            {
                diffLength = integerPartLength - SignificantDigits;
                var pow10 = Math.Pow(10, diffLength);
                value /= pow10;
                value = Round(value, 0, RoundingAlgorithm);
                value *= pow10;
                return value;
            }

            return Round(value, (int)diffLength, RoundingAlgorithm);
        }

        private static int GetIntegerLength(int integerPart)
        {
            if (integerPart == 0)
                return 1;

            return (int)Math.Floor(Math.Log10(Math.Abs(integerPart))) + 1;
        }

        private static double Round(double value, int digits, RoundingAlgorithm roundingAlgorithm)
        {
            var pow10 = Math.Pow(10, digits);
            value *= pow10;

            switch (roundingAlgorithm)
            {
                case RoundingAlgorithm.RoundDown:
                    value = Math.Floor(value);
                    break;
                case RoundingAlgorithm.RoundUp:
                    value = Math.Ceiling(value);
                    break;
                case RoundingAlgorithm.RoundTowardsZero:
                    {
                        if (value > 0)
                        {
                            value = Round(value, 0, RoundingAlgorithm.RoundDown);
                        }
                        else
                        {
                            value = Round(value, 0, RoundingAlgorithm.RoundUp);
                        }
                    }
                    break;
                case RoundingAlgorithm.RoundAwayFromZero:
                    {
                        if (value > 0)
                        {
                            value = Round(value, 0, RoundingAlgorithm.RoundUp);
                        }
                        else
                        {
                            value = Round(value, 0, RoundingAlgorithm.RoundDown);
                        }
                    }
                    break;
                case RoundingAlgorithm.RoundHalfDown:
                    {
                        var fracPart = GetRoundedAbsoluteFraction(value);

                        if (fracPart <= 0.5 && value > 0 ||
                            fracPart >= 0.5 && value < 0)
                        {
                            value = Math.Floor(value);
                        }
                        else
                        {
                            value = Math.Ceiling(value);
                        }
                    }
                    break;
                case RoundingAlgorithm.RoundHalfUp:
                    {
                        var fracPart = GetRoundedAbsoluteFraction(value);

                        if (fracPart >= 0.5 && value > 0 ||
                            fracPart <= 0.5 && value < 0)
                        {
                            value = Math.Ceiling(value);
                        }
                        else
                        {
                            value = Math.Floor(value);
                        }
                    }
                    break;
                case RoundingAlgorithm.RoundHalfTowardsZero:
                    {
                        var fracPart = GetRoundedAbsoluteFraction(value);

                        if (fracPart <= 0.5 && value > 0 ||
                            fracPart > 0.5 && value < 0)
                        {
                            value = Math.Floor(value);
                        }
                        else
                        {
                            value = Math.Ceiling(value);
                        }
                    }
                    break;
                case RoundingAlgorithm.RoundHalfAwayFromZero:
                    {
                        var fracPart = GetRoundedAbsoluteFraction(value);

                        if (fracPart >= 0.5 && value > 0 ||
                            fracPart < 0.5 && value < 0)
                        {
                            value = Math.Ceiling(value);
                        }
                        else
                        {
                            value = Math.Floor(value);
                        }
                    }
                    break;
                case RoundingAlgorithm.RoundHalfToEven:
                    value = Math.Round(value, 0, MidpointRounding.ToEven);
                    break;
                case RoundingAlgorithm.RoundHalfToOdd:
                    {
                        var intPart = Math.Truncate(value);
                        var isHalf = IsFractionExactlyHalf(value);

                        if (isHalf)
                        {
                            if (intPart % 2 == 1 && value > 0 ||
                                intPart % 2 == 0 && value < 0)
                            {
                                value = Math.Floor(value);
                            }
                            else
                            {
                                value = Math.Ceiling(value);
                            }
                        }
                        else
                        {
                            value = Math.Round(value, 0, MidpointRounding.AwayFromZero);
                        }
                    }
                    break;
                default:
                    value = Math.Round(value, 0, MidpointRounding.AwayFromZero);
                    break;
            }

            value /= pow10;
            return value;
        }

        private static double GetRoundedAbsoluteFraction(double value)
        {
            return Math.Round(Math.Abs(value % 1), 1, MidpointRounding.AwayFromZero);
        }

        private static bool IsFractionExactlyHalf(double value)
        {
            var frac = Math.Abs(value % 1);
            frac = Math.Round(frac, 2, MidpointRounding.AwayFromZero);

            return frac == 0.5;
        }
	}
}
