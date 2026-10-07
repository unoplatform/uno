#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Globalization.NumberFormatting;

namespace Uno.UI.Tests.Windows_Globalization;

[TestClass]
public class Given_DecimalFormatter_Integers
{
	[TestMethod]
	[DataRow(0L, 1, 0, false, "0")]
	[DataRow(5L, 1, 0, false, "5")]
	[DataRow(-5L, 1, 0, false, "-5")]
	[DataRow(5L, 1, 2, false, "5.00")]
	[DataRow(5L, 3, 0, false, "005")]
	[DataRow(1234567L, 1, 0, true, "1,234,567")]
	[DataRow(1234L, 6, 0, true, "001,234")]
	[DataRow(-1234L, 1, 0, true, "-1,234")]
	[DataRow(long.MaxValue, 1, 0, false, "9223372036854775807")]
	[DataRow(long.MinValue, 1, 0, false, "-9223372036854775808")]
	[DataRow(0L, 1, 2, false, "0.00")]
	public void When_FormatInt(long value, int integerDigits, int fractionDigits, bool isGrouped, string expected)
	{
		DecimalFormatter sut = new()
		{
			IntegerDigits = integerDigits,
			FractionDigits = fractionDigits,
			IsGrouped = isGrouped,
		};

		Assert.AreEqual(expected, sut.FormatInt(value));
		Assert.AreEqual(expected, sut.Format(value));
	}

	[TestMethod]
	public void When_FormatInt_DecimalPointAlwaysDisplayed()
	{
		DecimalFormatter sut = new()
		{
			FractionDigits = 0,
			IsDecimalPointAlwaysDisplayed = true,
		};

		Assert.AreEqual("42.", sut.FormatInt(42));
	}

	[TestMethod]
	[DataRow(0UL, "0")]
	[DataRow(42UL, "42")]
	[DataRow(ulong.MaxValue, "18446744073709551615")]
	public void When_FormatUInt(ulong value, string expected)
	{
		DecimalFormatter sut = new() { FractionDigits = 0 };

		Assert.AreEqual(expected, sut.FormatUInt(value));
		Assert.AreEqual(expected, sut.Format(value));
	}

	[TestMethod]
	public void When_Languages_And_Region_Then_Formats_With_Language_Numerals()
	{
		DecimalFormatter sut = new(new[] { "ar-SA" }, "SA") { FractionDigits = 0 };

		Assert.AreEqual("SA", sut.GeographicRegion);
		Assert.AreEqual("ar-SA", sut.Languages[0]);
		Assert.AreEqual("Arab", sut.NumeralSystem);
		Assert.AreEqual("١٢٣", sut.FormatInt(123));
	}

	[TestMethod]
	public void When_Languages_And_Region_Latin_Then_FormatInt()
	{
		DecimalFormatter sut = new(new[] { "en-US" }, "US") { FractionDigits = 0 };

		Assert.AreEqual("US", sut.ResolvedGeographicRegion);
		Assert.AreEqual("7", sut.FormatInt(7));
	}

	[TestMethod]
	[DataRow("")]
	[DataRow("U")]
	[DataRow("USA1")]
	[DataRow("U1")]
	public void When_Region_Is_Invalid_Then_Throws(string region)
		=> Assert.ThrowsExactly<ArgumentException>(() => new DecimalFormatter(new[] { "en-US" }, region));

	[TestMethod]
	public void When_Languages_Empty_Then_Throws()
		=> Assert.ThrowsExactly<ArgumentException>(() => new DecimalFormatter(Array.Empty<string>(), "US"));
}
