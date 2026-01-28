#nullable enable

using System;
using System.Globalization;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Globalization.NumberFormatting;

namespace Uno.UI.RuntimeTests.Tests.Windows_Globalization;

[TestClass]
public class Given_CurrencyFormatter
{
	private const string USDCurrencyCode = "USD";
	private const string USDSymbol = "$";
	private const char NoBreakSpaceChar = '\u00A0';
	private const char OpenPatternSymbol = '(';
	private const char ClosePatternSymbol = ')';

	[TestMethod]
	[DataRow(double.PositiveInfinity, "∞")]
	[DataRow(double.NegativeInfinity, "-∞")]
	[DataRow(double.NaN, "NaN")]
	public void When_FormatSpecialDouble(double value, string text)
	{
		var sut = MakeFormatter(USDCurrencyCode);
		var actual = sut.FormatDouble(value);

		Assert.AreEqual(text, actual);
	}

	[TestMethod]
	[DataRow(1.5d, 1, 2, "1.50")]
	[DataRow(1.567d, 1, 2, "1.567")]
	[DataRow(1.5602d, 1, 2, "1.5602")]
	[DataRow(0d, 0, 0, "0")]
	[DataRow(0d, 0, 2, ".00")]
	[DataRow(0d, 2, 0, "00")]
	[DataRow(0d, 3, 1, "000.0")]
	public void When_FormatDouble(double value, int integerDigits, int fractionDigits, string text)
	{
		var sut = MakeFormatter(USDCurrencyCode);
		sut.IntegerDigits = integerDigits;
		sut.FractionDigits = fractionDigits;

		var actual = sut.FormatDouble(value);
		var expected = FormatSymbolModePositiveNumber(text, USDSymbol);

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	[DataRow(-1.5602d, 1, 2, "1.5602")]
	public void When_FormatNegative(double value, int integerDigits, int fractionDigits, string text)
	{
		var sut = MakeFormatter(USDCurrencyCode);
		sut.IntegerDigits = integerDigits;
		sut.FractionDigits = fractionDigits;

		var actual = sut.FormatDouble(value);
		var expected = FormatSymbolModeNegativeNumber(text, USDSymbol);

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	[DataRow(1234, 2, 0, "1,234")]
	[DataRow(1234, 6, 0, "001,234")]
	[DataRow(1234.56, 2, 2, "1,234.56")]
	[DataRow(1234.0, 6, 2, "001,234.00")]
	[DataRow(1234.0, 6, 0, "001,234")]
	public void When_FormatDoubleWithIsGroupSetTrue(double value, int integerDigits, int fractionDigits, string text)
	{
		var sut = MakeFormatter(USDCurrencyCode);
		sut.IntegerDigits = integerDigits;
		sut.FractionDigits = fractionDigits;
		sut.IsGrouped = true;

		var actual = sut.FormatDouble(value);
		var expected = FormatSymbolModePositiveNumber(text, USDSymbol);

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	[DataRow(0, 0, "0")]
	[DataRow(0, 2, ".00")]
	[DataRow(2, 0, "00")]
	[DataRow(3, 1, "000.0")]
	public void When_FormatDoubleMinusZeroWithIsZeroSignedSetTrue(int integerDigits, int fractionDigits, string text)
	{
		var sut = MakeFormatter(USDCurrencyCode);
		sut.IntegerDigits = integerDigits;
		sut.FractionDigits = fractionDigits;
		sut.IsZeroSigned = true;

		var actual = sut.FormatDouble(-0d);
		var expected = FormatSymbolModeNegativeNumber(text, USDSymbol);
		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	[DataRow(0, 0, "0")]
	[DataRow(0, 2, ".00")]
	[DataRow(2, 0, "00")]
	[DataRow(3, 1, "000.0")]
	public void When_FormatDoubleZeroWithIsZeroSignedSetTrue(int integerDigits, int fractionDigits, string text)
	{
		var sut = MakeFormatter(USDCurrencyCode);
		sut.IntegerDigits = integerDigits;
		sut.FractionDigits = fractionDigits;
		sut.IsZeroSigned = true;

		var actual = sut.FormatDouble(0d);
		var expected = FormatSymbolModePositiveNumber(text, USDSymbol);

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	[DataRow(1d, "1.")]
	public void When_FormatDoubleWithIsDecimalPointerAlwaysDisplayedSetTrue(double value, string text)
	{
		var sut = MakeFormatter(USDCurrencyCode);
		sut.IsDecimalPointAlwaysDisplayed = true;
		sut.FractionDigits = 0;
		sut.IntegerDigits = 0;

		var actual = sut.FormatDouble(value);
		var expected = FormatSymbolModePositiveNumber(text, USDSymbol);

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	[DataRow(123.4567d, 5, 1, 2, "123.4567")]
	[DataRow(123.4567d, 10, 1, 2, "123.4567000")]
	[DataRow(123.4567d, 2, 1, 2, "123.4567")]
	[DataRow(12.3d, 4, 1, 2, "12.30")]
	[DataRow(12.3d, 4, 1, 0, "12.30")]
	public void When_FormatDoubleWithSpecificSignificantDigits(double value, int significantDigits, int integerDigits, int fractionDigits, string text)
	{
		var sut = MakeFormatter(USDCurrencyCode);
		sut.SignificantDigits = significantDigits;
		sut.IntegerDigits = integerDigits;
		sut.FractionDigits = fractionDigits;

		var actual = sut.FormatDouble(value);
		var expected = FormatSymbolModePositiveNumber(text, USDSymbol);

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void When_FormatDoubleUsingIncrementNumberRounder()
	{
		var sut = MakeFormatter(USDCurrencyCode);
		IncrementNumberRounder rounder = new IncrementNumberRounder();
		rounder.Increment = 0.5;
		sut.NumberRounder = rounder;
		var actual = sut.FormatDouble(1.8);
		var expected = FormatSymbolModePositiveNumber("2.00", USDSymbol);

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void When_FormatDoubleUsingSignificantDigitsNumberRounder()
	{
		var sut = MakeFormatter(USDCurrencyCode);
		SignificantDigitsNumberRounder rounder = new SignificantDigitsNumberRounder();
		rounder.SignificantDigits = 1;
		sut.NumberRounder = rounder;
		var actual = sut.FormatDouble(1.8);
		var expected = FormatSymbolModePositiveNumber("2.00", USDSymbol);

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void When_FormatDoubleWithUseCurrencyCodeMode()
	{
		var sut = MakeFormatter(USDCurrencyCode);
		sut.Mode = CurrencyFormatterMode.UseCurrencyCode;
		var actual = sut.FormatDouble(1.8);
		var expected = FormatCurrencyCodeModePositiveNumber("1.80", USDCurrencyCode);

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void When_FormatNegativeDoubleWithUseCurrencyCodeMode()
	{
		var sut = MakeFormatter(USDCurrencyCode);
		sut.Mode = CurrencyFormatterMode.UseCurrencyCode;
		var actual = sut.FormatDouble(-1.8);
		var expected = FormatCurrencyCodeModeNegativeNumber("1.80", USDCurrencyCode);

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void When_FormatDoubleAndApplyFor()
	{
		var sut = MakeFormatter(USDCurrencyCode);
		var algorithm = RoundingAlgorithm.RoundHalfAwayFromZero;
		sut.ApplyRoundingForCurrency(algorithm);
		var actual = sut.FormatDouble(1.8);

		if (sut.NumberRounder is not IncrementNumberRounder rounder)
		{
			throw new Exception("NumberRounder should be of type IncrementNumberRounder");
		}

		var expected = FormatSymbolModePositiveNumber("1.80", USDSymbol);

		Assert.AreEqual(algorithm, rounder.RoundingAlgorithm);
		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void When_Initialize()
	{
		var sut = MakeFormatter(USDCurrencyCode);

		Assert.AreEqual(0, sut.SignificantDigits);
		Assert.AreEqual(1, sut.IntegerDigits);
		Assert.AreEqual(2, sut.FractionDigits);
		Assert.IsFalse(sut.IsGrouped);
		Assert.IsFalse(sut.IsZeroSigned);
		Assert.IsFalse(sut.IsDecimalPointAlwaysDisplayed);
		Assert.IsNull(sut.NumberRounder);
	}

	[TestMethod]
	// Values updated to match WinUI actual behavior (en-US locale)
	// Symbols with \u00A0 (NBSP) are currencies where WinUI adds a space after the code
	[DataRow("HNL", "1.00", "L")]
	[DataRow("AED", "1.00", "AED")]
	[DataRow("AFN", "1", "؋")]
	[DataRow("ALL", "1", "ALL")]
	[DataRow("AMD", "1.00", "֏")]
	[DataRow("ANG", "1.00", "ANG")]
	[DataRow("AOA", "1.00", "Kz")]
	[DataRow("ARS", "1.00", "ARS\u00A0")]
	[DataRow("AUD", "1.00", "AUD\u00A0")]
	[DataRow("AWG", "1.00", "AWG")]
	[DataRow("AZN", "1.00", "₼")]
	[DataRow("BAM", "1.00", "KM")]
	[DataRow("BBD", "1.00", "BBD\u00A0")]
	[DataRow("BDT", "1.00", "৳")]
	[DataRow("BGN", "1.00", "BGN")]
	[DataRow("BHD", "1.000", "BHD")]
	[DataRow("BIF", "1", "BIF")]
	[DataRow("BMD", "1.00", "BMD\u00A0")]
	[DataRow("BND", "1.00", "BND\u00A0")]
	[DataRow("BOB", "1.00", "Bs")]
	[DataRow("BRL", "1.00", "R$")]
	[DataRow("BSD", "1.00", "BSD\u00A0")]
	[DataRow("BTN", "1.00", "BTN")]
	[DataRow("BWP", "1.00", "P")]
	[DataRow("BYR", "1", "BYR")]
	[DataRow("BZD", "1.00", "BZD\u00A0")]
	[DataRow("CAD", "1.00", "CAD\u00A0")]
	[DataRow("CDF", "1.00", "CDF")]
	[DataRow("CHF", "1.00", "CHF")]
	[DataRow("CLP", "1", "CLP\u00A0")]
	[DataRow("CNY", "1.00", "¥")]
	[DataRow("COP", "1.00", "COP\u00A0")]
	[DataRow("CRC", "1.00", "₡")]
	[DataRow("CUP", "1.00", "CUP\u00A0")]
	[DataRow("CVE", "1.00", "CVE")]
	[DataRow("CZK", "1.00", "Kč")]
	[DataRow("DJF", "1", "DJF")]
	[DataRow("DKK", "1.00", "kr")]
	[DataRow("DOP", "1.00", "DOP\u00A0")]
	[DataRow("DZD", "1.00", "DZD")]
	[DataRow("EGP", "1.00", "E£")]
	[DataRow("ERN", "1.00", "ERN")]
	[DataRow("ETB", "1.00", "ETB")]
	[DataRow("EUR", "1.00", "€")]
	[DataRow("FJD", "1.00", "FJD\u00A0")]
	[DataRow("FKP", "1.00", "£")]
	[DataRow("GBP", "1.00", "£")]
	[DataRow("GEL", "1.00", "₾")]
	[DataRow("GHS", "1.00", "GH₵")]
	[DataRow("GIP", "1.00", "£")]
	[DataRow("GMD", "1.00", "GMD")]
	[DataRow("GNF", "1", "FG")]
	[DataRow("GTQ", "1.00", "Q")]
	[DataRow("GYD", "1.00", "GYD\u00A0")]
	[DataRow("HKD", "1.00", "HKD\u00A0")]
	[DataRow("RON", "1.00", "lei")]
	[DataRow("HRK", "1.00", "kn")]
	[DataRow("HTG", "1.00", "HTG")]
	[DataRow("HUF", "1.00", "Ft")]
	[DataRow("IDR", "1.00", "Rp")]
	[DataRow("ILS", "1.00", "₪")]
	[DataRow("INR", "1.00", "₹")]
	[DataRow("IQD", "1", "IQD")]
	[DataRow("IRR", "1", "IRR")]
	[DataRow("ISK", "1", "kr")]
	[DataRow("JMD", "1.00", "JMD\u00A0")]
	[DataRow("JOD", "1.000", "JOD")]
	[DataRow("JPY", "1", "¥")]
	[DataRow("KES", "1.00", "KES")]
	[DataRow("KGS", "1.00", "KGS")]
	[DataRow("KHR", "1.00", "៛")]
	[DataRow("KMF", "1", "CF")]
	[DataRow("KPW", "1", "₩")]
	[DataRow("KRW", "1", "₩")]
	[DataRow("KWD", "1.000", "KWD")]
	[DataRow("KYD", "1.00", "KYD\u00A0")]
	[DataRow("KZT", "1.00", "₸")]
	[DataRow("LAK", "1", "₭")]
	[DataRow("LBP", "1", "L£")]
	[DataRow("LKR", "1.00", "Rs")]
	[DataRow("LRD", "1.00", "LRD\u00A0")]
	[DataRow("LSL", "1.00", "LSL")]
	[DataRow("LTL", "1.00", "Lt")]
	[DataRow("LVL", "1.00", "Ls")]
	[DataRow("LYD", "1.000", "LYD")]
	[DataRow("MAD", "1.00", "MAD")]
	[DataRow("MDL", "1.00", "MDL")]
	[DataRow("MGA", "1", "Ar")]
	[DataRow("MKD", "1.00", "MKD")]
	[DataRow("MMK", "1", "K")]
	[DataRow("MNT", "1.00", "₮")]
	[DataRow("MOP", "1.00", "MOP")]
	[DataRow("MRO", "1", "MRO")]
	[DataRow("MUR", "1.00", "Rs")]
	[DataRow("MVR", "1.00", "MVR")]
	[DataRow("MWK", "1.00", "MWK")]
	[DataRow("MXN", "1.00", "MXN\u00A0")]
	[DataRow("MYR", "1.00", "RM")]
	[DataRow("MZN", "1.00", "MZN")]
	[DataRow("NAD", "1.00", "NAD\u00A0")]
	[DataRow("NGN", "1.00", "₦")]
	[DataRow("NIO", "1.00", "C$")]
	[DataRow("NOK", "1.00", "kr")]
	[DataRow("NPR", "1.00", "Rs")]
	[DataRow("NZD", "1.00", "NZD\u00A0")]
	[DataRow("OMR", "1.000", "OMR")]
	[DataRow("PAB", "1.00", "PAB")]
	[DataRow("PEN", "1.00", "PEN")]
	[DataRow("PGK", "1.00", "PGK")]
	[DataRow("PHP", "1.00", "₱")]
	[DataRow("PKR", "1.00", "Rs")]
	[DataRow("PLN", "1.00", "zł")]
	[DataRow("PYG", "1", "₲")]
	[DataRow("QAR", "1.00", "QAR")]
	[DataRow("RSD", "1", "RSD")]
	[DataRow("RUB", "1.00", "₽")]
	[DataRow("RWF", "1", "RF")]
	[DataRow("SAR", "1.00", "SAR")]
	[DataRow("SBD", "1.00", "SBD\u00A0")]
	[DataRow("SCR", "1.00", "SCR")]
	[DataRow("SDG", "1.00", "SDG")]
	[DataRow("SEK", "1.00", "kr")]
	[DataRow("SGD", "1.00", "SGD\u00A0")]
	[DataRow("SHP", "1.00", "£")]
	[DataRow("SLL", "1", "SLL")]
	[DataRow("SOS", "1", "SOS")]
	[DataRow("SRD", "1.00", "SRD\u00A0")]
	[DataRow("STD", "1", "STD")]
	[DataRow("SYP", "1", "£")]
	[DataRow("SZL", "1.00", "SZL")]
	[DataRow("THB", "1.00", "฿")]
	[DataRow("TJS", "1.00", "TJS")]
	[DataRow("TMT", "1.00", "TMT")]
	[DataRow("TND", "1.000", "TND")]
	[DataRow("TOP", "1.00", "T$")]
	[DataRow("TRY", "1.00", "₺")]
	[DataRow("TTD", "1.00", "TTD\u00A0")]
	[DataRow("TWD", "1.00", "TWD\u00A0")]
	[DataRow("TZS", "1.00", "TZS")]
	[DataRow("UAH", "1.00", "₴")]
	[DataRow("UGX", "1", "UGX")]
	[DataRow("USD", "1.00", "$")]
	[DataRow("UYU", "1.00", "UYU\u00A0")]
	[DataRow("UZS", "1.00", "UZS")]
	[DataRow("VEF", "1.00", "Bs")]
	[DataRow("VND", "1", "₫")]
	[DataRow("VUV", "1", "VUV")]
	[DataRow("WST", "1.00", "WST")]
	[DataRow("XAF", "1", "FCFA")]
	[DataRow("XCD", "1.00", "XCD\u00A0")]
	[DataRow("XOF", "1", "F\u202FCFA")]
	[DataRow("XPF", "1", "CFPF")]
	[DataRow("XXX", "1.00", "¤")]
	[DataRow("YER", "1", "YER")]
	[DataRow("ZAR", "1.00", "R")]
	[DataRow("ZMW", "1.00", "ZK")]
	[DataRow("ZWL", "1.00", "ZWL")]
	[DataRow("BYN", "1.00", "BYN")]
	[DataRow("SSP", "1.00", "£")]
	[DataRow("STN", "1.00", "Db")]
	[DataRow("VES", "1.00", "VES")]
	[DataRow("MRU", "1.00", "MRU")]
	public void When_FormatDoubleWithSpecialCurrencyCode(string currencyCode, string text, string symbol)
	{
		var sut = MakeFormatter(currencyCode);
		sut.Mode = CurrencyFormatterMode.UseSymbol;
		var actual = sut.FormatDouble(1d);

		// In UseSymbol mode, WinUI always uses symbol formatting (no space between symbol and number)
		// even when the symbol equals the currency code
		var expected = FormatSymbolModePositiveNumber(text, symbol);

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	// Values updated to match WinUI actual behavior (decimal places)
	[DataRow("HNL", "1.00")]
	[DataRow("AED", "1.00")]
	[DataRow("AFN", "1")]
	[DataRow("ALL", "1")]
	[DataRow("AMD", "1.00")]
	[DataRow("ANG", "1.00")]
	[DataRow("AOA", "1.00")]
	[DataRow("ARS", "1.00")]
	[DataRow("AUD", "1.00")]
	[DataRow("AWG", "1.00")]
	[DataRow("AZN", "1.00")]
	[DataRow("BAM", "1.00")]
	[DataRow("BBD", "1.00")]
	[DataRow("BDT", "1.00")]
	[DataRow("BGN", "1.00")]
	[DataRow("BHD", "1.000")]
	[DataRow("BIF", "1")]
	[DataRow("BMD", "1.00")]
	[DataRow("BND", "1.00")]
	[DataRow("BOB", "1.00")]
	[DataRow("BRL", "1.00")]
	[DataRow("BSD", "1.00")]
	[DataRow("BTN", "1.00")]
	[DataRow("BWP", "1.00")]
	[DataRow("BYR", "1")]
	[DataRow("BZD", "1.00")]
	[DataRow("CAD", "1.00")]
	[DataRow("CDF", "1.00")]
	[DataRow("CHF", "1.00")]
	[DataRow("CLP", "1")]
	[DataRow("CNY", "1.00")]
	[DataRow("COP", "1.00")]
	[DataRow("CRC", "1.00")]
	[DataRow("CUP", "1.00")]
	[DataRow("CVE", "1.00")]
	[DataRow("CZK", "1.00")]
	[DataRow("DJF", "1")]
	[DataRow("DKK", "1.00")]
	[DataRow("DOP", "1.00")]
	[DataRow("DZD", "1.00")]
	[DataRow("EGP", "1.00")]
	[DataRow("ERN", "1.00")]
	[DataRow("ETB", "1.00")]
	[DataRow("EUR", "1.00")]
	[DataRow("FJD", "1.00")]
	[DataRow("FKP", "1.00")]
	[DataRow("GBP", "1.00")]
	[DataRow("GEL", "1.00")]
	[DataRow("GHS", "1.00")]
	[DataRow("GIP", "1.00")]
	[DataRow("GMD", "1.00")]
	[DataRow("GNF", "1")]
	[DataRow("GTQ", "1.00")]
	[DataRow("GYD", "1.00")]
	[DataRow("HKD", "1.00")]
	[DataRow("RON", "1.00")]
	[DataRow("HRK", "1.00")]
	[DataRow("HTG", "1.00")]
	[DataRow("HUF", "1.00")]
	[DataRow("IDR", "1.00")]
	[DataRow("ILS", "1.00")]
	[DataRow("INR", "1.00")]
	[DataRow("IQD", "1")]
	[DataRow("IRR", "1")]
	[DataRow("ISK", "1")]
	[DataRow("JMD", "1.00")]
	[DataRow("JOD", "1.000")]
	[DataRow("JPY", "1")]
	[DataRow("KES", "1.00")]
	[DataRow("KGS", "1.00")]
	[DataRow("KHR", "1.00")]
	[DataRow("KMF", "1")]
	[DataRow("KPW", "1")]
	[DataRow("KRW", "1")]
	[DataRow("KWD", "1.000")]
	[DataRow("KYD", "1.00")]
	[DataRow("KZT", "1.00")]
	[DataRow("LAK", "1")]
	[DataRow("LBP", "1")]
	[DataRow("LKR", "1.00")]
	[DataRow("LRD", "1.00")]
	[DataRow("LSL", "1.00")]
	[DataRow("LTL", "1.00")]
	[DataRow("LVL", "1.00")]
	[DataRow("LYD", "1.000")]
	[DataRow("MAD", "1.00")]
	[DataRow("MDL", "1.00")]
	[DataRow("MGA", "1")]
	[DataRow("MKD", "1.00")]
	[DataRow("MMK", "1")]
	[DataRow("MNT", "1.00")]
	[DataRow("MOP", "1.00")]
	[DataRow("MRO", "1")]
	[DataRow("MUR", "1.00")]
	[DataRow("MVR", "1.00")]
	[DataRow("MWK", "1.00")]
	[DataRow("MXN", "1.00")]
	[DataRow("MYR", "1.00")]
	[DataRow("MZN", "1.00")]
	[DataRow("NAD", "1.00")]
	[DataRow("NGN", "1.00")]
	[DataRow("NIO", "1.00")]
	[DataRow("NOK", "1.00")]
	[DataRow("NPR", "1.00")]
	[DataRow("NZD", "1.00")]
	[DataRow("OMR", "1.000")]
	[DataRow("PAB", "1.00")]
	[DataRow("PEN", "1.00")]
	[DataRow("PGK", "1.00")]
	[DataRow("PHP", "1.00")]
	[DataRow("PKR", "1.00")]
	[DataRow("PLN", "1.00")]
	[DataRow("PYG", "1")]
	[DataRow("QAR", "1.00")]
	[DataRow("RSD", "1")]
	[DataRow("RUB", "1.00")]
	[DataRow("RWF", "1")]
	[DataRow("SAR", "1.00")]
	[DataRow("SBD", "1.00")]
	[DataRow("SCR", "1.00")]
	[DataRow("SDG", "1.00")]
	[DataRow("SEK", "1.00")]
	[DataRow("SGD", "1.00")]
	[DataRow("SHP", "1.00")]
	[DataRow("SLL", "1")]
	[DataRow("SOS", "1")]
	[DataRow("SRD", "1.00")]
	[DataRow("STD", "1")]
	[DataRow("SYP", "1")]
	[DataRow("SZL", "1.00")]
	[DataRow("THB", "1.00")]
	[DataRow("TJS", "1.00")]
	[DataRow("TMT", "1.00")]
	[DataRow("TND", "1.000")]
	[DataRow("TOP", "1.00")]
	[DataRow("TRY", "1.00")]
	[DataRow("TTD", "1.00")]
	[DataRow("TWD", "1.00")]
	[DataRow("TZS", "1.00")]
	[DataRow("UAH", "1.00")]
	[DataRow("UGX", "1")]
	[DataRow("USD", "1.00")]
	[DataRow("UYU", "1.00")]
	[DataRow("UZS", "1.00")]
	[DataRow("VEF", "1.00")]
	[DataRow("VND", "1")]
	[DataRow("VUV", "1")]
	[DataRow("WST", "1.00")]
	[DataRow("XAF", "1")]
	[DataRow("XCD", "1.00")]
	[DataRow("XOF", "1")]
	[DataRow("XPF", "1")]
	[DataRow("XXX", "1.00")]
	[DataRow("YER", "1")]
	[DataRow("ZAR", "1.00")]
	[DataRow("ZMW", "1.00")]
	[DataRow("ZWL", "1.00")]
	[DataRow("BYN", "1.00")]
	[DataRow("SSP", "1.00")]
	[DataRow("STN", "1.00")]
	[DataRow("VES", "1.00")]
	[DataRow("MRU", "1.00")]
	public void When_FormatDoubleWithSpecialCurrencyCodeAndCurrencyCodeMode(string currencyCode, string text)
	{
		var sut = MakeFormatter(currencyCode);
		sut.Mode = CurrencyFormatterMode.UseCurrencyCode;
		var actual = sut.FormatDouble(1d);
		var expected = FormatCurrencyCodeModePositiveNumber(text, currencyCode);

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void When_FormatDoubleWithSpecialCurrencyCodeAndDifferentNumeralSystem()
	{
		var sut = MakeFormatter(USDCurrencyCode);
		sut.NumeralSystem = "ArabExt";
		var actual = sut.FormatDouble(1d);
		var translator = new NumeralSystemTranslator();
		translator.NumeralSystem = "ArabExt";
		var expected = translator.TranslateNumerals("1.00");
		expected = FormatSymbolModePositiveNumber(expected, USDSymbol);

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void When_ParseDouble()
	{
		var sut = MakeFormatter(USDCurrencyCode);
		var text = FormatSymbolModePositiveNumber("1.00", USDSymbol);
		var value = sut.ParseDouble(text);

		Assert.AreEqual(1d, value);
	}

	[TestMethod]
	public void When_ParseDoubleWithCurrencyCodeMode()
	{
		var sut = MakeFormatter(USDCurrencyCode);
		sut.Mode = CurrencyFormatterMode.UseCurrencyCode;

		var text = FormatCurrencyCodeModePositiveNumber("1.00", USDCurrencyCode);
		var value = sut.ParseDouble(text);

		Assert.AreEqual(1d, value);
	}

	[TestMethod]
	public void When_ParseDoubleNotValid()
	{
		var sut = MakeFormatter(USDCurrencyCode);
		var value = sut.ParseDouble("1.00");

		Assert.IsNull(value);
	}

	[TestMethod]
	public void When_CurrencyCodeIsNotValid()
	{
		Assert.ThrowsExactly<ArgumentException>(() => MakeFormatter("irr"));
	}

	private string FormatCurrencyCodeModeNegativeNumber(string text, string symbol)
	{
		var negativeSign = CultureInfo.CurrentCulture.NumberFormat.NegativeSign;
		var pattern = CultureInfo.CurrentCulture.NumberFormat.CurrencyNegativePattern;
		var spaceSymbol = NoBreakSpaceChar;
		var stringBuilder = new StringBuilder();

		switch (pattern)
		{
			case 0:
			case 14:
				stringBuilder.Append(OpenPatternSymbol);
				stringBuilder.Append(symbol);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(text);
				stringBuilder.Append(ClosePatternSymbol);
				break;
			case 1:
			case 9:
			case 2:
			case 12:
				stringBuilder.Append(symbol);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(text);
				break;
			case 3:
			case 11:
				stringBuilder.Append(symbol);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(text);
				stringBuilder.Append(negativeSign);
				break;
			case 4:
			case 15:
				stringBuilder.Append(OpenPatternSymbol);
				stringBuilder.Append(text);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(symbol);
				stringBuilder.Append(ClosePatternSymbol);
				break;
			case 5:
			case 8:
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(text);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(symbol);
				break;
			case 6:
			case 13:
			case 7:
			case 10:
				stringBuilder.Append(text);
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(symbol);
				break;

			default:
				break;
		}

		return stringBuilder.ToString();
	}

	private string FormatSymbolModeNegativeNumber(string text, string symbol)
	{
		var negativeSign = CultureInfo.CurrentCulture.NumberFormat.NegativeSign;
		var pattern = CultureInfo.CurrentCulture.NumberFormat.CurrencyNegativePattern;
		var stringBuilder = new StringBuilder();
		var spaceSymbol = NoBreakSpaceChar;

		switch (pattern)
		{
			case 0:
				stringBuilder.Append(OpenPatternSymbol);
				stringBuilder.Append(symbol);
				stringBuilder.Append(text);
				stringBuilder.Append(ClosePatternSymbol);
				break;
			case 1:
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(symbol);
				stringBuilder.Append(text);
				break;
			case 2:
				stringBuilder.Append(symbol);
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(text);
				break;
			case 3:
				stringBuilder.Append(symbol);
				stringBuilder.Append(text);
				stringBuilder.Append(negativeSign);
				break;
			case 4:
				stringBuilder.Append(OpenPatternSymbol);
				stringBuilder.Append(text);
				stringBuilder.Append(symbol);
				stringBuilder.Append(ClosePatternSymbol);
				break;
			case 5:
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(text);
				stringBuilder.Append(symbol);
				break;
			case 6:
				stringBuilder.Append(text);
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(symbol);
				break;
			case 7:
				stringBuilder.Append(text);
				stringBuilder.Append(symbol);
				stringBuilder.Append(negativeSign);
				break;
			case 8:
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(text);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(symbol);
				break;
			case 9:
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(symbol);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(text);
				break;
			case 10:
				stringBuilder.Append(text);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(symbol);
				stringBuilder.Append(negativeSign);
				break;
			case 11:
				stringBuilder.Append(symbol);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(text);
				stringBuilder.Append(negativeSign);
				break;
			case 12:
				stringBuilder.Append(symbol);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(text);
				break;
			case 13:
				stringBuilder.Append(text);
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(symbol);
				break;
			case 14:
				stringBuilder.Append(OpenPatternSymbol);
				stringBuilder.Append(symbol);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(text);
				stringBuilder.Append(ClosePatternSymbol);
				break;
			case 15:
				stringBuilder.Append(OpenPatternSymbol);
				stringBuilder.Append(text);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(symbol);
				stringBuilder.Append(ClosePatternSymbol);
				break;
			default:
				break;
		}

		return stringBuilder.ToString();
	}

	private string FormatCurrencyCodeModePositiveNumber(string text, string symbol)
	{
		var pattern = CultureInfo.CurrentCulture.NumberFormat.CurrencyPositivePattern;
		var spaceSymbol = NoBreakSpaceChar;
		var stringBuilder = new StringBuilder();

		switch (pattern)
		{
			case 0:
			case 2:
				stringBuilder.Append(symbol);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(text);
				break;
			case 1:
			case 3:
				stringBuilder.Append(text);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(symbol);
				break;
			default:
				break;
		}

		return stringBuilder.ToString();
	}

	private string FormatSymbolModePositiveNumber(string text, string symbol)
	{
		var pattern = CultureInfo.CurrentCulture.NumberFormat.CurrencyPositivePattern;
		var spaceSymbol = NoBreakSpaceChar;
		var stringBuilder = new StringBuilder();

		switch (pattern)
		{
			case 0:
				stringBuilder.Append(symbol);
				stringBuilder.Append(text);
				break;
			case 1:
				stringBuilder.Append(text);
				stringBuilder.Append(symbol);
				break;
			case 2:
				stringBuilder.Append(symbol);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(text);
				break;
			case 3:
				stringBuilder.Append(text);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(symbol);
				break;
			default:
				break;
		}

		return stringBuilder.ToString();
	}

	[TestMethod]
	public void When_ConstructorWithLanguagesAndRegion()
	{
		var sut = new CurrencyFormatter(USDCurrencyCode, new[] { "en-us" }, "US");

		Assert.AreEqual(USDCurrencyCode, sut.Currency);
		Assert.AreEqual("US", sut.GeographicRegion);
		Assert.AreEqual("US", sut.ResolvedGeographicRegion);
		Assert.AreEqual("en-US", sut.ResolvedLanguage);
	}

	[TestMethod]
	public void When_ConstructorWithNullLanguagesAndRegion()
	{
		// WinUI throws NullReferenceException when languages is null
		Assert.Throws<NullReferenceException>(() => new CurrencyFormatter(USDCurrencyCode, null, null));
	}

	[TestMethod]
	public void When_CurrencyProperty()
	{
		var sut = MakeFormatter(USDCurrencyCode);
		Assert.AreEqual(USDCurrencyCode, sut.Currency);

		var eurSut = MakeFormatter("EUR");
		Assert.AreEqual("EUR", eurSut.Currency);
	}

	[TestMethod]
	[DataRow(100L, "100.00")]
	[DataRow(-50L, "50.00")]
	[DataRow(0L, "0.00")]
	[DataRow(1234567890L, "1234567890.00")]
	public void When_FormatLong(long value, string expectedNumber)
	{
		var sut = MakeFormatter(USDCurrencyCode);
		var actual = sut.Format(value);

		string expected;
		if (value < 0)
		{
			expected = FormatSymbolModeNegativeNumber(expectedNumber, USDSymbol);
		}
		else
		{
			expected = FormatSymbolModePositiveNumber(expectedNumber, USDSymbol);
		}

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	[DataRow(100UL, "100.00")]
	[DataRow(0UL, "0.00")]
	[DataRow(1234567890UL, "1234567890.00")]
	public void When_FormatULong(ulong value, string expectedNumber)
	{
		var sut = MakeFormatter(USDCurrencyCode);
		var actual = sut.Format(value);
		var expected = FormatSymbolModePositiveNumber(expectedNumber, USDSymbol);

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	[DataRow(100L, "100.00")]
	[DataRow(-50L, "50.00")]
	public void When_FormatInt(long value, string expectedNumber)
	{
		var sut = MakeFormatter(USDCurrencyCode);
		var actual = sut.FormatInt(value);

		string expected;
		if (value < 0)
		{
			expected = FormatSymbolModeNegativeNumber(expectedNumber, USDSymbol);
		}
		else
		{
			expected = FormatSymbolModePositiveNumber(expectedNumber, USDSymbol);
		}

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	[DataRow(100UL, "100.00")]
	[DataRow(0UL, "0.00")]
	public void When_FormatUInt(ulong value, string expectedNumber)
	{
		var sut = MakeFormatter(USDCurrencyCode);
		var actual = sut.FormatUInt(value);
		var expected = FormatSymbolModePositiveNumber(expectedNumber, USDSymbol);

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void When_ParseInt()
	{
		var sut = MakeFormatter(USDCurrencyCode);
		var text = FormatSymbolModePositiveNumber("123.00", USDSymbol);
		var value = sut.ParseInt(text);

		Assert.AreEqual(123L, value);
	}

	[TestMethod]
	public void When_ParseIntWithFraction()
	{
		var sut = MakeFormatter(USDCurrencyCode);
		var text = FormatSymbolModePositiveNumber("123.45", USDSymbol);
		var value = sut.ParseInt(text);

		// WinUI returns null when parsing a value with non-zero fractional part
		Assert.IsNull(value);
	}

	[TestMethod]
	public void When_ParseIntNotValid()
	{
		var sut = MakeFormatter(USDCurrencyCode);
		var value = sut.ParseInt("123.00");

		Assert.IsNull(value);
	}

	[TestMethod]
	public void When_ParseUInt()
	{
		var sut = MakeFormatter(USDCurrencyCode);
		var text = FormatSymbolModePositiveNumber("123.00", USDSymbol);
		var value = sut.ParseUInt(text);

		Assert.AreEqual(123UL, value);
	}

	[TestMethod]
	public void When_ParseUIntWithFraction()
	{
		var sut = MakeFormatter(USDCurrencyCode);
		var text = FormatSymbolModePositiveNumber("123.99", USDSymbol);
		var value = sut.ParseUInt(text);

		// WinUI returns null when parsing a value with non-zero fractional part
		Assert.IsNull(value);
	}

	[TestMethod]
	public void When_ParseUIntNotValid()
	{
		var sut = MakeFormatter(USDCurrencyCode);
		var value = sut.ParseUInt("123.00");

		Assert.IsNull(value);
	}

	private static CurrencyFormatter MakeFormatter(string currencyCode) =>
		new CurrencyFormatter(currencyCode);
}
