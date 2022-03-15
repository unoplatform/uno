using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Globalization.NumberFormatting;

namespace Uno.UI.Tests.Windows_Globalization
{
	[TestClass]
	public class Given_CurrencyFormatter
	{
		private readonly string _currencyCode = "USD";

		[DataTestMethod]
		[DataRow(double.PositiveInfinity, "∞")]
		[DataRow(double.NegativeInfinity, "-∞")]
		[DataRow(double.NaN, "NaN")]
		public void When_FormatSpecialDouble(double value, string expected)
		{
			var sut = new CurrencyFormatter(_currencyCode);
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
			var sut = new CurrencyFormatter(_currencyCode);
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
			var sut = new CurrencyFormatter(_currencyCode);
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
			var sut = new CurrencyFormatter(_currencyCode);
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
			var sut = new CurrencyFormatter(_currencyCode);
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
			var sut = new CurrencyFormatter(_currencyCode);
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
			var sut = new CurrencyFormatter(_currencyCode);
			sut.SignificantDigits = significantDigits;
			sut.IntegerDigits = integerDigits;
			sut.FractionDigits = fractionDigits;

			var actual = sut.FormatDouble(value);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void When_FormatDoubleUsingIncrementNumberRounder()
		{
			var sut = new CurrencyFormatter(_currencyCode);
			IncrementNumberRounder rounder = new IncrementNumberRounder();
			rounder.Increment = 0.5;
			sut.NumberRounder = rounder;
			var actual = sut.FormatDouble(1.8);

			Assert.AreEqual("$2.00", actual);
		}

		[TestMethod]
		public void When_FormatDoubleUsingSignificantDigitsNumberRounder()
		{
			var sut = new CurrencyFormatter(_currencyCode);
			SignificantDigitsNumberRounder rounder = new SignificantDigitsNumberRounder();
			rounder.SignificantDigits = 1;
			sut.NumberRounder = rounder;
			var actual = sut.FormatDouble(1.8);

			Assert.AreEqual("$2.00", actual);
		}

		[TestMethod]
		public void When_FormatDoubleWithUseCurrencyCodeMode()
		{
			var sut = new CurrencyFormatter(_currencyCode);
			sut.Mode = CurrencyFormatterMode.UseCurrencyCode;
			var actual = sut.FormatDouble(1.8);

			Assert.AreEqual("USD 1.80", actual);
		}

		[TestMethod]
		public void When_FormatDoubleAndApplyFor()
		{
			var sut = new CurrencyFormatter(_currencyCode);
			var algorithm = RoundingAlgorithm.RoundHalfAwayFromZero;
			sut.ApplyRoundingForCurrency(algorithm);
			var actual = sut.FormatDouble(1.8);

			var rounder = sut.NumberRounder as IncrementNumberRounder;

			Assert.AreEqual(algorithm, rounder.RoundingAlgorithm);
			Assert.AreEqual("$1.80", actual);
		}

		[TestMethod]
		public void When_Initialize()
		{
			var sut = new CurrencyFormatter(_currencyCode);

			Assert.AreEqual(0, sut.SignificantDigits);
			Assert.AreEqual(1, sut.IntegerDigits);
			Assert.AreEqual(2, sut.FractionDigits);
			Assert.AreEqual(false, sut.IsGrouped);
			Assert.AreEqual(false, sut.IsZeroSigned);
			Assert.AreEqual(false, sut.IsDecimalPointAlwaysDisplayed);
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


		[DataTestMethod]
		[DataRow("ALL", "Lek1.00")]
		[DataRow("AFN", "؋1.00")]
		[DataRow("ARS", "ARS 1.00")]
		[DataRow("AWG", "ƒ1.00")]
		[DataRow("AUD", "AUD 1.00")]
		[DataRow("AZN", "₼1.00")]
		[DataRow("BSD", "BSD 1.00")]
		[DataRow("BBD", "BBD 1.00")]
		[DataRow("BYN", "Br1.00")]
		[DataRow("BZD", "BZ$1.00")]
		[DataRow("BMD", "BMD 1.00")]
		[DataRow("BOB", "Bs.1.00")]
		[DataRow("BAM", "KM1.00")]
		[DataRow("BWP", "P1.00")]
		[DataRow("BGN", "лв.1.00")]
		[DataRow("BRL", "R$1.00")]
		[DataRow("BND", "BND 1.00")]
		[DataRow("KHR", "៛1.00")]
		[DataRow("CAD", "CAD 1.00")]
		[DataRow("KYD", "KYD 1.00")]
		[DataRow("CLP", "CLP 1")]
		[DataRow("CNY", "¥1.00")]
		[DataRow("COP", "COP 1.00")]
		[DataRow("CRC", "₡1.00")]
		[DataRow("HRK", "kn1.00")]
		[DataRow("CUP", "CUP 1.00")]
		[DataRow("CZK", "Kč1.00")]
		[DataRow("DKK", "kr.1.00")]
		[DataRow("DOP", "RD$1.00")]
		[DataRow("XCD", "EC$1.00")]
		[DataRow("EGP", "ج.م.‏1.00")]
		[DataRow("SVC", "₡1.00")]
		[DataRow("EUR", "€1.00")]
		[DataRow("FKP", "£1.00")]
		[DataRow("FJD", "FJD 1.00")]
		[DataRow("GHS", "GH₵1.00")]
		[DataRow("GIP", "£1.00")]
		[DataRow("GTQ", "Q1.00")]
		[DataRow("GGP", "GGP1.00")]
		[DataRow("GYD", "GYD 1.00")]
		[DataRow("HNL", "L.1.00")]
		[DataRow("HKD", "HKD 1.00")]
		[DataRow("HUF", "Ft1.00")]
		[DataRow("ISK", "kr.1")]
		[DataRow("INR", "₹1.00")]
		[DataRow("IDR", "Rp1.00")]
		[DataRow("IRR", "ريال1.00")]
		[DataRow("IMP", "IMP1.00")]
		[DataRow("ILS", "₪1.00")]
		[DataRow("JMD", "J$1.00")]
		[DataRow("JPY", "¥1")]
		[DataRow("JEP", "JEP1.00")]
		[DataRow("KZT", "₸1.00")]
		[DataRow("KPW", "₩1.00")]
		[DataRow("KRW", "₩1")]
		[DataRow("KGS", "сом1.00")]
		[DataRow("LAK", "₭1.00")]
		[DataRow("LBP", "ل.ل.‏1.00")]
		[DataRow("LRD", "LRD 1.00")]
		[DataRow("MKD", "ден.1.00")]
		[DataRow("MYR", "RM1.00")]
		[DataRow("MUR", "₨1.00")]
		[DataRow("MXN", "MXN 1.00")]
		[DataRow("MNT", "₮1.00")]
		[DataRow("MZN", "MT1.00")]
		[DataRow("NAD", "NAD 1.00")]
		[DataRow("NPR", "रु1.00")]
		[DataRow("ANG", "NAƒ1.00")]
		[DataRow("NZD", "NZD 1.00")]
		[DataRow("NIO", "C$1.00")]
		[DataRow("NGN", "₦1.00")]
		[DataRow("NOK", "kr1.00")]
		[DataRow("OMR", "ر.ع.‏1.000")]
		[DataRow("PKR", "Rs1.00")]
		[DataRow("PAB", "B/.1.00")]
		[DataRow("PYG", "₲1")]
		[DataRow("PEN", "S/1.00")]
		[DataRow("PHP", "₱1.00")]
		[DataRow("PLN", "zł1.00")]
		[DataRow("QAR", "ر.ق.‏1.00")]
		[DataRow("RON", "lei1.00")]
		[DataRow("RUB", "₽1.00")]
		[DataRow("SHP", "£1.00")]
		[DataRow("SAR", "ر.س.‏1.00")]
		[DataRow("RSD", "din.1.00")]
		[DataRow("SCR", "SR1.00")]
		[DataRow("SGD", "SGD 1.00")]
		[DataRow("SBD", "SBD 1.00")]
		[DataRow("SOS", "S1.00")]
		[DataRow("ZAR", "R1.00")]
		[DataRow("LKR", "Rs1.00")]
		[DataRow("SEK", "kr1.00")]
		[DataRow("CHF", "CHF1.00")]
		[DataRow("SRD", "SRD 1.00")]
		[DataRow("SYP", "ل.س.‏1.00")]
		[DataRow("TWD", "NT$1.00")]
		[DataRow("THB", "฿1.00")]
		[DataRow("TTD", "TT$1.00")]
		[DataRow("TRY", "₺1.00")]
		[DataRow("TVD", "TVD1.00")]
		[DataRow("UAH", "₴1.00")]
		[DataRow("AED", "د.إ.‏1.00")]
		[DataRow("GBP", "£1.00")]
		[DataRow("USD", "$1.00")]
		[DataRow("UYU", "$U1.00")]
		[DataRow("UZS", "so'm1.00")]
		[DataRow("VEF", "Bs.F.1.00")]
		[DataRow("VND", "₫1")]
		[DataRow("YER", "ر.ي.‏1.00")]
		[DataRow("ZWD", "ZWD1.00")]
		public void When_FormatDoubleWithSpecialCurrencyCode(string currencyCode, string expected)
		{
			var sut = new CurrencyFormatter(currencyCode);
			var actual = sut.FormatDouble(1d);
			Assert.AreEqual(expected, actual);
		}

		[DataTestMethod]
		[DataRow("ALL", "ALL 1.00")]
		[DataRow("AFN", "AFN 1.00")]
		[DataRow("ARS", "ARS 1.00")]
		[DataRow("AWG", "AWG 1.00")]
		[DataRow("AUD", "AUD 1.00")]
		[DataRow("AZN", "AZN 1.00")]
		[DataRow("BSD", "BSD 1.00")]
		[DataRow("BBD", "BBD 1.00")]
		[DataRow("BYN", "BYN 1.00")]
		[DataRow("BZD", "BZD 1.00")]
		[DataRow("BMD", "BMD 1.00")]
		[DataRow("BOB", "BOB 1.00")]
		[DataRow("BAM", "BAM 1.00")]
		[DataRow("BWP", "BWP 1.00")]
		[DataRow("BGN", "BGN 1.00")]
		[DataRow("BRL", "BRL 1.00")]
		[DataRow("BND", "BND 1.00")]
		[DataRow("KHR", "KHR 1.00")]
		[DataRow("CAD", "CAD 1.00")]
		[DataRow("KYD", "KYD 1.00")]
		[DataRow("CLP", "CLP 1")]
		[DataRow("CNY", "CNY 1.00")]
		[DataRow("COP", "COP 1.00")]
		[DataRow("CRC", "CRC 1.00")]
		[DataRow("HRK", "HRK 1.00")]
		[DataRow("CUP", "CUP 1.00")]
		[DataRow("CZK", "CZK 1.00")]
		[DataRow("DKK", "DKK 1.00")]
		[DataRow("DOP", "DOP 1.00")]
		[DataRow("XCD", "XCD 1.00")]
		[DataRow("EGP", "EGP 1.00")]
		[DataRow("SVC", "SVC 1.00")]
		[DataRow("EUR", "EUR 1.00")]
		[DataRow("FKP", "FKP 1.00")]
		[DataRow("FJD", "FJD 1.00")]
		[DataRow("GHS", "GHS 1.00")]
		[DataRow("GIP", "GIP 1.00")]
		[DataRow("GTQ", "GTQ 1.00")]
		[DataRow("GGP", "GGP 1.00")]
		[DataRow("GYD", "GYD 1.00")]
		[DataRow("HNL", "HNL 1.00")]
		[DataRow("HKD", "HKD 1.00")]
		[DataRow("HUF", "HUF 1.00")]
		[DataRow("ISK", "ISK 1")]
		[DataRow("INR", "INR 1.00")]
		[DataRow("IDR", "IDR 1.00")]
		[DataRow("IRR", "IRR 1.00")]
		[DataRow("IMP", "IMP 1.00")]
		[DataRow("ILS", "ILS 1.00")]
		[DataRow("JMD", "JMD 1.00")]
		[DataRow("JPY", "JPY 1")]
		[DataRow("JEP", "JEP 1.00")]
		[DataRow("KZT", "KZT 1.00")]
		[DataRow("KPW", "KPW 1.00")]
		[DataRow("KRW", "KRW 1")]
		[DataRow("KGS", "KGS 1.00")]
		[DataRow("LAK", "LAK 1.00")]
		[DataRow("LBP", "LBP 1.00")]
		[DataRow("LRD", "LRD 1.00")]
		[DataRow("MKD", "MKD 1.00")]
		[DataRow("MYR", "MYR 1.00")]
		[DataRow("MUR", "MUR 1.00")]
		[DataRow("MXN", "MXN 1.00")]
		[DataRow("MNT", "MNT 1.00")]
		[DataRow("MNT", "MNT 1.00")]
		[DataRow("MZN", "MZN 1.00")]
		[DataRow("NAD", "NAD 1.00")]
		[DataRow("NPR", "NPR 1.00")]
		[DataRow("ANG", "ANG 1.00")]
		[DataRow("NZD", "NZD 1.00")]
		[DataRow("NIO", "NIO 1.00")]
		[DataRow("NGN", "NGN 1.00")]
		[DataRow("NOK", "NOK 1.00")]
		[DataRow("OMR", "OMR 1.000")]
		[DataRow("PKR", "PKR 1.00")]
		[DataRow("PAB", "PAB 1.00")]
		[DataRow("PYG", "PYG 1")]
		[DataRow("PEN", "PEN 1.00")]
		[DataRow("PHP", "PHP 1.00")]
		[DataRow("PLN", "PLN 1.00")]
		[DataRow("QAR", "QAR 1.00")]
		[DataRow("RON", "RON 1.00")]
		[DataRow("RUB", "RUB 1.00")]
		[DataRow("SHP", "SHP 1.00")]
		[DataRow("SAR", "SAR 1.00")]
		[DataRow("RSD", "RSD 1.00")]
		[DataRow("SCR", "SCR 1.00")]
		[DataRow("SGD", "SGD 1.00")]
		[DataRow("SBD", "SBD 1.00")]
		[DataRow("SOS", "SOS 1.00")]
		[DataRow("KRW", "KRW 1")]
		[DataRow("ZAR", "ZAR 1.00")]
		[DataRow("LKR", "LKR 1.00")]
		[DataRow("SEK", "SEK 1.00")]
		[DataRow("CHF", "CHF 1.00")]
		[DataRow("SRD", "SRD 1.00")]
		[DataRow("SYP", "SYP 1.00")]
		[DataRow("TWD", "TWD 1.00")]
		[DataRow("THB", "THB 1.00")]
		[DataRow("TTD", "TTD 1.00")]
		[DataRow("TRY", "TRY 1.00")]
		[DataRow("TVD", "TVD 1.00")]
		[DataRow("UAH", "UAH 1.00")]
		[DataRow("AED", "AED 1.00")]
		[DataRow("GBP", "GBP 1.00")]
		[DataRow("USD", "USD 1.00")]
		[DataRow("UYU", "UYU 1.00")]
		[DataRow("UZS", "UZS 1.00")]
		[DataRow("VEF", "VEF 1.00")]
		[DataRow("VND", "VND 1")]
		[DataRow("YER", "YER 1.00")]
		[DataRow("ZWD", "ZWD 1.00")]
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
			var sut = new CurrencyFormatter(_currencyCode);
			sut.NumeralSystem = "ArabExt";
			var actual = sut.FormatDouble(1d);
			Assert.AreEqual("$۱٫۰۰", actual);
		}

		[TestMethod]
		public void When_ParseDouble()
		{
			var sut = new CurrencyFormatter(_currencyCode);
			var value = sut.ParseDouble("$1.00");

			Assert.AreEqual(1d, value);
		}

		[TestMethod]
		public void When_ParseDoubleWithCurrencyCodeMode()
		{
			var sut = new CurrencyFormatter(_currencyCode);
			sut.Mode = CurrencyFormatterMode.UseCurrencyCode;
			var value = sut.ParseDouble("USD 1.00");

			Assert.AreEqual(1d, value);
		}

		[TestMethod]
		public void When_ParseDoubleNotValid()
		{
			var sut = new CurrencyFormatter(_currencyCode);
			var value = sut.ParseDouble("1.00");

			Assert.IsNull(value);
		}
	}
}
