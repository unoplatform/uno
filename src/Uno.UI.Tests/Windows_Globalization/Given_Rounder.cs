#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Globalization.NumberFormatting;
using Windows.Globalization.NumberFormatting;

namespace Uno.UI.Tests.Windows_Globalization
{
	[TestClass]
	public class Given_Rounder
	{

		[DataTestMethod]
		[DataRow(4.48, 0, 4d)]
		public void When_UsingRoundMethod(double value, int digits, double expected)
		{
			var rounded = Rounder.Round(value, digits, RoundingAlgorithm.RoundHalfAwayFromZero);
			Assert.AreEqual(expected, rounded);
		}

		[DataTestMethod]
		[DataRow(1.5, true)]
		[DataRow(0.5, true)]
		[DataRow(-0.5, true)]
		[DataRow(-12.5, true)]
		[DataRow(-4.5, true)]
		[DataRow(-4.50001, false)]
		public void When_Fraction_Is_Half_Then_Return_True(double value, bool expected)
		{
			Assert.AreEqual(expected, Rounder.IsFractionExactlyHalf(value));
		}

		[DataTestMethod]
		[DataRow(1.25, 1.3)]
		[DataRow(1.27, 1.3)]
		[DataRow(1.23, 1.3)]
		[DataRow(-1.25, -1.3)]
		[DataRow(-1.27, -1.3)]
		[DataRow(-1.23, -1.3)]
		public void When_RoundingAlgorithm_Is_RoundAwayFromZero(double value, double expected)
		{
			When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundAwayFromZero, expected);
		}

		[DataTestMethod]
		[DataRow(1.25, 1.3)]
		[DataRow(1.27, 1.3)]
		[DataRow(1.23, 1.2)]
		[DataRow(-1.25, -1.3)]
		[DataRow(-1.27, -1.3)]
		[DataRow(-1.23, -1.2)]
		public void When_RoundingAlgorithm_Is_RoundHalfAwayFromZero(double value, double expected)
		{
			When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundHalfAwayFromZero, expected);
		}

		[DataTestMethod]
		[DataRow(1.25, 1.3)]
		[DataRow(1.27, 1.3)]
		[DataRow(1.23, 1.3)]
		[DataRow(-1.25, -1.2)]
		[DataRow(-1.27, -1.2)]
		[DataRow(-1.23, -1.2)]
		public void When_RoundingAlgorithm_Is_RoundUp(double value, double expected)
		{
			When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundUp, expected);
		}

		[DataTestMethod]
		[DataRow(1.25, 1.3)]
		[DataRow(1.27, 1.3)]
		[DataRow(1.23, 1.2)]
		[DataRow(-1.25, -1.2)]
		[DataRow(-1.27, -1.3)]
		[DataRow(-1.23, -1.2)]
		public void When_RoundingAlgorithm_Is_RoundHalfUp(double value, double expected)
		{
			When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundHalfUp, expected);
		}

		[DataTestMethod]
		[DataRow(1.25, 1.2)]
		[DataRow(1.27, 1.2)]
		[DataRow(1.23, 1.2)]
		[DataRow(-1.25, -1.3)]
		[DataRow(-1.27, -1.3)]
		[DataRow(-1.23, -1.3)]
		public void When_RoundingAlgorithm_Is_RoundDown(double value, double expected)
		{
			When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundDown, expected);
		}

		[DataTestMethod]
		[DataRow(1.25, 1.2)]
		[DataRow(1.27, 1.3)]
		[DataRow(1.23, 1.2)]
		[DataRow(-1.25, -1.3)]
		[DataRow(-1.27, -1.3)]
		[DataRow(-1.23, -1.2)]
		public void When_RoundingAlgorithm_Is_RoundHalfDown(double value, double expected)
		{
			When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundHalfDown, expected);
		}

		[DataTestMethod]
		[DataRow(1.25, 1.2)]
		[DataRow(1.27, 1.2)]
		[DataRow(1.23, 1.2)]
		[DataRow(-1.25, -1.2)]
		[DataRow(-1.27, -1.2)]
		[DataRow(-1.23, -1.2)]
		public void When_RoundingAlgorithm_Is_RoundTowardsZero(double value, double expected)
		{
			When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundTowardsZero, expected);
		}

		[DataTestMethod]
		[DataRow(1.25, 1.2)]
		[DataRow(1.27, 1.3)]
		[DataRow(1.23, 1.2)]
		[DataRow(-1.25, -1.2)]
		[DataRow(-1.27, -1.3)]
		[DataRow(-1.23, -1.2)]
		public void When_RoundingAlgorithm_Is_RoundHalfTowardsZero(double value, double expected)
		{
			When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundHalfTowardsZero, expected);
		}

		[DataTestMethod]
		[DataRow(1.25, 1.2)]
		[DataRow(1.27, 1.3)]
		[DataRow(1.23, 1.2)]
		[DataRow(-1.25, -1.2)]
		[DataRow(-1.27, -1.3)]
		[DataRow(-1.23, -1.2)]
		[DataRow(1.35, 1.4)]
		[DataRow(1.37, 1.4)]
		[DataRow(1.33, 1.3)]
		[DataRow(-1.35, -1.4)]
		[DataRow(-1.37, -1.4)]
		[DataRow(-1.33, -1.3)]
		public void When_RoundingAlgorithm_Is_RoundHalfToEven(double value, double expected)
		{
			When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundHalfToEven, expected);
		}

		[DataTestMethod]
		[DataRow(1.25, 1.3)]
		[DataRow(1.27, 1.3)]
		[DataRow(1.23, 1.2)]
		[DataRow(-1.25, -1.3)]
		[DataRow(-1.27, -1.3)]
		[DataRow(-1.23, -1.2)]
		[DataRow(1.35, 1.3)]
		[DataRow(1.37, 1.4)]
		[DataRow(1.33, 1.3)]
		[DataRow(-1.35, -1.3)]
		[DataRow(-1.37, -1.4)]
		[DataRow(-1.33, -1.3)]
		public void When_RoundingAlgorithm_Is_RoundHalfToOdd(double value, double expected)
		{
			When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundHalfToOdd, expected);
		}

		private void When_UsingARoundingAlgorithmCore(double value, RoundingAlgorithm roundingAlgorithm, double expected)
		{
			var rounded = Rounder.Round(value, 1, roundingAlgorithm);
			Assert.AreEqual(expected, rounded);
		}
	}
}
