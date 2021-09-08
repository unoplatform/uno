using System;
using System.Linq;

namespace Windows.Globalization.NumberFormatting
{
    public partial class IncrementNumberRounder : global::Windows.Globalization.NumberFormatting.INumberRounder
    {
        private static readonly double[] Exceptions = new double[]
        {
            1E-11,
            1E-12,
            1E-13,
            1E-14,
            1E-15,
            1E-16,
            1E-17,
            1E-18,
            1E-19,
            1E-20,
        };

        private RoundingAlgorithm roundingAlgorithm = RoundingAlgorithm.RoundHalfUp;
        private double increment = 1d;

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

        public double Increment
        {
            get => increment;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("The parameter is incorrect");
                }
                else if (value <= 0.5)
                {
                    if (!Exceptions.Any(e => e == value))
                    {
                        var inv = (1 / value);
                        var n = Math.Truncate(inv);
                        if (n < 2 || n > 10000000000)
                        {
                            throw new ArgumentException("The parameter is incorrect");
                        }

                        var modf = Math.Round(inv % 1, 14, MidpointRounding.AwayFromZero);
                        if (modf > 0)
                        {
                            throw new ArgumentException("The parameter is incorrect");
                        }
                    }
                }
                else if (value < 1)
                {
                    throw new ArgumentException("The parameter is incorrect");
                }
                else if (Math.Truncate(value) != value)
                {
                    throw new ArgumentException("The parameter is incorrect");
                }


                increment = value;
            }
        }

        public IncrementNumberRounder()
        {
        }

        public double RoundDouble(double value)
        {
            var rounded = value / increment;
            rounded = Rounder.Round(rounded, 0, RoundingAlgorithm);
            rounded *= increment;

            return rounded;
        }
    }
}
