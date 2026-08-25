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
	}
}
