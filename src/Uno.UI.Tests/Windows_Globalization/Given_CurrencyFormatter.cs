#nullable enable

using System;
using Windows.Globalization.NumberFormatting;

namespace Uno.UI.Tests.Windows_Globalization
{
	[TestClass]
	public class Given_CurrencyFormatter
	{
		private const string USDCurrencyCode = "USD";

		[ClassInitialize]
		public static void ClassInit(TestContext context)
		{
			Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "en-us";
		}

		[DataTestMethod]
		[DataRow(double.PositiveInfinity, "∞")]
		[DataRow(double.NegativeInfinity, "-∞")]
		[DataRow(double.NaN, "NaN")]
		public void When_FormatSpecialDouble(double value, string expected)
		{
			var sut = new CurrencyFormatter(USDCurrencyCode);
			var actual = sut.FormatDouble(value);

			Assert.AreEqual(expected, actual);
		}

		[DataTestMethod]
		[DataRow(1.5d, 1, 2, "$1.50")]
		[DataRow(1.567d, 1, 2, "$1.567")]
		[DataRow(1.5602d, 1, 2, "$1.5602")]
		[DataRow(-1.5602d, 1, 2, "($1.5602)")]
		[DataRow(0d, 0, 0, "$0")]
		[DataRow(-0d, 0, 0, "$0")]
		[DataRow(0d, 0, 2, "$.00")]
		[DataRow(-0d, 0, 2, "$.00")]
		[DataRow(0d, 2, 0, "$00")]
		[DataRow(-0d, 2, 0, "$00")]
		[DataRow(0d, 3, 1, "$000.0")]
		[DataRow(-0d, 3, 1, "$000.0")]
		public void When_FormatDouble(double value, int integerDigits, int fractionDigits, string expected)
		{
			var sut = new CurrencyFormatter(USDCurrencyCode);
			sut.IntegerDigits = integerDigits;
			sut.FractionDigits = fractionDigits;

			var actual = sut.FormatDouble(value);
			Assert.AreEqual(expected, actual);
		}

		[DataTestMethod]
		[DataRow(1234, 2, 0, "$1,234")]
		[DataRow(1234, 6, 0, "$001,234")]
		[DataRow(1234.56, 2, 2, "$1,234.56")]
		[DataRow(1234.0, 6, 2, "$001,234.00")]
		[DataRow(1234.0, 6, 0, "$001,234")]
		public void When_FormatDoubleWithIsGroupSetTrue(double value, int integerDigits, int fractionDigits, string expected)
		{
			var sut = new CurrencyFormatter(USDCurrencyCode);
			sut.IntegerDigits = integerDigits;
			sut.FractionDigits = fractionDigits;
			sut.IsGrouped = true;

			var actual = sut.FormatDouble(value);
			Assert.AreEqual(expected, actual);
		}

		[DataTestMethod]
		[DataRow(0, 0, "($0)")]
		[DataRow(0, 2, "($.00)")]
		[DataRow(2, 0, "($00)")]
		[DataRow(3, 1, "($000.0)")]
		public void When_FormatDoubleMinusZeroWithIsZeroSignedSetTrue(int integerDigits, int fractionDigits, string expected)
		{
			var sut = new CurrencyFormatter(USDCurrencyCode);
			sut.IntegerDigits = integerDigits;
			sut.FractionDigits = fractionDigits;
			sut.IsZeroSigned = true;

			var actual = sut.FormatDouble(-0d);
			Assert.AreEqual(expected, actual);
		}

		[DataTestMethod]
		[DataRow(0, 0, "$0")]
		[DataRow(0, 2, "$.00")]
		[DataRow(2, 0, "$00")]
		[DataRow(3, 1, "$000.0")]
		public void When_FormatDoubleZeroWithIsZeroSignedSetTrue(int integerDigits, int fractionDigits, string expected)
		{
			var sut = new CurrencyFormatter(USDCurrencyCode);
			sut.IntegerDigits = integerDigits;
			sut.FractionDigits = fractionDigits;
			sut.IsZeroSigned = true;

			var actual = sut.FormatDouble(0d);
			Assert.AreEqual(expected, actual);
		}

		[DataTestMethod]
		[DataRow(1d, "$1.")]
		public void When_FormatDoubleWithIsDecimalPointerAlwaysDisplayedSetTrue(double value, string expected)
		{
			var sut = new CurrencyFormatter(USDCurrencyCode);
			sut.IsDecimalPointAlwaysDisplayed = true;
			sut.FractionDigits = 0;
			sut.IntegerDigits = 0;

			var actual = sut.FormatDouble(value);
			Assert.AreEqual(expected, actual);
		}

		[DataTestMethod]
		[DataRow(123.4567d, 5, 1, 2, "$123.4567")]
		[DataRow(123.4567d, 10, 1, 2, "$123.4567000")]
		[DataRow(123.4567d, 2, 1, 2, "$123.4567")]
		[DataRow(12.3d, 4, 1, 2, "$12.30")]
		[DataRow(12.3d, 4, 1, 0, "$12.30")]
		public void When_FormatDoubleWithSpecificSignificantDigits(double value, int significantDigits, int integerDigits, int fractionDigits, string expected)
		{
			var sut = new CurrencyFormatter(USDCurrencyCode);
			sut.SignificantDigits = significantDigits;
			sut.IntegerDigits = integerDigits;
			sut.FractionDigits = fractionDigits;

			var actual = sut.FormatDouble(value);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void When_FormatDoubleUsingIncrementNumberRounder()
		{
			var sut = new CurrencyFormatter(USDCurrencyCode);
			IncrementNumberRounder rounder = new IncrementNumberRounder();
			rounder.Increment = 0.5;
			sut.NumberRounder = rounder;
			var actual = sut.FormatDouble(1.8);

			Assert.AreEqual("$2.00", actual);
		}

		[TestMethod]
		public void When_FormatDoubleUsingSignificantDigitsNumberRounder()
		{
			var sut = new CurrencyFormatter(USDCurrencyCode);
			SignificantDigitsNumberRounder rounder = new SignificantDigitsNumberRounder();
			rounder.SignificantDigits = 1;
			sut.NumberRounder = rounder;
			var actual = sut.FormatDouble(1.8);

			Assert.AreEqual("$2.00", actual);
		}

		[TestMethod]
		public void When_FormatDoubleWithUseCurrencyCodeMode()
		{
			var sut = new CurrencyFormatter(USDCurrencyCode);
			sut.Mode = CurrencyFormatterMode.UseCurrencyCode;
			var actual = sut.FormatDouble(1.8);

			Assert.AreEqual("USD 1.80", actual);
		}

		[TestMethod]
		public void When_FormatDoubleAndApplyFor()
		{
			var sut = new CurrencyFormatter(USDCurrencyCode);
			var algorithm = RoundingAlgorithm.RoundHalfAwayFromZero;
			sut.ApplyRoundingForCurrency(algorithm);
			var actual = sut.FormatDouble(1.8);

			if (sut.NumberRounder is not IncrementNumberRounder rounder)
			{
				throw new Exception("NumberRounder should be of type IncrementNumberRounder");
			}

			Assert.AreEqual(algorithm, rounder.RoundingAlgorithm);
			Assert.AreEqual("$1.80", actual);
		}

		[TestMethod]
		public void When_Initialize()
		{
			var sut = new CurrencyFormatter(USDCurrencyCode);

			Assert.AreEqual(0, sut.SignificantDigits);
			Assert.AreEqual(1, sut.IntegerDigits);
			Assert.AreEqual(2, sut.FractionDigits);
			Assert.AreEqual(false, sut.IsGrouped);
			Assert.AreEqual(false, sut.IsZeroSigned);
			Assert.AreEqual(false, sut.IsDecimalPointAlwaysDisplayed);
			Assert.AreEqual("en-US", sut.ResolvedLanguage);
			Assert.IsNull(sut.NumberRounder);
		}


		[DataTestMethod]
		[DataRow("HNL", "L.1.00")]
		[DataRow("AED", "د.إ.‏1.00")]
		[DataRow("AFN", "؋1.00")]
		[DataRow("ALL", "Lek1.00")]
		[DataRow("AMD", "֏1.00")]
		[DataRow("ANG", "NAƒ1.00")]
		[DataRow("AOA", "Kz1.00")]
		[DataRow("ARS", "ARS 1.00")]
		[DataRow("AUD", "AUD 1.00")]
		[DataRow("AWG", "ƒ1.00")]
		[DataRow("AZN", "₼1.00")]
		[DataRow("BAM", "KM1.00")]
		[DataRow("BBD", "BBD 1.00")]
		[DataRow("BDT", "৳1.00")]
		[DataRow("BGN", "лв.1.00")]
		[DataRow("BHD", "د.ب.‏1.000")]
		[DataRow("BIF", "FBu1")]
		[DataRow("BMD", "BMD 1.00")]
		[DataRow("BND", "BND 1.00")]
		[DataRow("BOB", "Bs.1.00")]
		[DataRow("BRL", "R$1.00")]
		[DataRow("BSD", "BSD 1.00")]
		[DataRow("BTN", "Nu.1.00")]
		[DataRow("BWP", "P1.00")]
		[DataRow("BYR", "Br1")]
		[DataRow("BZD", "BZ$1.00")]
		[DataRow("CAD", "CAD 1.00")]
		[DataRow("CDF", "FC1.00")]
		[DataRow("CHF", "CHF1.00")]
		[DataRow("CLP", "CLP 1")]
		[DataRow("CNY", "¥1.00")]
		[DataRow("COP", "COP 1.00")]
		[DataRow("CRC", "₡1.00")]
		[DataRow("CUP", "CUP 1.00")]
		[DataRow("CVE", "CVE 1.00")]
		[DataRow("CZK", "Kč1.00")]
		[DataRow("DJF", "Fdj1")]
		[DataRow("DKK", "kr.1.00")]
		[DataRow("DOP", "RD$1.00")]
		[DataRow("DZD", "DA1.00")]
		[DataRow("EGP", "ج.م.‏1.00")]
		[DataRow("ERN", "Nfk1.00")]
		[DataRow("ETB", "Br1.00")]
		[DataRow("EUR", "€1.00")]
		[DataRow("FJD", "FJD 1.00")]
		[DataRow("FKP", "£1.00")]
		[DataRow("GBP", "£1.00")]
		[DataRow("GEL", "₾1.00")]
		[DataRow("GHS", "GH₵1.00")]
		[DataRow("GIP", "£1.00")]
		[DataRow("GMD", "D1.00")]
		[DataRow("GNF", "FG1")]
		[DataRow("GTQ", "Q1.00")]
		[DataRow("GYD", "GYD 1.00")]
		[DataRow("HKD", "HKD 1.00")]
		[DataRow("RON", "lei1.00")]
		[DataRow("HRK", "kn1.00")]
		[DataRow("HTG", "G1.00")]
		[DataRow("HUF", "Ft1.00")]
		[DataRow("IDR", "Rp1.00")]
		[DataRow("ILS", "₪1.00")]
		[DataRow("INR", "₹1.00")]
		[DataRow("IQD", "د.ع.‏1.000")]
		[DataRow("IRR", "ريال1.00")]
		[DataRow("ISK", "kr.1")]
		[DataRow("JMD", "J$1.00")]
		[DataRow("JOD", "د.ا.‏1.000")]
		[DataRow("JPY", "¥1")]
		[DataRow("KES", "KSh1.00")]
		[DataRow("KGS", "сом1.00")]
		[DataRow("KHR", "៛1.00")]
		[DataRow("KMF", "CF1")]
		[DataRow("KPW", "₩1.00")]
		[DataRow("KRW", "₩1")]
		[DataRow("KWD", "د.ك.‏1.000")]
		[DataRow("KYD", "KYD 1.00")]
		[DataRow("KZT", "₸1.00")]
		[DataRow("LAK", "₭1.00")]
		[DataRow("LBP", "ل.ل.‏1.00")]
		[DataRow("LKR", "Rs1.00")]
		[DataRow("LRD", "LRD 1.00")]
		[DataRow("LSL", "L1.00")]
		[DataRow("LTL", "Lt1.00")]
		[DataRow("LVL", "Ls1.00")]
		[DataRow("LYD", "د.ل.‏1.000")]
		[DataRow("MAD", "DH1.00")]
		[DataRow("MDL", "L1.00")]
		[DataRow("MGA", "Ar1.00")]
		[DataRow("MKD", "ден.1.00")]
		[DataRow("MMK", "K1.00")]
		[DataRow("MNT", "₮1.00")]
		[DataRow("MOP", "MOP$1.00")]
		[DataRow("MRO", "UM1.00")]
		[DataRow("MUR", "₨1.00")]
		[DataRow("MVR", "ރ.1.00")]
		[DataRow("MWK", "MK1.00")]
		[DataRow("MXN", "MXN 1.00")]
		[DataRow("MYR", "RM1.00")]
		[DataRow("MZN", "MT1.00")]
		[DataRow("NAD", "NAD 1.00")]
		[DataRow("NGN", "₦1.00")]
		[DataRow("NIO", "C$1.00")]
		[DataRow("NOK", "kr1.00")]
		[DataRow("NPR", "रु1.00")]
		[DataRow("NZD", "NZD 1.00")]
		[DataRow("OMR", "ر.ع.‏1.000")]
		[DataRow("PAB", "B/.1.00")]
		[DataRow("PEN", "S/1.00")]
		[DataRow("PGK", "K1.00")]
		[DataRow("PHP", "₱1.00")]
		[DataRow("PKR", "Rs1.00")]
		[DataRow("PLN", "zł1.00")]
		[DataRow("PYG", "₲1")]
		[DataRow("QAR", "ر.ق.‏1.00")]
		[DataRow("RSD", "din.1.00")]
		[DataRow("RUB", "₽1.00")]
		[DataRow("RWF", "RF1")]
		[DataRow("SAR", "ر.س.‏1.00")]
		[DataRow("SBD", "SBD 1.00")]
		[DataRow("SCR", "SR1.00")]
		[DataRow("SDG", "£1.00")]
		[DataRow("SEK", "kr1.00")]
		[DataRow("SGD", "SGD 1.00")]
		[DataRow("SHP", "£1.00")]
		[DataRow("SLL", "Le1.00")]
		[DataRow("SOS", "S1.00")]
		[DataRow("SRD", "SRD 1.00")]
		[DataRow("STD", "Db1.00")]
		[DataRow("SYP", "ل.س.‏1.00")]
		[DataRow("SZL", "L1.00")]
		[DataRow("THB", "฿1.00")]
		[DataRow("TJS", "смн1.00")]
		[DataRow("TMT", "m.1.00")]
		[DataRow("TND", "د.ت.‏1.000")]
		[DataRow("TOP", "T$1.00")]
		[DataRow("TRY", "₺1.00")]
		[DataRow("TTD", "TT$1.00")]
		[DataRow("TWD", "NT$1.00")]
		[DataRow("TZS", "TSh1.00")]
		[DataRow("UAH", "₴1.00")]
		[DataRow("UGX", "USh1")]
		[DataRow("USD", "$1.00")]
		[DataRow("UYU", "$U1.00")]
		[DataRow("UZS", "so'm1.00")]
		[DataRow("VEF", "Bs.F.1.00")]
		[DataRow("VND", "₫1")]
		[DataRow("VUV", "VT1")]
		[DataRow("WST", "WST 1.00")]
		[DataRow("XAF", "FCFA1")]
		[DataRow("XCD", "EC$1.00")]
		[DataRow("XOF", "CFA1")]
		[DataRow("XPF", "F1")]
		[DataRow("XXX", "1")]
		[DataRow("YER", "ر.ي.‏1.00")]
		[DataRow("ZAR", "R1.00")]
		[DataRow("ZMW", "K1.00")]
		[DataRow("ZWL", "Z$1.00")]
		[DataRow("BYN", "Br1.00")]
		[DataRow("SSP", "£1.00")]
		[DataRow("STN", "Db1.00")]
		[DataRow("VES", "Bs.S1.00")]
		[DataRow("MRU", "UM1.00")]
		public void When_FormatDoubleWithSpecialCurrencyCode(string currencyCode, string expected)
		{
			var sut = new CurrencyFormatter(currencyCode);
			var actual = sut.FormatDouble(1d);
			Assert.AreEqual(expected, actual);
		}

		[DataTestMethod]
		[DataRow("HNL", "HNL 1.00")]
		[DataRow("AED", "AED 1.00")]
		[DataRow("AFN", "AFN 1.00")]
		[DataRow("ALL", "ALL 1.00")]
		[DataRow("AMD", "AMD 1.00")]
		[DataRow("ANG", "ANG 1.00")]
		[DataRow("AOA", "AOA 1.00")]
		[DataRow("ARS", "ARS 1.00")]
		[DataRow("AUD", "AUD 1.00")]
		[DataRow("AWG", "AWG 1.00")]
		[DataRow("AZN", "AZN 1.00")]
		[DataRow("BAM", "BAM 1.00")]
		[DataRow("BBD", "BBD 1.00")]
		[DataRow("BDT", "BDT 1.00")]
		[DataRow("BGN", "BGN 1.00")]
		[DataRow("BHD", "BHD 1.000")]
		[DataRow("BIF", "BIF 1")]
		[DataRow("BMD", "BMD 1.00")]
		[DataRow("BND", "BND 1.00")]
		[DataRow("BOB", "BOB 1.00")]
		[DataRow("BRL", "BRL 1.00")]
		[DataRow("BSD", "BSD 1.00")]
		[DataRow("BTN", "BTN 1.00")]
		[DataRow("BWP", "BWP 1.00")]
		[DataRow("BYR", "BYR 1")]
		[DataRow("BZD", "BZD 1.00")]
		[DataRow("CAD", "CAD 1.00")]
		[DataRow("CDF", "CDF 1.00")]
		[DataRow("CHF", "CHF 1.00")]
		[DataRow("CLP", "CLP 1")]
		[DataRow("CNY", "CNY 1.00")]
		[DataRow("COP", "COP 1.00")]
		[DataRow("CRC", "CRC 1.00")]
		[DataRow("CUP", "CUP 1.00")]
		[DataRow("CVE", "CVE 1.00")]
		[DataRow("CZK", "CZK 1.00")]
		[DataRow("DJF", "DJF 1")]
		[DataRow("DKK", "DKK 1.00")]
		[DataRow("DOP", "DOP 1.00")]
		[DataRow("DZD", "DZD 1.00")]
		[DataRow("EGP", "EGP 1.00")]
		[DataRow("ERN", "ERN 1.00")]
		[DataRow("ETB", "ETB 1.00")]
		[DataRow("EUR", "EUR 1.00")]
		[DataRow("FJD", "FJD 1.00")]
		[DataRow("FKP", "FKP 1.00")]
		[DataRow("GBP", "GBP 1.00")]
		[DataRow("GEL", "GEL 1.00")]
		[DataRow("GHS", "GHS 1.00")]
		[DataRow("GIP", "GIP 1.00")]
		[DataRow("GMD", "GMD 1.00")]
		[DataRow("GNF", "GNF 1")]
		[DataRow("GTQ", "GTQ 1.00")]
		[DataRow("GYD", "GYD 1.00")]
		[DataRow("HKD", "HKD 1.00")]
		[DataRow("RON", "RON 1.00")]
		[DataRow("HRK", "HRK 1.00")]
		[DataRow("HTG", "HTG 1.00")]
		[DataRow("HUF", "HUF 1.00")]
		[DataRow("IDR", "IDR 1.00")]
		[DataRow("ILS", "ILS 1.00")]
		[DataRow("INR", "INR 1.00")]
		[DataRow("IQD", "IQD 1.000")]
		[DataRow("IRR", "IRR 1.00")]
		[DataRow("ISK", "ISK 1")]
		[DataRow("JMD", "JMD 1.00")]
		[DataRow("JOD", "JOD 1.000")]
		[DataRow("JPY", "JPY 1")]
		[DataRow("KES", "KES 1.00")]
		[DataRow("KGS", "KGS 1.00")]
		[DataRow("KHR", "KHR 1.00")]
		[DataRow("KMF", "KMF 1")]
		[DataRow("KPW", "KPW 1.00")]
		[DataRow("KRW", "KRW 1")]
		[DataRow("KWD", "KWD 1.000")]
		[DataRow("KYD", "KYD 1.00")]
		[DataRow("KZT", "KZT 1.00")]
		[DataRow("LAK", "LAK 1.00")]
		[DataRow("LBP", "LBP 1.00")]
		[DataRow("LKR", "LKR 1.00")]
		[DataRow("LRD", "LRD 1.00")]
		[DataRow("LSL", "LSL 1.00")]
		[DataRow("LTL", "LTL 1.00")]
		[DataRow("LVL", "LVL 1.00")]
		[DataRow("LYD", "LYD 1.000")]
		[DataRow("MAD", "MAD 1.00")]
		[DataRow("MDL", "MDL 1.00")]
		[DataRow("MGA", "MGA 1.00")]
		[DataRow("MKD", "MKD 1.00")]
		[DataRow("MMK", "MMK 1.00")]
		[DataRow("MNT", "MNT 1.00")]
		[DataRow("MOP", "MOP 1.00")]
		[DataRow("MRO", "MRO 1.00")]
		[DataRow("MUR", "MUR 1.00")]
		[DataRow("MVR", "MVR 1.00")]
		[DataRow("MWK", "MWK 1.00")]
		[DataRow("MXN", "MXN 1.00")]
		[DataRow("MYR", "MYR 1.00")]
		[DataRow("MZN", "MZN 1.00")]
		[DataRow("NAD", "NAD 1.00")]
		[DataRow("NGN", "NGN 1.00")]
		[DataRow("NIO", "NIO 1.00")]
		[DataRow("NOK", "NOK 1.00")]
		[DataRow("NPR", "NPR 1.00")]
		[DataRow("NZD", "NZD 1.00")]
		[DataRow("OMR", "OMR 1.000")]
		[DataRow("PAB", "PAB 1.00")]
		[DataRow("PEN", "PEN 1.00")]
		[DataRow("PGK", "PGK 1.00")]
		[DataRow("PHP", "PHP 1.00")]
		[DataRow("PKR", "PKR 1.00")]
		[DataRow("PLN", "PLN 1.00")]
		[DataRow("PYG", "PYG 1")]
		[DataRow("QAR", "QAR 1.00")]
		[DataRow("RSD", "RSD 1.00")]
		[DataRow("RUB", "RUB 1.00")]
		[DataRow("RWF", "RWF 1")]
		[DataRow("SAR", "SAR 1.00")]
		[DataRow("SBD", "SBD 1.00")]
		[DataRow("SCR", "SCR 1.00")]
		[DataRow("SDG", "SDG 1.00")]
		[DataRow("SEK", "SEK 1.00")]
		[DataRow("SGD", "SGD 1.00")]
		[DataRow("SHP", "SHP 1.00")]
		[DataRow("SLL", "SLL 1.00")]
		[DataRow("SOS", "SOS 1.00")]
		[DataRow("SRD", "SRD 1.00")]
		[DataRow("STD", "STD 1.00")]
		[DataRow("SYP", "SYP 1.00")]
		[DataRow("SZL", "SZL 1.00")]
		[DataRow("THB", "THB 1.00")]
		[DataRow("TJS", "TJS 1.00")]
		[DataRow("TMT", "TMT 1.00")]
		[DataRow("TND", "TND 1.000")]
		[DataRow("TOP", "TOP 1.00")]
		[DataRow("TRY", "TRY 1.00")]
		[DataRow("TTD", "TTD 1.00")]
		[DataRow("TWD", "TWD 1.00")]
		[DataRow("TZS", "TZS 1.00")]
		[DataRow("UAH", "UAH 1.00")]
		[DataRow("UGX", "UGX 1")]
		[DataRow("USD", "USD 1.00")]
		[DataRow("UYU", "UYU 1.00")]
		[DataRow("UZS", "UZS 1.00")]
		[DataRow("VEF", "VEF 1.00")]
		[DataRow("VND", "VND 1")]
		[DataRow("VUV", "VUV 1")]
		[DataRow("WST", "WST 1.00")]
		[DataRow("XAF", "XAF 1")]
		[DataRow("XCD", "XCD 1.00")]
		[DataRow("XOF", "XOF 1")]
		[DataRow("XPF", "XPF 1")]
		[DataRow("XXX", "XXX 1")]
		[DataRow("YER", "YER 1.00")]
		[DataRow("ZAR", "ZAR 1.00")]
		[DataRow("ZMW", "ZMW 1.00")]
		[DataRow("ZWL", "ZWL 1.00")]
		[DataRow("BYN", "BYN 1.00")]
		[DataRow("SSP", "SSP 1.00")]
		[DataRow("STN", "STN 1.00")]
		[DataRow("VES", "VES 1.00")]
		[DataRow("MRU", "MRU 1.00")]
		public void When_FormatDoubleWithSpecialCurrencyCodeAndCurrencyCodeMode(string currencyCode, string expected)
		{
			var sut = new CurrencyFormatter(currencyCode);
			sut.Mode = CurrencyFormatterMode.UseCurrencyCode;
			var actual = sut.FormatDouble(1d);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void When_FormatDoubleWithSpecialCurrencyCodeAndDifferentNumeralSystem()
		{
			var sut = new CurrencyFormatter(USDCurrencyCode);
			sut.NumeralSystem = "ArabExt";
			var actual = sut.FormatDouble(1d);
			Assert.AreEqual("$۱٫۰۰", actual);
		}

		[TestMethod]
		public void When_ParseDouble()
		{
			var sut = new CurrencyFormatter(USDCurrencyCode);
			var value = sut.ParseDouble("$1.00");

			Assert.AreEqual(1d, value);
		}

		[TestMethod]
		public void When_ParseDoubleWithCurrencyCodeMode()
		{
			var sut = new CurrencyFormatter(USDCurrencyCode);
			sut.Mode = CurrencyFormatterMode.UseCurrencyCode;
			var value = sut.ParseDouble("USD 1.00");

			Assert.AreEqual(1d, value);
		}

		[TestMethod]
		public void When_ParseDoubleNotValid()
		{
			var sut = new CurrencyFormatter(USDCurrencyCode);
			var value = sut.ParseDouble("1.00");

			Assert.IsNull(value);
		}

		[TestMethod]
		public void When_CurrencyCodeIsNotValid()
		{
			try
			{
				_ = new CurrencyFormatter("irr");
			}
			catch (Exception ex)
			{
				Assert.AreEqual("The parameter is incorrect.\r\n\r\ncurrencyCode", ex.Message);
			}
			Assert.ThrowsException<ArgumentException>(() => new CurrencyFormatter("irr"));
		}
	}
}
