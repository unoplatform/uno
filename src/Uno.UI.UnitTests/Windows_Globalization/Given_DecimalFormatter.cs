#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Globalization.NumberFormatting;

namespace Uno.UI.Tests.Windows_Globalization
{
	[TestClass]
	public class Given_DecimalFormatter
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
			var sut = MakeFormatter();
			sut.IntegerDigits = integerDigits;
			sut.FractionDigits = fractionDigits;

			var formatted = sut.FormatDouble(value);
			Assert.AreEqual(expected, formatted);
		}

		[TestMethod]
		[DataRow(1234, 2, 0, "1,234")]
		[DataRow(1234, 6, 0, "001,234")]
		[DataRow(1234.56, 2, 2, "1,234.56")]
		[DataRow(1234.0, 6, 2, "001,234.00")]
		[DataRow(1234.0, 6, 0, "001,234")]
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
		[DataRow(0, 0, "-0")]
		[DataRow(0, 2, "-.00")]
		[DataRow(2, 0, "-00")]
		[DataRow(3, 1, "-000.0")]
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
		[DataRow(0, 0, "0")]
		[DataRow(0, 2, ".00")]
		[DataRow(2, 0, "00")]
		[DataRow(3, 1, "000.0")]
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
		[DataRow(1d, "1.")]
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
		[DataRow(123.4567d, 5, 1, 2, "123.4567")]
		[DataRow(123.4567d, 10, 1, 2, "123.4567000")]
		[DataRow(123.4567d, 2, 1, 2, "123.4567")]
		[DataRow(12.3d, 4, 1, 2, "12.30")]
		[DataRow(12.3d, 4, 1, 0, "12.30")]
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

			Assert.AreEqual("2.00", formatted);
		}

		[TestMethod]
		public void When_FormatDoubleUsingSignificantDigitsNumberRounder()
		{
			var sut = MakeFormatter();
			SignificantDigitsNumberRounder rounder = new SignificantDigitsNumberRounder();
			rounder.SignificantDigits = 1;
			sut.NumberRounder = rounder;
			var formatted = sut.FormatDouble(1.8);

			Assert.AreEqual("2.00", formatted);
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
			Assert.AreEqual("Latn", sut.NumeralSystem);
			Assert.AreEqual("US", sut.GeographicRegion);
			Assert.AreEqual("ZZ", sut.ResolvedGeographicRegion);
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
		[DataRow("1.2", 1.2)]
		[DataRow("-1.2", -1.2)]
		[DataRow("+1", null)]
		[DataRow("+1.2", null)]
		[DataRow("1.2 ", null)]
		[DataRow(" 1.2", null)]
		[DataRow("1.2\t", null)]
		[DataRow("\t1.2", null)]
		[DataRow("1.20", 1.2)]
		[DataRow("12,34.2", null)]
		[DataRow("0", 0d)]
		public void When_ParseDouble(string value, double? expected)
		{
			var sut = MakeFormatter();
			sut.FractionDigits = 2;

			var actual = sut.ParseDouble(value);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		[DataRow("1234.2", 1234.2)]
		[DataRow("1,234.2", 1234.2)]
		[DataRow("12,34.2", null)]
		public void When_ParseDoubleAndIsGroupSetTrue(string value, double? expected)
		{
			var sut = MakeFormatter();
			sut.FractionDigits = 2;
			sut.IsGrouped = true;

			var actual = sut.ParseDouble(value);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		[DataRow("1", 1d)]
		[DataRow("1.", 1d)]
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
			var actual = sut.ParseDouble("-0");
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
			var translated = translator.TranslateNumerals("1234.56789");

			var actual = sut.ParseDouble(translated);
			Assert.AreEqual(1234.56789, actual);
		}

		// In UWP DecimalFormatter() ignore PrimaryLanguageOverride
		// and use the localization settings of the OS;
		// to avoid this you need to use the constructor
		// public DecimalFormatter([In] IEnumerable<string> languages, [In] string geographicRegion).
		private static DecimalFormatter MakeFormatter() =>
#if HAS_UNO || IS_UNIT_TESTS
			new DecimalFormatter();
#else
			new DecimalFormatter(new[] { "en-us" }, "US");
#endif

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_LanguagesIsNull_Then_Throw()
		{
			Assert.ThrowsExactly<NullReferenceException>(() => new DecimalFormatter(languages: null!, "US"));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
#pragma warning disable MSTEST0014 // DataRow should be valid - Works in our case
		[DataRow(new string[0])]
		[DataRow(new string[] { "abcd" })]
		[DataRow(new string[] { "en-US", "abcd" })]
#pragma warning restore MSTEST0014 // DataRow should be valid
		public void When_LanguagesIsInvalid_Then_Throw(IEnumerable<string> languages)
		{
			Assert.ThrowsExactly<ArgumentException>(() => new DecimalFormatter(languages, "US"));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_GeographicRegionIsNull_Then_Throw()
		{
			Assert.ThrowsExactly<ArgumentException>(() => new DecimalFormatter(new[] { "en-US" }, geographicRegion: null!));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		[DataRow("")]
		[DataRow("NotARegion")]
		[DataRow("003")]
		[DataRow("123")]
		[DataRow("899")]
		[DataRow("1234")]
		public void When_GeographicRegionIsInvalid_Then_Throw(string geographicRegion)
		{
			Assert.ThrowsExactly<ArgumentException>(() => new DecimalFormatter(new[] { "en-US" }, geographicRegion));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		[DataRow("000")]
		[DataRow("419")]
		[DataRow("001")]
		[DataRow("840")]
		[DataRow("900")]
		[DataRow("999")]
		[DataRow("XA")]
		[DataRow("XB")]
		[DataRow("XX")]
		[DataRow("ZZ")]
		public void When_GeographicRegionIsSupportedByWinRT_Then_Accept(string geographicRegion)
		{
			var sut = new DecimalFormatter(new[] { "en-US" }, geographicRegion);

			Assert.AreEqual(geographicRegion, sut.GeographicRegion);
			Assert.AreEqual("ZZ", sut.ResolvedGeographicRegion);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		[DataRow("en-US", "IN", "en-US", "1,234,567.50")]
		[DataRow("en-IN", "US", "en-IN", "12,34,567.50")]
		[DataRow("en", "IN", "en", "1,234,567.50")]
		public void When_LanguageResolves_Then_RegionDoesNotOverrideNumberFormat(
			string language,
			string geographicRegion,
			string expectedResolvedLanguage,
			string expected)
		{
			var sut = new DecimalFormatter(new[] { language }, geographicRegion)
			{
				IsGrouped = true,
				IntegerDigits = 2,
				FractionDigits = 2,
			};

			Assert.AreEqual(geographicRegion, sut.GeographicRegion);
			Assert.AreEqual(expectedResolvedLanguage, sut.ResolvedLanguage);
			Assert.AreEqual(expected, sut.FormatDouble(1234567.5));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_ConstructedWithLanguagesAndRegion_Then_PropertiesAreResolved()
		{
			var sut = new DecimalFormatter(new[] { "en-US" }, "US");

			Assert.AreEqual(1, sut.Languages.Count);
			Assert.AreEqual("en-US", sut.Languages[0]);
			Assert.AreEqual("en-US", sut.ResolvedLanguage);
			Assert.AreEqual("Latn", sut.NumeralSystem);
			Assert.AreEqual("US", sut.GeographicRegion);
			Assert.AreEqual("ZZ", sut.ResolvedGeographicRegion);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_GeographicRegionIsLowercase_Then_Throw()
		{
			Assert.ThrowsExactly<ArgumentException>(() => new DecimalFormatter(new[] { "en-US" }, "us"));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_ConstructedWithLanguagesAndRegion_Then_FormattingMatchesDefaultConstructor()
		{
			// en-US's separators/group sizes are the same as CultureInfo.InvariantCulture, so the
			// locale-aware constructor must format identically to the default constructor.
			var sut = new DecimalFormatter(new[] { "en-US" }, "US");
			sut.IsGrouped = true;
			sut.IntegerDigits = 2;
			sut.FractionDigits = 2;

			Assert.AreEqual("1,234.50", sut.FormatDouble(1234.5));
			Assert.AreEqual(1234.5, sut.ParseDouble("1,234.50"));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_UniformGroupingHasMultipleSeparators_Then_RoundTrips()
		{
			var sut = new DecimalFormatter(new[] { "en-US" }, "US")
			{
				IsGrouped = true,
				IntegerDigits = 2,
				FractionDigits = 2,
			};

			var formatted = sut.FormatDouble(1234567);

			Assert.AreEqual("1,234,567.00", formatted);
			Assert.AreEqual(1234567d, sut.ParseDouble(formatted));
			Assert.IsNull(sut.ParseDouble("+1,234,567.00"));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_DecimalCommaLocale_Then_FormatUsesLocaleSeparators()
		{
			var culture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
			var decimalSeparator = culture.NumberFormat.NumberDecimalSeparator;

			var sut = new DecimalFormatter(new[] { "fr-FR" }, "FR");
			sut.IntegerDigits = 1;
			sut.FractionDigits = 2;

			Assert.AreEqual("Latn", sut.NumeralSystem);
			Assert.AreEqual($"1{decimalSeparator}50", sut.FormatDouble(1.5));
			Assert.AreEqual(1.5, sut.ParseDouble($"1{decimalSeparator}50"));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_DecimalCommaLocale_Then_GroupingUsesLocaleGroupSeparator()
		{
			var culture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
			var decimalSeparator = culture.NumberFormat.NumberDecimalSeparator;
			var groupSeparator = culture.NumberFormat.NumberGroupSeparator;

			var sut = new DecimalFormatter(new[] { "fr-FR" }, "FR");
			sut.IsGrouped = true;
			sut.IntegerDigits = 2;
			sut.FractionDigits = 2;

			var expected = $"1{groupSeparator}234{decimalSeparator}50";
			var formatted = sut.FormatDouble(1234.5);

			Assert.AreEqual(expected, formatted);
			Assert.AreEqual(1234.5, sut.ParseDouble(formatted));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_NonUniformGroupSizeLocale_Then_GroupsUsingLocaleGroupSizes()
		{
			// "en-IN" uses NumberGroupSizes {3, 2} (lakh/crore grouping, e.g. "12,34,567") on real
			// Windows/WinRT. .NET's custom numeric picture format string (built by
			// FormatterHelper.AppendFormatIntegerPart) honors the NumberFormatInfo's NumberGroupSizes
			// when formatting via StringBuilder.AppendFormat, so locale-aware grouping is correctly
			// non-uniform here - this test pins that behavior against regression.
			var sut = new DecimalFormatter(new[] { "en-IN" }, "IN");
			sut.IsGrouped = true;
			sut.IntegerDigits = 2;
			sut.FractionDigits = 2;

			Assert.AreEqual("12,34,567.00", sut.FormatDouble(1234567));
			Assert.AreEqual(1234567d, sut.ParseDouble("12,34,567.00"));
			Assert.IsNull(sut.ParseDouble("1,234,567.00"));
			Assert.IsNull(sut.ParseDouble("123,45,67.00"));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_DecimalCommaLocale_Then_NegativeZeroRoundTrips()
		{
			var culture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
			var negativeSign = culture.NumberFormat.NegativeSign;

			var sut = new DecimalFormatter(new[] { "fr-FR" }, "FR");
			sut.IsZeroSigned = true;
			sut.IntegerDigits = 1;
			sut.FractionDigits = 0;

			var formatted = sut.FormatDouble(-0d);
			Assert.AreEqual($"{negativeSign}0", formatted);

			var parsed = sut.ParseDouble(formatted);
			Assert.IsTrue(parsed.HasValue);
			Assert.IsTrue(BitConverter.DoubleToInt64Bits(parsed!.Value) < 0);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_ArabicLanguage_Then_NumeralSystemIsArabAndDigitsAreLocalized()
		{
			var sut = new DecimalFormatter(new[] { "ar-SA" }, "SA");
			sut.IntegerDigits = 1;
			sut.FractionDigits = 2;

			Assert.AreEqual("Arab", sut.NumeralSystem);
			Assert.AreEqual("ar-SA", sut.ResolvedLanguage);

			var formatted = sut.FormatDouble(12.5);

			// No ASCII digits should remain: they were all translated to Arabic-Indic digits.
			foreach (var c in formatted)
			{
				Assert.IsFalse(c >= '0' && c <= '9', $"Unexpected ASCII digit '{c}' in '{formatted}'");
			}

			Assert.AreEqual(12.5, sut.ParseDouble(formatted));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_ArabicLanguage_Then_GroupingAndDecimalPunctuationRoundTrip()
		{
			var sut = new DecimalFormatter(new[] { "ar-SA" }, "SA");
			sut.IsGrouped = true;
			sut.IntegerDigits = 2;
			sut.FractionDigits = 2;

			var formatted = sut.FormatDouble(1234.5);

			Assert.IsTrue(formatted.Contains('\u066c'), "Expected the Arabic thousands separator.");
			Assert.IsTrue(formatted.Contains('\u066b'), "Expected the Arabic decimal separator.");
			Assert.AreEqual(1234.5, sut.ParseDouble(formatted));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_NumeralSystemChangedAfterConstruction_Then_ArabicRoundTrips()
		{
			// Switching NumeralSystem after construction must re-coordinate the underlying punctuation
			// source with the translator so Arabic digits/punctuation still round-trip correctly.
			var sut = new DecimalFormatter(new[] { "fr-FR" }, "FR");
			sut.IntegerDigits = 1;
			sut.FractionDigits = 2;
			sut.NumeralSystem = "Arab";

			var formatted = sut.FormatDouble(1.5);

			foreach (var c in formatted)
			{
				Assert.IsFalse(c >= '0' && c <= '9', $"Unexpected ASCII digit '{c}' in '{formatted}'");
			}

			Assert.AreEqual(1.5, sut.ParseDouble(formatted));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		// ISO 3166-1 alpha-3 codes, including the user-assigned AAA-AAZ/QMA-QZZ/XAA-XZZ/ZZA-ZZZ blocks
		// and the Windows-specific "OOO" operator pseudo-region, are all accepted by native WinRT.
		[DataRow("USA")]
		[DataRow("FRA")]
		[DataRow("IND")]
		[DataRow("ANT")]
		[DataRow("OOO")]
		[DataRow("AAA")]
		[DataRow("QQQ")]
		[DataRow("XYZ")]
		[DataRow("ZZZ")]
		public void When_GeographicRegionIsAlpha3_Then_Accept(string geographicRegion)
		{
			var sut = new DecimalFormatter(new[] { "en-US" }, geographicRegion);

			Assert.AreEqual(geographicRegion, sut.GeographicRegion);
			Assert.AreEqual("ZZ", sut.ResolvedGeographicRegion);
			Assert.AreEqual("1234.50", sut.FormatDouble(1234.5));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		// Native WinRT only accepts ISO 3166-1 assigned alpha-2/alpha-3 codes plus the user-assigned
		// ranges - an arbitrary uppercase letter pair is rejected.
		[DataRow("AB")]
		[DataRow("EU")]
		[DataRow("UK")]
		[DataRow("DA")]
		[DataRow("ZB")]
		[DataRow("USa")]
		[DataRow("usa")]
		[DataRow("AB1")]
		[DataRow("A1B")]
		[DataRow("ABCD")]
		public void When_GeographicRegionIsUnassigned_Then_Throw(string geographicRegion)
		{
			Assert.ThrowsExactly<ArgumentException>(() => new DecimalFormatter(new[] { "en-US" }, geographicRegion));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		// Native WinRT's decimal parser has no exponent syntax, so scientific notation is not a number.
		[DataRow("1e2")]
		[DataRow("1E2")]
		[DataRow("1.5e-1")]
		[DataRow("0e-1")]
		[DataRow("0E-1")]
		[DataRow("-0e1")]
		[DataRow("0.0e-5")]
		public void When_ParseDoubleWithExponent_Then_Null(string text)
		{
			var sut = MakeFormatter();

			Assert.IsNull(sut.ParseDouble(text));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_ParseDoubleSpecialValues_Then_RoundTripFormatting()
		{
			var sut = MakeFormatter();

			Assert.IsTrue(double.IsNaN(sut.ParseDouble(sut.FormatDouble(double.NaN))!.Value));
			Assert.AreEqual(double.PositiveInfinity, sut.ParseDouble(sut.FormatDouble(double.PositiveInfinity)));
			Assert.AreEqual(double.NegativeInfinity, sut.ParseDouble(sut.FormatDouble(double.NegativeInfinity)));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		// WinRT parses back only the exact symbols it formats: the comparison is case-sensitive and
		// no sign variation is accepted.
		[DataRow("nan")]
		[DataRow("NAN")]
		[DataRow("+\u221e")]
		[DataRow("\u2212\u221e")]
		[DataRow("Infinity")]
		[DataRow("-Infinity")]
		[DataRow("+Infinity")]
		[DataRow("infinity")]
		[DataRow("inf")]
		[DataRow(" NaN")]
		[DataRow("NaN ")]
		public void When_ParseDoubleSpecialValueVariant_Then_Null(string text)
		{
			var sut = MakeFormatter();

			Assert.IsNull(sut.ParseDouble(text));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		// ICU (and therefore .NET) reports U+2212 MINUS SIGN for these locales while the Windows NLS
		// data that WinRT reads uses the ASCII hyphen-minus for every locale.
		[DataRow("sv-SE", "SE")]
		[DataRow("fi-FI", "FI")]
		[DataRow("lt-LT", "LT")]
		[DataRow("et-EE", "EE")]
		[DataRow("nb-NO", "NO")]
		public void When_LocaleUsesNonAsciiMinusSign_Then_FormatsAsciiHyphen(string language, string geographicRegion)
		{
			var sut = new DecimalFormatter(new[] { language }, geographicRegion)
			{
				IsGrouped = true,
				IntegerDigits = 1,
				FractionDigits = 2,
			};

			var formatted = sut.FormatDouble(-1234.5);

			Assert.AreEqual('-', formatted[0], $"Expected an ASCII hyphen-minus in '{formatted}'.");
			Assert.AreEqual(-1234.5, sut.ParseDouble(formatted));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_GroupedWithSingleIntegerDigit_Then_ThousandsAreSeparated()
		{
			var sut = new DecimalFormatter(new[] { "en-US" }, "US")
			{
				IsGrouped = true,
				IntegerDigits = 1,
				FractionDigits = 2,
			};

			Assert.AreEqual("1,234.50", sut.FormatDouble(1234.5));
			Assert.AreEqual("1,234,567.00", sut.FormatDouble(1234567));
			Assert.AreEqual("-1,234.50", sut.FormatDouble(-1234.5));
			Assert.AreEqual(1234.5, sut.ParseDouble("1,234.50"));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_GroupedWithoutIntegerDigits_Then_ThousandsAreSeparated()
		{
			var sut = new DecimalFormatter(new[] { "en-US" }, "US")
			{
				IsGrouped = true,
				IntegerDigits = 0,
				FractionDigits = 2,
			};

			Assert.AreEqual("1,234.50", sut.FormatDouble(1234.5));
			Assert.AreEqual(".50", sut.FormatDouble(0.5));
			Assert.AreEqual(".00", sut.FormatDouble(0d));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		[DataRow(2147483648d, "2,147,483,648.00")]
		[DataRow(-2147483649d, "-2,147,483,649.00")]
		[DataRow(4294967296d, "4,294,967,296.00")]
		[DataRow(1e15, "1,000,000,000,000,000.00")]
		[DataRow(9007199254740992d, "9,007,199,254,740,992.00")]
		public void When_ValueExceedsInt32Range_Then_IntegerPartIsPreserved(double value, string expected)
		{
			var sut = new DecimalFormatter(new[] { "en-US" }, "US")
			{
				IsGrouped = true,
				IntegerDigits = 1,
				FractionDigits = 2,
			};

			var formatted = sut.FormatDouble(value);

			Assert.AreEqual(expected, formatted);
			Assert.AreEqual(value, sut.ParseDouble(formatted));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_ValueIsNegativeFraction_Then_SignIsPreserved()
		{
			var sut = new DecimalFormatter(new[] { "en-US" }, "US")
			{
				IntegerDigits = 1,
				FractionDigits = 2,
			};

			Assert.AreEqual("-0.125", sut.FormatDouble(-0.125));
			Assert.AreEqual("-0.50", sut.FormatDouble(-0.5));
			Assert.AreEqual(-0.125, sut.ParseDouble("-0.125"));

			sut.IntegerDigits = 0;

			Assert.AreEqual("-.50", sut.FormatDouble(-0.5));
			Assert.AreEqual(".50", sut.FormatDouble(0.5));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		// .NET renders magnitudes below 1e-4 in scientific notation, which has no decimal separator to
		// take the fraction from; WinRT always writes the digits out in full.
		[DataRow(1e-5, "0.00001")]
		[DataRow(-1e-5, "-0.00001")]
		[DataRow(1.5e-7, "0.00000015")]
		[DataRow(1.5e-5, "0.000015")]
		[DataRow(1.2345e-9, "0.0000000012345")]
		[DataRow(1e-20, "0.00000000000000000001")]
		[DataRow(1.234567890123e-5, "0.00001234567890123")]
		public void When_ValueIsBelowFractionDigits_Then_FixedPointDigitsArePrinted(double value, string expected)
		{
			var sut = new DecimalFormatter(new[] { "en-US" }, "US")
			{
				IntegerDigits = 1,
				FractionDigits = 2,
			};

			var formatted = sut.FormatDouble(value);

			Assert.AreEqual(expected, formatted);
			Assert.AreEqual(value, sut.ParseDouble(formatted));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_SmallValueLocaleUsesComma_Then_FixedPointUsesLocaleSeparator()
		{
			var culture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
			var sut = new DecimalFormatter(new[] { "fr-FR" }, "FR")
			{
				IntegerDigits = 1,
				FractionDigits = 2,
			};

			Assert.AreEqual($"0{culture.NumberFormat.NumberDecimalSeparator}00001", sut.FormatDouble(1e-5));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		// The sign has to be written before the "0" the zero fallback emits, otherwise the two would
		// come out in the wrong order.
		public void When_FormatIntegralIsNegativeZeroWithoutDigits_Then_SignLeads()
		{
			var sut = new DecimalFormatter(new[] { "en-US" }, "US")
			{
				IsZeroSigned = true,
				IntegerDigits = 0,
				FractionDigits = 0,
			};

			Assert.AreEqual("-0", sut.FormatDouble(-0d));
			Assert.AreEqual("0", sut.FormatDouble(0d));
			Assert.AreEqual("0", sut.FormatInt(0));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		[DataRow("en-US", "US", "00,000,000")]
		[DataRow("en-IN", "IN", "0,00,00,000")]
		public void When_GroupedZeroIsPadded_Then_LocaleGroupSizesApply(string language, string geographicRegion, string expected)
		{
			var sut = new DecimalFormatter(new[] { language }, geographicRegion)
			{
				IsGrouped = true,
				IntegerDigits = 8,
				FractionDigits = 0,
			};

			Assert.AreEqual(expected, sut.FormatDouble(0d));
			Assert.AreEqual(0d, sut.ParseDouble(expected));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_GroupedSignedZeroIsPadded_Then_SignPrefixesTheGroups()
		{
			var sut = new DecimalFormatter(new[] { "en-US" }, "US")
			{
				IsZeroSigned = true,
				IsGrouped = true,
				IntegerDigits = 8,
				FractionDigits = 0,
			};

			Assert.AreEqual("-00,000,000", sut.FormatDouble(-0d));
			Assert.AreEqual("00,000,000", sut.FormatDouble(0d));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_ParseDoubleOverflowsDoubleRange_Then_Null()
		{
			var sut = MakeFormatter();

			Assert.IsNull(sut.ParseDouble(new string('9', 400)));
			Assert.IsNull(sut.ParseDouble("1" + new string('0', 400)));
			Assert.AreEqual(0d, sut.ParseDouble("0." + new string('0', 400) + "1"));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		// The integer part is printed with the 17 significant digits that round-trip a double, padded
		// with zeros - not the exact binary expansion and not a 15 significant digit picture.
		[DataRow(1e19, "10000000000000000000.00")]
		[DataRow(1.2345678901234567e19, "12345678901234567000.00")]
		[DataRow(1e21, "1000000000000000000000.00")]
		[DataRow(9223372036854775808d, "9223372036854775800.00")]
		public void When_ValueExceedsInt64Range_Then_RoundTripDigitsArePreserved(double value, string expected)
		{
			var sut = new DecimalFormatter(new[] { "en-US" }, "US")
			{
				IntegerDigits = 1,
				FractionDigits = 2,
			};

			Assert.AreEqual(expected, sut.FormatDouble(value));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_ValueIsMaxDouble_Then_SeventeenSignificantDigitsArePrinted()
		{
			var sut = new DecimalFormatter(new[] { "en-US" }, "US")
			{
				IntegerDigits = 1,
				FractionDigits = 0,
			};

			var expected = "17976931348623157" + new string('0', 292);

			Assert.AreEqual(expected, sut.FormatDouble(double.MaxValue));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		// WinRT localizes NaN but never infinity, which always uses "∞" and an ASCII hyphen.
		[DataRow("en-US", "US")]
		[DataRow("fr-FR", "FR")]
		[DataRow("fi-FI", "FI")]
		[DataRow("ar-SA", "SA")]
		[DataRow("sv-SE", "SE")]
		public void When_SpecialValues_Then_InfinityIsNotLocalizedAndNaNRoundTrips(string language, string geographicRegion)
		{
			var sut = new DecimalFormatter(new[] { language }, geographicRegion);

			Assert.AreEqual("\u221e", sut.FormatDouble(double.PositiveInfinity));
			Assert.AreEqual("-\u221e", sut.FormatDouble(double.NegativeInfinity));
			Assert.AreEqual(double.PositiveInfinity, sut.ParseDouble("\u221e"));
			Assert.AreEqual(double.NegativeInfinity, sut.ParseDouble("-\u221e"));

			var nan = sut.FormatDouble(double.NaN);

			Assert.IsTrue(double.IsNaN(sut.ParseDouble(nan)!.Value), $"'{nan}' must round-trip.");

			// Only the formatter's own symbol is accepted, whichever one the locale data yields.
			Assert.IsNull(sut.ParseDouble(nan == "NaN" ? "ep\u00e4luku" : "NaN"));

#if HAS_UNO || IS_UNIT_TESTS
			// Uno resolves the NaN symbol from the same locale data .NET exposes. Native WinRT reads the
			// Windows locale data instead, which carries a different CLDR revision for a few languages.
			Assert.AreEqual(
				System.Globalization.CultureInfo.GetCultureInfo(language).NumberFormat.NaNSymbol,
				nan);
#endif
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		// .NET treats SPACE, NO-BREAK SPACE and NARROW NO-BREAK SPACE as interchangeable group
		// separators; WinRT only pairs SPACE with NO-BREAK SPACE. The separator is read back from the
		// formatter because which one a locale uses depends on the locale data revision.
		[DataRow("fr-FR", "FR")]
		[DataRow("sv-SE", "SE")]
		[DataRow("en-US", "US")]
		[DataRow("de-DE", "DE")]
		public void When_Grouped_Then_OnlyEquivalentSpacesParse(string language, string geographicRegion)
		{
			var sut = new DecimalFormatter(new[] { language }, geographicRegion)
			{
				IsGrouped = true,
				IntegerDigits = 1,
				FractionDigits = 2,
			};

			var formatted = sut.FormatDouble(1234.5);
			var separator = formatted[1];

			Assert.AreEqual(1234.5, sut.ParseDouble(formatted), $"'{language}' must parse its own output.");

			var separatorIsSpaceLike = separator is ' ' or '\u00a0';

			foreach (var candidate in new[] { ' ', '\u00a0', '\u202f' })
			{
				if (candidate == separator)
				{
					continue;
				}

				var expected = separatorIsSpaceLike && candidate is ' ' or '\u00a0' ? 1234.5 : (double?)null;

				Assert.AreEqual(
					expected,
					sut.ParseDouble(formatted.Replace(separator, candidate)),
					$"'{language}' with U+{(int)candidate:X4} as the separator");
			}
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_FormatIntegral_Then_MatchesFormatOverloads()
		{
			var sut = new DecimalFormatter(new[] { "en-US" }, "US");

			Assert.AreEqual("-42.00", sut.FormatInt(-42));
			Assert.AreEqual("42.00", sut.FormatUInt(42));
			Assert.AreEqual("-42.00", sut.Format(-42L));
			Assert.AreEqual("42.00", sut.Format(42UL));
			Assert.AreEqual("0.00", sut.FormatInt(0));
			Assert.AreEqual("0.00", sut.FormatUInt(0));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_FormatIntegralAtTypeLimits_Then_EveryDigitIsKept()
		{
			var sut = new DecimalFormatter(new[] { "en-US" }, "US");

			Assert.AreEqual("9223372036854775807.00", sut.FormatInt(long.MaxValue));
			Assert.AreEqual("-9223372036854775808.00", sut.FormatInt(long.MinValue));
			Assert.AreEqual("18446744073709551615.00", sut.FormatUInt(ulong.MaxValue));

			sut.IsGrouped = true;

			Assert.AreEqual("9,223,372,036,854,775,807.00", sut.FormatInt(long.MaxValue));
			Assert.AreEqual("18,446,744,073,709,551,615.00", sut.FormatUInt(ulong.MaxValue));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_FormatIntegral_Then_OptionsApply()
		{
			var sut = new DecimalFormatter(new[] { "en-US" }, "US")
			{
				IsGrouped = true,
				IntegerDigits = 6,
				FractionDigits = 2,
			};

			Assert.AreEqual("001,234.00", sut.FormatInt(1234));
			Assert.AreEqual("-001,234.00", sut.FormatInt(-1234));

			var indian = new DecimalFormatter(new[] { "en-IN" }, "IN")
			{
				IsGrouped = true,
				IntegerDigits = 6,
				FractionDigits = 2,
			};

			Assert.AreEqual("0,01,234.00", indian.FormatInt(1234));
			Assert.AreEqual("12,34,56,789.00", indian.FormatInt(123456789));

			var minimal = new DecimalFormatter(new[] { "en-US" }, "US")
			{
				IntegerDigits = 0,
				FractionDigits = 0,
			};

			Assert.AreEqual("0", minimal.FormatInt(0));
			Assert.AreEqual("7", minimal.FormatInt(7));

			minimal.IsDecimalPointAlwaysDisplayed = true;
			minimal.IntegerDigits = 1;

			Assert.AreEqual("7.", minimal.FormatInt(7));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_FormatIntegralIsArabic_Then_DigitsAreTranslated()
		{
			var sut = new DecimalFormatter(new[] { "ar-SA" }, "SA")
			{
				IsGrouped = true,
				IntegerDigits = 1,
				FractionDigits = 2,
			};

			var formatted = sut.FormatInt(-1234567);

			Assert.AreEqual('-', formatted[0]);
			Assert.IsTrue(formatted.Contains('\u066c'), "Expected the Arabic thousands separator.");
			Assert.IsTrue(formatted.Contains('\u066b'), "Expected the Arabic decimal separator.");
			Assert.AreEqual(-1234567L, sut.ParseInt(formatted.Substring(0, formatted.IndexOf('\u066b'))));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		// SignificantDigits pads the fraction with (SignificantDigits - integer digit count) zeros,
		// counting a zero value as one digit.
		public void When_SignificantDigits_Then_IntegralAndZeroArePadded()
		{
			var sut = new DecimalFormatter(new[] { "en-US" }, "US")
			{
				SignificantDigits = 6,
				IntegerDigits = 1,
				FractionDigits = 2,
			};

			Assert.AreEqual("42.0000", sut.FormatInt(42));
			Assert.AreEqual("-42.0000", sut.FormatInt(-42));
			Assert.AreEqual("1234567.00", sut.FormatInt(1234567));
			Assert.AreEqual("0.00000", sut.FormatInt(0));
			Assert.AreEqual("0.00000", sut.FormatDouble(0d));

			sut.IntegerDigits = 3;
			sut.FractionDigits = 1;

			Assert.AreEqual("000.00000", sut.FormatDouble(0d));
			Assert.AreEqual("005.00000", sut.FormatInt(5));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_NumberRounderIsSet_Then_IntegralOverloadsRoundExactly()
		{
			var increment = new DecimalFormatter(new[] { "en-US" }, "US")
			{
				IntegerDigits = 1,
				FractionDigits = 2,
				NumberRounder = new IncrementNumberRounder { Increment = 1000 },
			};

			Assert.AreEqual("2000.00", increment.FormatInt(1500));
			Assert.AreEqual("-1000.00", increment.FormatInt(-1500));
			Assert.AreEqual("3000.00", increment.FormatInt(2500));
			Assert.AreEqual("2000.00", increment.FormatUInt(1500));

			var identity = new DecimalFormatter(new[] { "en-US" }, "US")
			{
				IntegerDigits = 1,
				FractionDigits = 2,
				NumberRounder = new IncrementNumberRounder { Increment = 1 },
			};

			Assert.AreEqual("9223372036854775807.00", identity.FormatInt(long.MaxValue));
			Assert.AreEqual("18446744073709551615.00", identity.FormatUInt(ulong.MaxValue));
			Assert.AreEqual("-9223372036854775808.00", identity.FormatInt(long.MinValue));

			var fractionalIncrement = new DecimalFormatter(new[] { "en-US" }, "US")
			{
				IntegerDigits = 1,
				FractionDigits = 2,
				NumberRounder = new IncrementNumberRounder { Increment = 0.5 },
			};

			Assert.AreEqual("7.00", fractionalIncrement.FormatInt(7));
			Assert.AreEqual("9223372036854775807.00", fractionalIncrement.FormatInt(long.MaxValue));

			var significant = new DecimalFormatter(new[] { "en-US" }, "US")
			{
				IntegerDigits = 1,
				FractionDigits = 2,
				NumberRounder = new SignificantDigitsNumberRounder { SignificantDigits = 3 },
			};

			Assert.AreEqual("123000.00", significant.FormatInt(123456));
			Assert.AreEqual("-123000.00", significant.FormatInt(-123456));
			Assert.AreEqual("12.00", significant.FormatInt(12));
			Assert.AreEqual("18400000000000000000.00", significant.FormatUInt(ulong.MaxValue));
			Assert.AreEqual("-9220000000000000000.00", significant.FormatInt(long.MinValue));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		[DataRow("-42", -42L, null)]
		[DataRow("42", 42L, 42UL)]
		[DataRow("0", 0L, 0UL)]
		[DataRow("-0", 0L, null)]
		[DataRow("42.00", 42L, 42UL)]
		[DataRow("42.", 42L, 42UL)]
		[DataRow(".0", 0L, 0UL)]
		[DataRow("-.0", 0L, null)]
		[DataRow("0.000", 0L, 0UL)]
		[DataRow("007", 7L, 7UL)]
		[DataRow("1.5", null, null)]
		[DataRow("-1.5", null, null)]
		[DataRow(".5", null, null)]
		[DataRow("1.00000000000000000001", null, null)]
		[DataRow("+42", null, null)]
		[DataRow("1e2", null, null)]
		[DataRow("-", null, null)]
		[DataRow("", null, null)]
		[DataRow("--1", null, null)]
		[DataRow(" 42", null, null)]
		[DataRow("42 ", null, null)]
		[DataRow("NaN", null, null)]
		[DataRow("\u221e", null, null)]
		[DataRow("9223372036854775807", long.MaxValue, 9223372036854775807UL)]
		[DataRow("9223372036854775808", null, 9223372036854775808UL)]
		[DataRow("-9223372036854775808", long.MinValue, null)]
		[DataRow("-9223372036854775809", null, null)]
		[DataRow("18446744073709551615", null, ulong.MaxValue)]
		[DataRow("18446744073709551616", null, null)]
		public void When_ParseIntegral_Then_MatchesWinRT(string text, long? expectedInt, ulong? expectedUInt)
		{
			var sut = new DecimalFormatter(new[] { "en-US" }, "US")
			{
				IsGrouped = true,
				IntegerDigits = 1,
				FractionDigits = 2,
			};

			Assert.AreEqual(expectedInt, sut.ParseInt(text), $"ParseInt(\"{text}\")");
			Assert.AreEqual(expectedUInt, sut.ParseUInt(text), $"ParseUInt(\"{text}\")");
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
		public void When_ParseIntegralIsGrouped_Then_GroupSizesAreValidated()
		{
			var sut = new DecimalFormatter(new[] { "en-US" }, "US")
			{
				IsGrouped = true,
				IntegerDigits = 1,
				FractionDigits = 2,
			};

			Assert.AreEqual(1234L, sut.ParseInt("1,234"));
			Assert.AreEqual(1234UL, sut.ParseUInt("1,234"));
			Assert.AreEqual(-1234L, sut.ParseInt("-1,234"));
			Assert.IsNull(sut.ParseUInt("-1,234"));
			Assert.AreEqual(1L, sut.ParseInt("0,001"));
			Assert.IsNull(sut.ParseInt("12,34"));
			Assert.IsNull(sut.ParseInt("1,2,3"));
			Assert.IsNull(sut.ParseInt("1,234.99"));

			var french = new DecimalFormatter(new[] { "fr-FR" }, "FR")
			{
				IsGrouped = true,
				IntegerDigits = 1,
				FractionDigits = 2,
			};

			// Which space character groups a locale depends on the locale data revision, so it is read
			// back from the formatter rather than pinned.
			var separator = french.FormatInt(1234)[1];
			var foreign = separator == '\u202f' ? '\u00a0' : '\u202f';

			Assert.AreEqual(1234L, french.ParseInt($"1{separator}234"));
			Assert.IsNull(french.ParseInt($"1{foreign}234"));
		}
	}
}
