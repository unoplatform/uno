#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.GridTests;

[TestClass]
public class Given_GridLengthHelper
{
	[TestMethod]
	[DataRow(0.1)]
	[DataRow(123.456789)]
	[DataRow(1e10 + 0.5)]
	public void When_FromPixels_Then_Double_Precision_Kept(double pixels)
	{
		var length = GridLengthHelper.FromPixels(pixels);

		Assert.AreEqual(pixels, length.Value);
		Assert.AreEqual(GridUnitType.Pixel, length.GridUnitType);
	}

	[TestMethod]
	public void When_FromValueAndType_Star_Then_Double_Precision_Kept()
	{
		var length = GridLengthHelper.FromValueAndType(0.3, GridUnitType.Star);

		Assert.AreEqual(0.3, length.Value);
		Assert.IsTrue(GridLengthHelper.GetIsStar(length));
	}

	[TestMethod]
	public void When_FromValueAndType_Auto_Then_Value_Is_One()
	{
		var length = GridLengthHelper.FromValueAndType(42, GridUnitType.Auto);

		Assert.AreEqual(1.0, length.Value);
		Assert.IsTrue(GridLengthHelper.GetIsAuto(length));
		Assert.IsTrue(GridLengthHelper.Equals(GridLengthHelper.Auto, length));
	}

	[TestMethod]
	[DataRow(double.NaN)]
	[DataRow(double.PositiveInfinity)]
	[DataRow(double.NegativeInfinity)]
	[DataRow(-1.0)]
	public void When_FromPixels_Invalid_Then_Throws(double pixels)
		=> Assert.ThrowsExactly<ArgumentException>(() => GridLengthHelper.FromPixels(pixels));

	[TestMethod]
	public void When_FromValueAndType_Unknown_Type_Then_Throws()
		=> Assert.ThrowsExactly<ArgumentException>(() => GridLengthHelper.FromValueAndType(1, (GridUnitType)42));
}
