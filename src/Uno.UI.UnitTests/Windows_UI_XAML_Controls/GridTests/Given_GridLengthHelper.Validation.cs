#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.GridTests;

// Mirrors GridLengthFactory::FromValueAndType (GridLength_Partial.cpp): E_INVALIDARG for a NaN,
// infinite or negative value or an unknown unit type, checked before Auto replaces the value.
public partial class Given_GridLengthHelper
{
	[TestMethod]
	[DataRow(double.NaN, GridUnitType.Pixel)]
	[DataRow(double.NaN, GridUnitType.Star)]
	[DataRow(double.NaN, GridUnitType.Auto)]
	[DataRow(double.PositiveInfinity, GridUnitType.Star)]
	[DataRow(double.PositiveInfinity, GridUnitType.Auto)]
	[DataRow(double.NegativeInfinity, GridUnitType.Pixel)]
	[DataRow(-0.5, GridUnitType.Pixel)]
	[DataRow(-1.0, GridUnitType.Star)]
	[DataRow(-1.0, GridUnitType.Auto)]
	public void When_FromValueAndType_Invalid_Value_Then_Throws(double value, GridUnitType type)
		=> Assert.ThrowsExactly<ArgumentException>(() => GridLengthHelper.FromValueAndType(value, type));

	[TestMethod]
	[DataRow(-1)]
	[DataRow(3)]
	[DataRow(int.MaxValue)]
	public void When_FromValueAndType_Out_Of_Range_Type_Then_Throws(int type)
		=> Assert.ThrowsExactly<ArgumentException>(() => GridLengthHelper.FromValueAndType(1.0, (GridUnitType)type));

	[TestMethod]
	[DataRow(0.0, GridUnitType.Pixel)]
	[DataRow(0.0, GridUnitType.Star)]
	[DataRow(-0.0, GridUnitType.Pixel)]
	[DataRow(double.MaxValue, GridUnitType.Pixel)]
	[DataRow(double.Epsilon, GridUnitType.Star)]
	public void When_FromValueAndType_Boundary_Value_Then_Kept(double value, GridUnitType type)
	{
		var length = GridLengthHelper.FromValueAndType(value, type);

		Assert.AreEqual(value, length.Value);
		Assert.AreEqual(type, length.GridUnitType);
	}

	[TestMethod]
	[DataRow(0.0)]
	[DataRow(1.0)]
	[DataRow(double.MaxValue)]
	public void When_FromValueAndType_Auto_Then_Value_Stored_As_One(double value)
	{
		var length = GridLengthHelper.FromValueAndType(value, GridUnitType.Auto);

		Assert.AreEqual(1.0, length.Value);
		Assert.AreEqual(GridUnitType.Auto, length.GridUnitType);
	}

	[TestMethod]
	public void When_Auto_Then_Value_Is_One()
	{
		Assert.AreEqual(1.0, GridLengthHelper.Auto.Value);
		Assert.AreEqual(GridUnitType.Auto, GridLengthHelper.Auto.GridUnitType);
	}

	[TestMethod]
	public void When_FromPixels_Zero_Then_Valid()
	{
		var length = GridLengthHelper.FromPixels(0);

		Assert.AreEqual(0.0, length.Value);
		Assert.IsTrue(GridLengthHelper.GetIsAbsolute(length));
	}
}
