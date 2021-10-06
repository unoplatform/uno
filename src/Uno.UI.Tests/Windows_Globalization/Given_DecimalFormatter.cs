
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Globalization.NumberFormatting;

namespace Uno.UI.Tests.Windows_Globalization
{
	[TestClass]
	public class Given_DecimalFormatter
	{
		[DataTestMethod]
		[DataRow(double.PositiveInfinity, "∞")]
		[DataRow(double.NegativeInfinity, "-∞")]
		[DataRow(double.NaN, "NaN")]
		public void When_FormatSpecialDouble(double value, string expected)
		{
			DecimalFormatter df = new DecimalFormatter();
			var actual = df.FormatDouble(value);

			Assert.AreEqual(expected, actual);
		}

		[DataTestMethod]
		[DataRow(1.5d, 1, 2, "1.50")]
		[DataRow(1.567d, 1, 2, "1.567")]
		[DataRow(1.5602d, 1, 2, "1.5602")]
		[DataRow(0d, 0, 0, "0")]
		[DataRow(-0d, 0, 0, "0")]
		[DataRow(0d, 0, 2, ".00")]
		[DataRow(-0d, 0, 2, ".00")]
		[DataRow(0d, 2, 0, "00")]
		[DataRow(-0d, 2, 0, "00")]
		[DataRow(0d, 3, 1, "000.0")]
		[DataRow(-0d, 3, 1, "000.0")]
		public void When_FormatDouble(double value, int integerDigits, int fractionDigits, string expected)
		{
			DecimalFormatter df = new DecimalFormatter();
			df.IntegerDigits = integerDigits;
			df.FractionDigits = fractionDigits;

			var formatted = df.FormatDouble(value);
			Assert.AreEqual(expected, formatted);
		}

		[DataTestMethod]
		[DataRow(1234, 2, 0, "1,234")]
		[DataRow(1234, 6, 0, "001,234")]
		[DataRow(1234.56, 2, 2, "1,234.56")]
		[DataRow(1234.0, 6, 2, "001,234.00")]
		[DataRow(1234.0, 6, 0, "001,234")]
		public void When_FormatDoubleWithIsGroupSetTrue(double value, int integerDigits, int fractionDigits, string expected)
		{
			DecimalFormatter df = new DecimalFormatter();
			df.IntegerDigits = integerDigits;
			df.FractionDigits = fractionDigits;
			df.IsGrouped = true;

			var formatted = df.FormatDouble(value);
			Assert.AreEqual(expected, formatted);
		}

		[DataTestMethod]
		[DataRow(0, 0, "-0")]
		[DataRow(0, 2, "-.00")]
		[DataRow(2, 0, "-00")]
		[DataRow(3, 1, "-000.0")]
		public void When_FormatDoubleMinusZeroWithIsZeroSignedSetTrue(int integerDigits, int fractionDigits, string expected)
		{
			DecimalFormatter df = new DecimalFormatter();
			df.IntegerDigits = integerDigits;
			df.FractionDigits = fractionDigits;
			df.IsZeroSigned = true;

			var formatted = df.FormatDouble(-0d);
			Assert.AreEqual(expected, formatted);
		}

		[DataTestMethod]
		[DataRow(0, 0, "0")]
		[DataRow(0, 2, ".00")]
		[DataRow(2, 0, "00")]
		[DataRow(3, 1, "000.0")]
		public void When_FormatDoubleZeroWithIsZeroSignedSetTrue(int integerDigits, int fractionDigits, string expected)
		{
			DecimalFormatter df = new DecimalFormatter();
			df.IntegerDigits = integerDigits;
			df.FractionDigits = fractionDigits;
			df.IsZeroSigned = true;

			var formatted = df.FormatDouble(0d);
			Assert.AreEqual(expected, formatted);
		}

		[DataTestMethod]
		[DataRow(1d, "1.")]
		public void When_FormatDoubleWithIsDecimalPointerAlwaysDisplayedSetTrue(double value, string expected)
		{
			DecimalFormatter df = new DecimalFormatter();
			df.IsDecimalPointAlwaysDisplayed = true;
			df.FractionDigits = 0;
			df.IntegerDigits = 0;

			var formatted = df.FormatDouble(value);
			Assert.AreEqual(expected, formatted);
		}

		[DataTestMethod]
		[DataRow(123.4567d, 5, 1, 2, "123.4567")]
		[DataRow(123.4567d, 10, 1, 2, "123.4567000")]
		[DataRow(123.4567d, 2, 1, 2, "123.4567")]
		[DataRow(12.3d, 4, 1, 2, "12.30")]
		[DataRow(12.3d, 4, 1, 0, "12.30")]
		public void When_FormatDoubleWithSpecificSignificantDigits(double value, int significantDigits, int integerDigits, int fractionDigits, string expected)
		{
			DecimalFormatter df = new DecimalFormatter();
			df.SignificantDigits = significantDigits;
			df.IntegerDigits = integerDigits;
			df.FractionDigits = fractionDigits;

			var formatted = df.FormatDouble(value);
			Assert.AreEqual(expected, formatted);
		}

		[TestMethod]
		public void When_FormatDoubleUsingIncrementNumberRounder()
		{
			DecimalFormatter df = new DecimalFormatter();
			IncrementNumberRounder rounder = new IncrementNumberRounder();
			rounder.Increment = 0.5;
			df.NumberRounder = rounder;
			var formatted = df.FormatDouble(1.8);

			Assert.AreEqual("2.00", formatted);
		}

		[TestMethod]
		public void When_FormatDoubleUsingSignificantDigitsNumberRounder()
		{
			DecimalFormatter df = new DecimalFormatter();
			SignificantDigitsNumberRounder rounder = new SignificantDigitsNumberRounder();
			rounder.SignificantDigits = 1;
			df.NumberRounder = rounder;
			var formatted = df.FormatDouble(1.8);

			Assert.AreEqual("2.00", formatted);
		}

		[TestMethod]
		public void When_Initialize()
		{
			DecimalFormatter df = new DecimalFormatter();

			Assert.AreEqual(0, df.SignificantDigits);
			Assert.AreEqual(1, df.IntegerDigits);
			Assert.AreEqual(2, df.FractionDigits);
			Assert.AreEqual(false, df.IsGrouped);
			Assert.AreEqual(false, df.IsZeroSigned);
			Assert.AreEqual(false, df.IsDecimalPointAlwaysDisplayed);
			Assert.AreEqual("en-US", df.ResolvedLanguage);
			Assert.IsNull(df.NumberRounder);
			/*
				FractionDigits	2	int
				GeographicRegion	"US"	string
				IntegerDigits	1	int
				IsDecimalPointAlwaysDisplayed	false	bool
				IsGrouped	false	bool
				IsZeroSigned	false	bool
				NumberRounder	null	WindoGlobalization.NumberFormatting.INumberRounder
				NumeralSystem	"Latn"	string
				ResolvedGeographicRegion	"ZZ"	string
				ResolvedLanguage	"en-US"	string
				SignificantDigits	0	int

			 */
		}

		[DataTestMethod]
		[DataRow("1.2", 1.2)]
		[DataRow("1.20", 1.2)]
		[DataRow("12,34.2", null)]
		[DataRow("0", 0d)]
		public void When_ParseDouble(string value, double? expected)
		{
			DecimalFormatter df = new DecimalFormatter();
			df.FractionDigits = 2;

			var actual = df.ParseDouble(value);
			Assert.AreEqual(expected, actual);
		}

		[DataTestMethod]
		[DataRow("1234.2", 1234.2)]
		[DataRow("1,234.2", 1234.2)]
		[DataRow("12,34.2", null)]
		public void When_ParseDoubleAndIsGroupSetTrue(string value, double? expected)
		{
			DecimalFormatter df = new DecimalFormatter();
			df.FractionDigits = 2;
			df.IsGrouped = true;

			var actual = df.ParseDouble(value);
			Assert.AreEqual(expected, actual);
		}

		[DataTestMethod]
		[DataRow("1", 1d)]
		[DataRow("1.", 1d)]
		public void When_ParseDoubleAndIsDecimalPointAlwaysDisplayedSetTrue(string value, double? expected)
		{
			DecimalFormatter df = new DecimalFormatter();
			df.FractionDigits = 2;
			df.IsDecimalPointAlwaysDisplayed = true;

			var actual = df.ParseDouble(value);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void When_ParseDoubleMinusZero()
		{
			DecimalFormatter df = new DecimalFormatter();
			var actual = df.ParseDouble("-0");
			bool isNegative = false;

			if (actual.HasValue)
			{
				isNegative = BitConverter.DoubleToInt64Bits(actual.Value) < 0;
			}

			Assert.AreEqual(true, isNegative);
		}

		[DataTestMethod]
		[DataRow("Arab")]
		[DataRow("ArabExt")]
		[DataRow("Bali")]
		[DataRow("Beng")]
		[DataRow("Cham")]
		[DataRow("Deva")]
		[DataRow("FullWide")]
		[DataRow("Gujr")]
		[DataRow("Guru")]
		[DataRow("Java")]
		[DataRow("Kali")]
		[DataRow("Khmr")]
		[DataRow("Knda")]
		[DataRow("Lana")]
		[DataRow("LanaTham")]
		[DataRow("Laoo")]
		[DataRow("Latn")]
		[DataRow("Lepc")]
		[DataRow("Limb")]
		[DataRow("Mlym")]
		[DataRow("Mong")]
		[DataRow("Mtei")]
		[DataRow("Mymr")]
		[DataRow("MymrShan")]
		[DataRow("Nkoo")]
		[DataRow("Olck")]
		[DataRow("Orya")]
		[DataRow("Saur")]
		[DataRow("Sund")]
		[DataRow("Talu")]
		[DataRow("TamlDec")]
		[DataRow("Telu")]
		[DataRow("Thai")]
		[DataRow("Tibt")]
		[DataRow("Vaii")]
		public void When_ParseDoubleUsingSpeceficNumeralSystem(string numeralSystem)
		{
			DecimalFormatter df = new DecimalFormatter();
			df.NumeralSystem = numeralSystem;

			var translator = new NumeralSystemTranslator { NumeralSystem = numeralSystem };
			var translated = translator.TranslateNumerals("1234.56789");

			var actual = df.ParseDouble(translated);
			Assert.AreEqual(1234.56789, actual);
		}

		[TestMethod]
		public void When_ParseNotValidDouble()
		{
			DecimalFormatter df = new DecimalFormatter();
			
			var actual =  df.ParseDouble("a12");
			Assert.AreEqual(null, actual);
		}
	}
}
