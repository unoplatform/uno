#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Globalization.NumberFormatting;

namespace Uno.UI.Tests.Windows_Globalization
{
	[TestClass]
	public class Given_PermilleFormatter
	{
		[TestMethod]
		[DataRow(double.PositiveInfinity, "∞")]
		[DataRow(double.NegativeInfinity, "-∞")]
		[DataRow(double.NaN, "NaN")]
		public void When_FormatSpecialDouble(double value, string expected)
		{
			var sut = MakeFormatter();
			var actual = sut.FormatDouble(value);

			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		[DataRow(0.0015d, 1, 2, "1.50‰")]
		[DataRow(0.001567d, 1, 2, "1.567‰")]
		[DataRow(0.0015602d, 1, 2, "1.5602‰")]
		[DataRow(0d, 0, 0, "0‰")]
		[DataRow(-0d, 0, 0, "0‰")]
		[DataRow(0d, 0, 2, ".00‰")]
		[DataRow(-0d, 0, 2, ".00‰")]
		[DataRow(0d, 2, 0, "00‰")]
		[DataRow(-0d, 2, 0, "00‰")]
		[DataRow(0d, 3, 1, "000.0‰")]
		[DataRow(-0d, 3, 1, "000.0‰")]
		public void When_FormatDouble(double value, int integerDigits, int fractionDigits, string expected)
		{
			var sut = MakeFormatter();
			sut.IntegerDigits = integerDigits;
			sut.FractionDigits = fractionDigits;

			var formatted = sut.FormatDouble(value);
			Assert.AreEqual(expected, formatted);
		}

		[TestMethod]
		[DataRow(1.234, 2, 0, "1,234‰")]
		[DataRow(1.234, 6, 0, "001,234‰")]
		[DataRow(1.23456, 2, 2, "1,234.56‰")]
		[DataRow(1.2340, 6, 2, "001,234.00‰")]
		public void When_FormatDoubleWithIsGroupSetTrue(double value, int integerDigits, int fractionDigits, string expected)
		{
			var sut = MakeFormatter();
			sut.IntegerDigits = integerDigits;
			sut.FractionDigits = fractionDigits;
			sut.IsGrouped = true;

			var formatted = sut.FormatDouble(value);
			Assert.AreEqual(expected, formatted);
		}

		[TestMethod]
		[DataRow(0, 0, "-0‰")]
		[DataRow(0, 2, "-.00‰")]
		[DataRow(2, 0, "-00‰")]
		[DataRow(3, 1, "-000.0‰")]
		public void When_FormatDoubleMinusZeroWithIsZeroSignedSetTrue(int integerDigits, int fractionDigits, string expected)
		{
			var sut = MakeFormatter();
			sut.IntegerDigits = integerDigits;
			sut.FractionDigits = fractionDigits;
			sut.IsZeroSigned = true;

			var formatted = sut.FormatDouble(-0d);
			Assert.AreEqual(expected, formatted);
		}

		[TestMethod]
		[DataRow(0, 0, "0‰")]
		[DataRow(0, 2, ".00‰")]
		[DataRow(2, 0, "00‰")]
		[DataRow(3, 1, "000.0‰")]
		public void When_FormatDoubleZeroWithIsZeroSignedSetTrue(int integerDigits, int fractionDigits, string expected)
		{
			var sut = MakeFormatter();
			sut.IntegerDigits = integerDigits;
			sut.FractionDigits = fractionDigits;
			sut.IsZeroSigned = true;

			var formatted = sut.FormatDouble(0d);
			Assert.AreEqual(expected, formatted);
		}

		[TestMethod]
		[DataRow(0.001d, "1.‰")]
		public void When_FormatDoubleWithIsDecimalPointerAlwaysDisplayedSetTrue(double value, string expected)
		{
			var sut = MakeFormatter();
			sut.IsDecimalPointAlwaysDisplayed = true;
			sut.FractionDigits = 0;
			sut.IntegerDigits = 0;

			var formatted = sut.FormatDouble(value);
			Assert.AreEqual(expected, formatted);
		}

		[TestMethod]
		[DataRow(0.1234567d, 5, 1, 2, "123.4567‰")]
		[DataRow(0.1234567d, 10, 1, 2, "123.4567000‰")]
		[DataRow(0.1234567d, 2, 1, 2, "123.4567‰")]
		[DataRow(0.0123d, 4, 1, 2, "12.30‰")]
		[DataRow(0.0123d, 4, 1, 0, "12.30‰")]
		public void When_FormatDoubleWithSpecificSignificantDigits(double value, int significantDigits, int integerDigits, int fractionDigits, string expected)
		{
			var sut = MakeFormatter();
			sut.SignificantDigits = significantDigits;
			sut.IntegerDigits = integerDigits;
			sut.FractionDigits = fractionDigits;

			var formatted = sut.FormatDouble(value);
			Assert.AreEqual(expected, formatted);
		}

		[TestMethod]
		public void When_FormatDoubleUsingIncrementNumberRounder()
		{
			var sut = MakeFormatter();
			IncrementNumberRounder rounder = new IncrementNumberRounder();
			rounder.Increment = 0.5;
			sut.NumberRounder = rounder;
			var formatted = sut.FormatDouble(1.8);

			Assert.AreEqual("2000.00‰", formatted);
		}

		[TestMethod]
		public void When_FormatDoubleUsingSignificantDigitsNumberRounder()
		{
			var sut = MakeFormatter();
			SignificantDigitsNumberRounder rounder = new SignificantDigitsNumberRounder();
			rounder.SignificantDigits = 1;
			sut.NumberRounder = rounder;
			var formatted = sut.FormatDouble(1.8);

			Assert.AreEqual("2000.00‰", formatted);
		}

		[TestMethod]
		public void When_Initialize()
		{
			var sut = MakeFormatter();

			Assert.AreEqual(0, sut.SignificantDigits);
			Assert.AreEqual(1, sut.IntegerDigits);
			Assert.AreEqual(2, sut.FractionDigits);
			Assert.IsFalse(sut.IsGrouped);
			Assert.IsFalse(sut.IsZeroSigned);
			Assert.IsFalse(sut.IsDecimalPointAlwaysDisplayed);
			Assert.AreEqual("en-US", sut.ResolvedLanguage);
			Assert.IsNull(sut.NumberRounder);
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

		[TestMethod]
		[DataRow("1.2‰", 0.0012)]
		[DataRow(" 1.2‰", null)]
		[DataRow("1.2‰ ", null)]
		[DataRow("1.20‰", 0.0012)]
		[DataRow("1.2", null)]
		[DataRow("‰1.20", null)]
		[DataRow("1.20 ‰", null)]
		[DataRow("12,34.2‰", null)]
		[DataRow("0‰", 0d)]
		public void When_ParseDouble(string value, double? expected)
		{
			var sut = MakeFormatter();
			sut.FractionDigits = 2;

			var actual = sut.ParseDouble(value);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		[DataRow("1234.2‰", 1.2342)]
		[DataRow("1,234.2‰", 1.2342)]
		[DataRow("12,34.2‰", null)]
		public void When_ParseDoubleAndIsGroupSetTrue(string value, double? expected)
		{
			var sut = MakeFormatter();
			sut.FractionDigits = 2;
			sut.IsGrouped = true;

			var actual = sut.ParseDouble(value);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		[DataRow("1‰", 0.001)]
		[DataRow("1.‰", 0.001)]
		public void When_ParseDoubleAndIsDecimalPointAlwaysDisplayedSetTrue(string value, double? expected)
		{
			var sut = MakeFormatter();
			sut.FractionDigits = 2;
			sut.IsDecimalPointAlwaysDisplayed = true;

			var actual = sut.ParseDouble(value);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void When_ParseDoubleMinusZero()
		{
			var sut = MakeFormatter();
			var actual = sut.ParseDouble("-0‰");
			bool isNegative = false;

			if (actual.HasValue)
			{
				isNegative = BitConverter.DoubleToInt64Bits(actual.Value) < 0;
			}

			Assert.IsTrue(isNegative);
		}

		[TestMethod]
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
			var sut = MakeFormatter();
			sut.NumeralSystem = numeralSystem;

			var translator = new NumeralSystemTranslator { NumeralSystem = numeralSystem };
			var translated = translator.TranslateNumerals("1234567.89‰");

			var actual = sut.ParseDouble(translated);
			Assert.AreEqual(1234.56789, actual);
		}

		[TestMethod]
		public void When_ParseNotValidDouble()
		{
			var sut = MakeFormatter();

			var actual = sut.ParseDouble("a12‰");
			Assert.IsNull(actual);
		}

		// In UWP PermilleFormatter() ignore PrimaryLanguageOverride
		// and use the localization settings of the OS;
		// to avoid this you need to use the constructor
		// public PermilleFormatter([In] IEnumerable<string> languages, [In] string geographicRegion).
		private static PermilleFormatter MakeFormatter() =>
#if HAS_UNO || IS_UNIT_TESTS
			new PermilleFormatter();
#else
			new PermilleFormatter(new[] { "en-us" }, "US");
#endif
	}
}
