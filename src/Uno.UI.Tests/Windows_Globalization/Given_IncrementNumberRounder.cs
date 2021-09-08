using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Globalization.NumberFormatting;


namespace Uno.UI.Tests.Windows_Globalization
{
    [TestClass]
    public class Given_IncrementNumberRounder
    {
        [TestMethod]
        public void Should_Throw_When_RoundingAlgorithm_Is_None()
        {
            IncrementNumberRounder rounder = new IncrementNumberRounder();
            Assert.ThrowsException<ArgumentException>(() => rounder.RoundingAlgorithm = RoundingAlgorithm.None);
        }

        [DataTestMethod]
        [DataRow(0.5, false)]
        [DataRow(0.4, true)]
        [DataRow(0.25, false)]
        [DataRow(0.27, true)]
        [DataRow(0.333333333333333, false)]
        [DataRow(0.33333333333333, true)]
        [DataRow(0.1666666666666666, false)]
        [DataRow(0.166666666666666, true)]
        [DataRow(0.00020881186051367718, false)] // inv(4789)
        [DataRow(0.0002088118605136772, true)]
        [DataRow(1 / 10000000001d, true)]
        [DataRow(0.00000000017927947438331678, false)] // inv(5577883377)
        [DataRow(0.0000000001792794743833166, true)]
        public void When_Increment_Is_Invalid_Then_Should_Throw(double increment, bool shouldThrow)
        {
            IncrementNumberRounder rounder = new IncrementNumberRounder();
            
            if (shouldThrow)
            {
                Assert.ThrowsException<ArgumentException>(() => rounder.Increment = increment);
            }
            else
            {
                rounder.Increment = increment;
            }
        }

        [DataTestMethod]
        [DataRow(1.27, 0.25, 1.25)]
        [DataRow(1.35, 1d / 3, 1 + 1d / 3)]
        [DataRow(1.125, 0.25, 1.25)]
        [DataRow(1.125, 1d / 7, 1 + 1d / 7)]
        [DataRow(1.25, 0.25, 1.25)]
        [DataRow(8, 3, 9)]
        [DataRow(0.2, 0.5, 0)]
        [DataRow(1 + 1e-22, 1e-20, 1 + 1e-22)]
        public void When_UsingVariousIncrements(double value, double increment, double expected)
        {
            IncrementNumberRounder rounder = new IncrementNumberRounder();
            rounder.Increment = increment;

            var rounded = rounder.RoundDouble(value);
            Assert.AreEqual(expected, rounded);
        }

        [DataTestMethod]
        [DataRow(1.1, 1.25)]
        [DataRow(-1.1, -1.25)]
        public void When_UsingRoundAwayFromZeroRoundingAlgorithm(double value, double expected)
        {
            When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundAwayFromZero, expected);
        }

        [DataTestMethod]
        [DataRow(1.1, 1.0)]
        [DataRow(-1.1, -1.25)]
        public void When_UsingRoundDownRoundingAlgorithm(double value, double expected)
        {
            When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundDown, expected);
        }

        [DataTestMethod]
        [DataRow(1.125, 1.25)]
        [DataRow(-1.125, -1.25)]
        [DataRow(1.12, 1.0)]
        [DataRow(-1.12, -1.0)]
        public void When_UsingRoundHalfAwayFromZeroRoundingAlgorithm(double value, double expected)
        {
            When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundHalfAwayFromZero, expected);
        }

        [DataTestMethod]
        [DataRow(1.125, 1.0)]
        [DataRow(-1.125, -1.25)]
        [DataRow(1.126, 1.25)]
        [DataRow(-1.126, -1.25)]
        public void When_UsingRoundHalfDownRoundingAlgorithm(double value, double expected)
        {
            When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundHalfDown, expected);
        }

        [DataTestMethod]
        [DataRow(1.125, 1.0)]
        [DataRow(-1.125, -1.0)]
        [DataRow(0.875, 1.0)]
        [DataRow(-0.875, -1.0)]
        public void When_UsingRoundHalfToEvenRoundingAlgorithm(double value, double expected)
        {
            When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundHalfToEven, expected);
        }

        [DataTestMethod]
        [DataRow(1.125, 1.25)]
        [DataRow(-1.125, -1.25)]
        [DataRow(0.875, 0.75)]
        [DataRow(-0.875, -0.75)]
        public void When_UsingRoundHalfToOddRoundingAlgorithm(double value, double expected)
        {
            When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundHalfToOdd, expected);
        }

        [DataTestMethod]
        [DataRow(0.875, 0.75)]
        [DataRow(-0.875, -0.75)]
        [DataRow(0.876, 1.0)]
        [DataRow(-0.876, -1.0)]
        public void When_UsingRoundHalfTowardsZeroRoundingAlgorithm(double value, double expected)
        {
            When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundHalfTowardsZero, expected);
        }

        [DataTestMethod]
        [DataRow(0.875, 1.0)]
        [DataRow(-0.875, -0.75)]
        [DataRow(0.870, 0.75)]
        [DataRow(-0.870, -0.75)]
        public void When_UsingRoundHalfUpRoundingAlgorithm(double value, double expected)
        {
            When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundHalfUp, expected);
        }

        [DataTestMethod]
        [DataRow(0.880, 0.75)]
        [DataRow(-0.880, -0.75)]
        public void When_UsingRoundTowardsZeroRoundingAlgorithm(double value, double expected)
        {
            When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundTowardsZero, expected);
        }

        [DataTestMethod]
        [DataRow(0.870, 1.0)]
        [DataRow(-0.870, -0.75)]
        public void When_UsingRoundUpnRoundingAlgorithm(double value, double expected)
        {
            When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundUp, expected);
        }

        public void When_UsingARoundingAlgorithmCore(double value, RoundingAlgorithm roundingAlgorithm, double expected)
        {
            IncrementNumberRounder rounder = new IncrementNumberRounder();
            rounder.Increment = 0.25;
            rounder.RoundingAlgorithm = roundingAlgorithm;

            var rounded = rounder.RoundDouble(value);
            Assert.AreEqual(expected, rounded);
        }
    }
}
