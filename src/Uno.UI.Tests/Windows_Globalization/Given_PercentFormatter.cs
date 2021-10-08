using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Windows.Globalization.NumberFormatting;

namespace Uno.UI.Tests.Windows_Globalization
{
	[TestClass]
	public class Given_PercentFormatter
	{
		[DataTestMethod]
		[DataRow(double.PositiveInfinity, "∞")]
		[DataRow(double.NegativeInfinity, "-∞")]
		[DataRow(double.NaN, "NaN")]
		public void When_FormatSpecialDouble(double value, string expected)
		{
			var formatter = new PercentFormatter();
			var actual = formatter.FormatDouble(value);

			Assert.AreEqual(expected, actual);
		}

		[DataTestMethod]
		[DataRow(0.015d, 1, 2, "1.50%")]
		[DataRow(0.01567d, 1, 2, "1.567%")]
		[DataRow(0.015602d, 1, 2, "1.5602%")]
		[DataRow(0d, 0, 0, "0%")]
		[DataRow(-0d, 0, 0, "0%")]
		[DataRow(0d, 0, 2, ".00%")]
		[DataRow(-0d, 0, 2, ".00%")]
		[DataRow(0d, 2, 0, "00%")]
		[DataRow(-0d, 2, 0, "00%")]
		[DataRow(0d, 3, 1, "000.0%")]
		[DataRow(-0d, 3, 1, "000.0%")]
		public void When_FormatDouble(double value, int integerDigits, int fractionDigits, string expected)
		{
			var formatter = new PercentFormatter();
			formatter.IntegerDigits = integerDigits;
			formatter.FractionDigits = fractionDigits;

			var formatted = formatter.FormatDouble(value);
			Assert.AreEqual(expected, formatted);
		}

		[DataTestMethod]
		[DataRow(12.34, 2, 0, "1,234%")]
		[DataRow(12.34, 6, 0, "001,234%")]
		[DataRow(12.3456, 2, 2, "1,234.56%")]
		[DataRow(12.340, 6, 2, "001,234.00%")]
		[DataRow(12.340, 6, 0, "001,234%")]
		public void When_FormatDoubleWithIsGroupSetTrue(double value, int integerDigits, int fractionDigits, string expected)
		{
			var formatter = new PercentFormatter();
			formatter.IntegerDigits = integerDigits;
			formatter.FractionDigits = fractionDigits;
			formatter.IsGrouped = true;

			var formatted = formatter.FormatDouble(value);
			Assert.AreEqual(expected, formatted);
		}

		[DataTestMethod]
		[DataRow(0, 0, "-0%")]
		[DataRow(0, 2, "-.00%")]
		[DataRow(2, 0, "-00%")]
		[DataRow(3, 1, "-000.0%")]
		public void When_FormatDoubleMinusZeroWithIsZeroSignedSetTrue(int integerDigits, int fractionDigits, string expected)
		{
			var formatter = new PercentFormatter();
			formatter.IntegerDigits = integerDigits;
			formatter.FractionDigits = fractionDigits;
			formatter.IsZeroSigned = true;

			var formatted = formatter.FormatDouble(-0d);
			Assert.AreEqual(expected, formatted);
		}

		[DataTestMethod]
		[DataRow(0, 0, "0%")]
		[DataRow(0, 2, ".00%")]
		[DataRow(2, 0, "00%")]
		[DataRow(3, 1, "000.0%")]
		public void When_FormatDoubleZeroWithIsZeroSignedSetTrue(int integerDigits, int fractionDigits, string expected)
		{
			var formatter = new PercentFormatter();
			formatter.IntegerDigits = integerDigits;
			formatter.FractionDigits = fractionDigits;
			formatter.IsZeroSigned = true;

			var formatted = formatter.FormatDouble(0d);
			Assert.AreEqual(expected, formatted);
		}

		[DataTestMethod]
		[DataRow(0.01d, "1.%")]
		public void When_FormatDoubleWithIsDecimalPointerAlwaysDisplayedSetTrue(double value, string expected)
		{
			var formatter = new PercentFormatter();
			formatter.IsDecimalPointAlwaysDisplayed = true;
			formatter.FractionDigits = 0;
			formatter.IntegerDigits = 0;

			var formatted = formatter.FormatDouble(value);
			Assert.AreEqual(expected, formatted);
		}

		[DataTestMethod]
		[DataRow(1.234567d, 5, 1, 2, "123.4567%")]
		[DataRow(1.234567d, 10, 1, 2, "123.4567000%")]
		[DataRow(1.234567d, 2, 1, 2, "123.4567%")]
		[DataRow(0.123d, 4, 1, 2, "12.30%")]
		[DataRow(0.123d, 4, 1, 0, "12.30%")]
		public void When_FormatDoubleWithSpecificSignificantDigits(double value, int significantDigits, int integerDigits, int fractionDigits, string expected)
		{
			var formatter = new PercentFormatter();
			formatter.SignificantDigits = significantDigits;
			formatter.IntegerDigits = integerDigits;
			formatter.FractionDigits = fractionDigits;

			var formatted = formatter.FormatDouble(value);
			Assert.AreEqual(expected, formatted);
		}

		[TestMethod]
		public void When_FormatDoubleUsingIncrementNumberRounder()
		{
			var formatter = new PercentFormatter();
			IncrementNumberRounder rounder = new IncrementNumberRounder();
			rounder.Increment = 0.5;
			formatter.NumberRounder = rounder;
			var formatted = formatter.FormatDouble(1.8);

			Assert.AreEqual("200.00%", formatted);
		}

		[TestMethod]
		public void When_FormatDoubleUsingSignificantDigitsNumberRounder()
		{
			var formatter = new PercentFormatter();
			SignificantDigitsNumberRounder rounder = new SignificantDigitsNumberRounder();
			rounder.SignificantDigits = 1;
			formatter.NumberRounder = rounder;
			var formatted = formatter.FormatDouble(1.8);

			Assert.AreEqual("200.00%", formatted);
		}

		[TestMethod]
		public void When_Initialize()
		{
			var formatter = new PercentFormatter();

			Assert.AreEqual(0, formatter.SignificantDigits);
			Assert.AreEqual(1, formatter.IntegerDigits);
			Assert.AreEqual(2, formatter.FractionDigits);
			Assert.AreEqual(false, formatter.IsGrouped);
			Assert.AreEqual(false, formatter.IsZeroSigned);
			Assert.AreEqual(false, formatter.IsDecimalPointAlwaysDisplayed);
			Assert.AreEqual("en-US", formatter.ResolvedLanguage);
			Assert.IsNull(formatter.NumberRounder);
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
		[DataRow("1.2%", 0.012)]
		[DataRow(" 1.2%", null)]
		[DataRow("1.2% ", null)]
		[DataRow("1.20%", 0.012)]
		[DataRow("1.2", null)]
		[DataRow("%1.20", null)]
		[DataRow("1.20 %", null)]
		[DataRow("12,34.2%", null)]
		[DataRow("0%", 0d)]
		public void When_ParseDouble(string value, double? expected)
		{
			var formatter = new PercentFormatter();
			formatter.FractionDigits = 2;

			var actual = formatter.ParseDouble(value);
			Assert.AreEqual(expected, actual);
		}

		[DataTestMethod]
		[DataRow("1234.2%", 12.342)]
		[DataRow("1,234.2%", 12.342)]
		[DataRow("12,34.2%", null)]
		public void When_ParseDoubleAndIsGroupSetTrue(string value, double? expected)
		{
			var formatter = new PercentFormatter();
			formatter.FractionDigits = 2;
			formatter.IsGrouped = true;

			var actual = formatter.ParseDouble(value);
			Assert.AreEqual(expected, actual);
		}

		[DataTestMethod]
		[DataRow("1%", 0.01)]
		[DataRow("1.%", 0.01)]
		public void When_ParseDoubleAndIsDecimalPointAlwaysDisplayedSetTrue(string value, double? expected)
		{
			var formatter = new PercentFormatter();
			formatter.FractionDigits = 2;
			formatter.IsDecimalPointAlwaysDisplayed = true;

			var actual = formatter.ParseDouble(value);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void When_ParseDoubleMinusZero()
		{
			var formatter = new PercentFormatter();
			var actual = formatter.ParseDouble("-0%");
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
			var formatter = new PercentFormatter();
			formatter.NumeralSystem = numeralSystem;

			var translator = new NumeralSystemTranslator { NumeralSystem = numeralSystem };
			var translated = translator.TranslateNumerals("123456.789%");

			var actual = formatter.ParseDouble(translated);
			Assert.AreEqual(1234.56789, actual);
		}

		[TestMethod]
		public void When_ParseNotValidDouble()
		{
			var formatter = new PercentFormatter();

			var actual = formatter.ParseDouble("a12%");
			Assert.AreEqual(null, actual);
		}
	}
}
