#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Globalization.NumberFormatting;


namespace Uno.UI.Tests.Windows_Globalization
{
	[TestClass]
	public class Given_IncrementNumberRounder
	{
		[TestMethod]
		public void Should_Throw_When_RoundingAlgorithm_Is_None()
		{
			var sut = new IncrementNumberRounder();

			try
			{
				sut.RoundingAlgorithm = RoundingAlgorithm.None;
			}
			catch (Exception ex)
			{
				Assert.AreEqual("The parameter is incorrect.\r\n\r\nvalue", ex.Message);
			}

			Assert.ThrowsExactly<ArgumentException>(() => sut.RoundingAlgorithm = RoundingAlgorithm.None);
		}

		[TestMethod]
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
			var sut = new IncrementNumberRounder();

			if (shouldThrow)
			{
				try
				{
					sut.Increment = increment;
				}
				catch (Exception ex)
				{
					Assert.AreEqual("The parameter is incorrect.\r\n\r\nvalue", ex.Message);
				}

				Assert.ThrowsExactly<ArgumentException>(() => sut.Increment = increment);
			}
			else
			{
				sut.Increment = increment;
			}
		}

		[TestMethod]
		[DataRow(1.27, 0.25, 1.25)]
		[DataRow(1.35, 1d / 3, 1 + 1d / 3)]
		[DataRow(1.125, 0.25, 1.25)]
		[DataRow(1.125, 1d / 7, 1 + 1d / 7)]
		[DataRow(1.25, 0.25, 1.25)]
		[DataRow(8, 3, 9)]
		[DataRow(0.2, 0.5, 0)]
		[DataRow(1 + 1e-22, 1e-20, 1 + 1e-22)]
#if RUNTIME_NATIVE_AOT
		[Ignore("DataRowAttribute.GetData() wraps data in an extra array under NativeAOT; not yet understood why.")]
#endif  // RUNTIME_NATIVE_AOT
		public void When_UsingVariousIncrements(double value, double increment, double expected)
		{
			var sut = new IncrementNumberRounder();
			sut.Increment = increment;

			var rounded = sut.RoundDouble(value);
			Assert.AreEqual(expected, rounded);
		}

		[TestMethod]
		[DataRow(1.1, 1.25)]
		[DataRow(-1.1, -1.25)]
		public void When_UsingRoundAwayFromZeroRoundingAlgorithm(double value, double expected)
		{
			When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundAwayFromZero, expected);
		}

		[TestMethod]
		[DataRow(1.1, 1.0)]
		[DataRow(-1.1, -1.25)]
		public void When_UsingRoundDownRoundingAlgorithm(double value, double expected)
		{
			When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundDown, expected);
		}

		[TestMethod]
		[DataRow(1.125, 1.25)]
		[DataRow(-1.125, -1.25)]
		[DataRow(1.12, 1.0)]
		[DataRow(-1.12, -1.0)]
		public void When_UsingRoundHalfAwayFromZeroRoundingAlgorithm(double value, double expected)
		{
			When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundHalfAwayFromZero, expected);
		}

		[TestMethod]
		[DataRow(1.125, 1.0)]
		[DataRow(-1.125, -1.25)]
		[DataRow(1.126, 1.25)]
		[DataRow(-1.126, -1.25)]
		public void When_UsingRoundHalfDownRoundingAlgorithm(double value, double expected)
		{
			When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundHalfDown, expected);
		}

		[TestMethod]
		[DataRow(1.125, 1.0)]
		[DataRow(-1.125, -1.0)]
		[DataRow(0.875, 1.0)]
		[DataRow(-0.875, -1.0)]
		public void When_UsingRoundHalfToEvenRoundingAlgorithm(double value, double expected)
		{
			When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundHalfToEven, expected);
		}

		[TestMethod]
		[DataRow(1.125, 1.25)]
		[DataRow(-1.125, -1.25)]
		[DataRow(0.875, 0.75)]
		[DataRow(-0.875, -0.75)]
		public void When_UsingRoundHalfToOddRoundingAlgorithm(double value, double expected)
		{
			When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundHalfToOdd, expected);
		}

		[TestMethod]
		[DataRow(0.875, 0.75)]
		[DataRow(-0.875, -0.75)]
		[DataRow(0.876, 1.0)]
		[DataRow(-0.876, -1.0)]
		public void When_UsingRoundHalfTowardsZeroRoundingAlgorithm(double value, double expected)
		{
			When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundHalfTowardsZero, expected);
		}

		[TestMethod]
		[DataRow(0.875, 1.0)]
		[DataRow(-0.875, -0.75)]
		[DataRow(0.870, 0.75)]
		[DataRow(-0.870, -0.75)]
		public void When_UsingRoundHalfUpRoundingAlgorithm(double value, double expected)
		{
			When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundHalfUp, expected);
		}

		[TestMethod]
		[DataRow(0.880, 0.75)]
		[DataRow(-0.880, -0.75)]
		public void When_UsingRoundTowardsZeroRoundingAlgorithm(double value, double expected)
		{
			When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundTowardsZero, expected);
		}

		[TestMethod]
		[DataRow(0.870, 1.0)]
		[DataRow(-0.870, -0.75)]
		public void When_UsingRoundUpnRoundingAlgorithm(double value, double expected)
		{
			When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundUp, expected);
		}

		private void When_UsingARoundingAlgorithmCore(double value, RoundingAlgorithm roundingAlgorithm, double expected)
		{
			var sut = new IncrementNumberRounder();
			sut.Increment = 0.25;
			sut.RoundingAlgorithm = roundingAlgorithm;

			var rounded = sut.RoundDouble(value);
			Assert.AreEqual(expected, rounded);
		}
	}
}
