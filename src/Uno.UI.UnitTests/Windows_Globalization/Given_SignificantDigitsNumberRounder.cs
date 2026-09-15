#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Globalization.NumberFormatting;

namespace Uno.UI.Tests.Windows_Globalization
{
	[TestClass]
	public class Given_SignificantDigitsNumberRounder
	{
		[TestMethod]
		[DataRow(123.456, (uint)8, 123.456)]
		[DataRow(123.456, (uint)6, 123.456)]
		[DataRow(123.456, (uint)4, 123.5)]
		[DataRow(123.456, (uint)2, 120)]
		[DataRow(123.456, (uint)1, 100)]
		[DataRow(123, (uint)5, 123)]
#if RUNTIME_NATIVE_AOT
		[Ignore("DataRowAttribute.GetData() wraps data in an extra array under NativeAOT; not yet understood why.")]
#endif  // RUNTIME_NATIVE_AOT
		public void When_UsingVariousSignificantDigits(double value, uint significantDigits, double expected)
		{
			var sut = new SignificantDigitsNumberRounder();
			sut.SignificantDigits = significantDigits;

			var rounded = sut.RoundDouble(value);
			Assert.AreEqual(expected, rounded);
		}

		[TestMethod]
		[DataRow(123.456f, (uint)2, 120f)]
		[DataRow(0.012345f, (uint)2, 0.012f)]
		[DataRow(-987.65f, (uint)3, -988f)]
		public void When_RoundingSingle_Then_UsesSignificantDigits(float value, uint significantDigits, float expected)
		{
			var sut = new SignificantDigitsNumberRounder
			{
				SignificantDigits = significantDigits,
			};

			Assert.AreEqual(expected, sut.RoundSingle(value), 0.000001f);
		}

		[TestMethod]
		[DataRow(1.234567E-38f, (uint)1, RoundingAlgorithm.RoundDown, 1.0000001E-38f)]
		[DataRow(1.234567E-38f, (uint)2, RoundingAlgorithm.RoundHalfUp, 1.2000001E-38f)]
		[DataRow(999.99994f, (uint)3, RoundingAlgorithm.RoundDown, 990f)]
		[DataRow(1.9985278f, (uint)9, RoundingAlgorithm.RoundDown, 1.9985278f)]
		[DataRow(1.9985278f, (uint)9, RoundingAlgorithm.RoundUp, 1.9985278f)]
		public void When_RoundingSingle_Then_Matches_Native_Binary32_Scaling(float value, uint digits, RoundingAlgorithm algorithm, float expected)
		{
			var sut = new SignificantDigitsNumberRounder
			{
				SignificantDigits = digits,
				RoundingAlgorithm = algorithm,
			};

			Assert.AreEqual(expected, sut.RoundSingle(value));
		}

		[TestMethod]
		public void When_Rounding_Zero_Then_Sign_Is_Preserved()
		{
			var sut = new SignificantDigitsNumberRounder();
			var negativeDoubleZero = BitConverter.Int64BitsToDouble(long.MinValue);
			var negativeSingleZero = BitConverter.Int32BitsToSingle(int.MinValue);

			Assert.AreEqual(long.MinValue, BitConverter.DoubleToInt64Bits(sut.RoundDouble(negativeDoubleZero)));
			Assert.AreEqual(int.MinValue, BitConverter.SingleToInt32Bits(sut.RoundSingle(negativeSingleZero)));
		}

		[TestMethod]
		[DataRow(12345678901d, (uint)3, 12300000000d)]
		[DataRow(-12345678901d, (uint)3, -12300000000d)]
		public void When_Value_ExceedsInt32_Then_RoundsWithoutOverflow(double value, uint significantDigits, double expected)
		{
			var sut = new SignificantDigitsNumberRounder
			{
				SignificantDigits = significantDigits,
			};

			Assert.AreEqual(expected, sut.RoundDouble(value));
		}

		[TestMethod]
		public void When_Value_Is_Immediately_Below_Decade_Then_Native_Scaling_Is_Preserved()
		{
			var sut = new SignificantDigitsNumberRounder
			{
				SignificantDigits = 3,
				RoundingAlgorithm = RoundingAlgorithm.RoundDown,
			};

			Assert.AreEqual(1000d, sut.RoundDouble(999.9999999999999d));
		}

		[TestMethod]
		public void When_Value_Is_Subnormal_Then_It_Is_Rounded()
		{
			var sut = new SignificantDigitsNumberRounder
			{
				SignificantDigits = 3,
			};

			Assert.AreEqual(1.23E-310, sut.RoundDouble(1.23456789E-310), 1E-322);
		}

		[TestMethod]
		public void When_SignificantDigits_Exceeds_Double_Precision_Then_Value_Is_Unchanged()
		{
			var sut = new SignificantDigitsNumberRounder
			{
				SignificantDigits = 310,
			};

			Assert.AreEqual(123.456d, sut.RoundDouble(123.456d));
		}

		[TestMethod]
		public void When_Subnormal_Value_Is_Immediately_Below_Decade_Then_Directed_Rounding_Is_Preserved()
		{
			var sut = new SignificantDigitsNumberRounder
			{
				SignificantDigits = 3,
				RoundingAlgorithm = RoundingAlgorithm.RoundDown,
			};

			Assert.AreEqual(9.99E-314, sut.RoundDouble(Math.BitDecrement(1E-313)), 1E-323);
		}

		[TestMethod]
		[DataRow(RoundingAlgorithm.RoundDown)]
		[DataRow(RoundingAlgorithm.RoundUp)]
		[DataRow(RoundingAlgorithm.RoundHalfUp)]
		[DataRow(RoundingAlgorithm.RoundHalfToEven)]
		public void When_SignificantDigits_Equals_Double_Precision_Then_Native_Scaling_Is_Preserved(RoundingAlgorithm algorithm)
		{
			const double value = 1.9985277518021318;
			var sut = new SignificantDigitsNumberRounder
			{
				SignificantDigits = 17,
				RoundingAlgorithm = algorithm,
			};

			Assert.AreEqual(Math.BitIncrement(value), sut.RoundDouble(value));
		}

		[TestMethod]
		[DataRow(RoundingAlgorithm.RoundDown)]
		[DataRow(RoundingAlgorithm.RoundUp)]
		[DataRow(RoundingAlgorithm.RoundHalfUp)]
		[DataRow(RoundingAlgorithm.RoundHalfToEven)]
		public void When_Split_Scaling_Is_Used_Then_Native_Value_Is_Preserved(RoundingAlgorithm algorithm)
		{
			const double value = 6.6E-300;
			var sut = new SignificantDigitsNumberRounder
			{
				SignificantDigits = 16,
				RoundingAlgorithm = algorithm,
			};

			Assert.AreEqual(value, sut.RoundDouble(value));
		}

		[TestMethod]
		[DataRow(1E-310, (uint)1, RoundingAlgorithm.RoundDown, 0d)]
		[DataRow(1E-310, (uint)1, RoundingAlgorithm.RoundUp, 1E-310)]
		[DataRow(1E-310, (uint)1, RoundingAlgorithm.RoundHalfUp, 1E-310)]
		[DataRow(1E-310, (uint)1, RoundingAlgorithm.RoundHalfToEven, 1E-310)]
		[DataRow(1.2345678901234568E300, (uint)15, RoundingAlgorithm.RoundDown, 1.23456789012345E300)]
		[DataRow(1.2345678901234568E300, (uint)15, RoundingAlgorithm.RoundHalfUp, 1.23456789012346E300)]
		[DataRow(1.2345678901234568E300, (uint)16, RoundingAlgorithm.RoundDown, 1.2345678901234568E300)]
		[DataRow(1.2345678901234568E300, (uint)16, RoundingAlgorithm.RoundUp, 1.2345678901234568E300)]
		[DataRow(1.2345678901234568E300, (uint)17, RoundingAlgorithm.RoundDown, 1.2345678901234567E300)]
		[DataRow(1.2345678901234568E300, (uint)17, RoundingAlgorithm.RoundUp, 1.2345678901234567E300)]
		[DataRow(1.2345678901234568E300, (uint)17, RoundingAlgorithm.RoundHalfUp, 1.2345678901234567E300)]
		[DataRow(1.2345678901234568E300, (uint)17, RoundingAlgorithm.RoundHalfToEven, 1.2345678901234567E300)]
		public void When_Extreme_Magnitude_Then_Matches_Native_WinRT(double value, uint digits, RoundingAlgorithm algorithm, double expected)
		{
			var sut = new SignificantDigitsNumberRounder
			{
				SignificantDigits = digits,
				RoundingAlgorithm = algorithm,
			};

			Assert.AreEqual(expected, sut.RoundDouble(value));
		}

		[TestMethod]
		[DataRow(RoundingAlgorithm.RoundDown)]
		[DataRow(RoundingAlgorithm.RoundUp)]
		[DataRow(RoundingAlgorithm.RoundHalfUp)]
		[DataRow(RoundingAlgorithm.RoundHalfToEven)]
		public void When_Smallest_Subnormals_Then_Matches_Native_Quantization(RoundingAlgorithm algorithm)
		{
			var sut = new SignificantDigitsNumberRounder { RoundingAlgorithm = algorithm };
			foreach (var value in new[] { double.Epsilon, 2 * double.Epsilon, -double.Epsilon, -2 * double.Epsilon })
			{
				Assert.AreEqual(value, sut.RoundDouble(value));
			}
		}

		[TestMethod]
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

		[TestMethod]
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

		[TestMethod]
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

		[TestMethod]
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

		[TestMethod]
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

		[TestMethod]
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

		[TestMethod]
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

		[TestMethod]
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

		[TestMethod]
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

		[TestMethod]
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

		[TestMethod]
		[DataRow(double.NaN, double.NaN)]
		[DataRow(double.NegativeInfinity, double.NaN)]
		[DataRow(double.PositiveInfinity, double.NaN)]
		public void When_Value_Is_Special(double value, double expected)
		{
			When_UsingARoundingAlgorithmCore(value, RoundingAlgorithm.RoundHalfUp, expected);
		}

		private void When_UsingARoundingAlgorithmCore(double value, RoundingAlgorithm roundingAlgorithm, double expected)
		{
			var sut = new SignificantDigitsNumberRounder();
			sut.SignificantDigits = 2;
			sut.RoundingAlgorithm = roundingAlgorithm;

			var rounded = sut.RoundDouble(value);
			Assert.AreEqual(expected, rounded);
		}


		[TestMethod]
		public void When_RoundingAlgorithm_Is_None_Then_Should_Throw()
		{
			var sut = new SignificantDigitsNumberRounder();

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
		public void When_SignificantDigits_Is_Zero_Then_Should_Throw()
		{
			var sut = new SignificantDigitsNumberRounder();

			try
			{
				sut.SignificantDigits = 0;
			}
			catch (Exception ex)
			{
				Assert.AreEqual("The parameter is incorrect.\r\n\r\nvalue", ex.Message);
			}

			Assert.ThrowsExactly<ArgumentException>(() => sut.SignificantDigits = 0);
		}
	}
}
