#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;

namespace Uno.UI.Tests.Helpers;

[TestClass]
public class Given_ValueConversionHelpers
{
	[TestMethod]
	[DataRow(typeof(byte), PropertyType.UInt8)]
	[DataRow(typeof(short), PropertyType.Int16)]
	[DataRow(typeof(ushort), PropertyType.UInt16)]
	[DataRow(typeof(int), PropertyType.Int32)]
	[DataRow(typeof(uint), PropertyType.UInt32)]
	[DataRow(typeof(long), PropertyType.Int64)]
	[DataRow(typeof(ulong), PropertyType.UInt64)]
	[DataRow(typeof(float), PropertyType.Single)]
	[DataRow(typeof(double), PropertyType.Double)]
	[DataRow(typeof(char), PropertyType.Char16)]
	[DataRow(typeof(bool), PropertyType.Boolean)]
	[DataRow(typeof(string), PropertyType.String)]
	[DataRow(typeof(Guid), PropertyType.Guid)]
	[DataRow(typeof(DateTimeOffset), PropertyType.DateTime)]
	[DataRow(typeof(DateTime), PropertyType.DateTime)]
	[DataRow(typeof(TimeSpan), PropertyType.TimeSpan)]
	[DataRow(typeof(Point), PropertyType.Point)]
	[DataRow(typeof(Size), PropertyType.Size)]
	[DataRow(typeof(Rect), PropertyType.Rect)]
	[DataRow(typeof(Visibility), PropertyType.OtherType)]
	[DataRow(typeof(Thickness), PropertyType.OtherType)]
	[DataRow(typeof(decimal), PropertyType.OtherType)]
	[DataRow(typeof(object), PropertyType.OtherType)]
	public void When_GetPropertyType(Type type, PropertyType expected)
		=> Assert.AreEqual(expected, ValueConversionHelpers.GetPropertyType(type));

	[TestMethod]
	public void When_GetPropertyType_Null_Then_Empty()
		=> Assert.AreEqual(PropertyType.Empty, ValueConversionHelpers.GetPropertyType(null!));

	[TestMethod]
	public void When_TryGetPropertyType_Reference_Type_Then_Not_PropertyValue()
	{
		// A CsWinRT CCW of a plain class offers no IPropertyValue.
		Assert.IsFalse(ValueConversionHelpers.TryGetPropertyType(new object(), out var type));
		Assert.AreEqual(PropertyType.Empty, type);
		Assert.IsFalse(ValueConversionHelpers.TryGetPropertyType(new Uri("https://platform.uno"), out _));
	}

	[TestMethod]
	public void When_TryGetPropertyType_Null_Then_Not_PropertyValue()
		=> Assert.IsFalse(ValueConversionHelpers.TryGetPropertyType(null!, out _));

	[TestMethod]
	public void When_TryGetPropertyType_Boxed_Values_Then_PropertyValue()
	{
		Assert.IsTrue(ValueConversionHelpers.TryGetPropertyType("text", out var stringType));
		Assert.AreEqual(PropertyType.String, stringType);
		Assert.IsTrue(ValueConversionHelpers.TryGetPropertyType(42, out var intType));
		Assert.AreEqual(PropertyType.Int32, intType);
		Assert.IsTrue(ValueConversionHelpers.TryGetPropertyType(Visibility.Collapsed, out var enumType));
		Assert.AreEqual(PropertyType.OtherType, enumType);
	}

	private enum AppKind
	{
		First,
	}

	private readonly record struct AppPoint(int X);

	[TestMethod]
	public void When_TryGetPropertyType_Non_WinRT_Value_Then_Not_PropertyValue()
	{
		// CsWinRT offers IReference<T>/IPropertyValue only for WinRT scalars and projected structs/enums.
		Assert.IsFalse(ValueConversionHelpers.TryGetPropertyType(1.5m, out _));
		Assert.IsFalse(ValueConversionHelpers.TryGetPropertyType(DateTime.Now, out _));
		Assert.IsFalse(ValueConversionHelpers.TryGetPropertyType((1, 2), out _));
		Assert.IsFalse(ValueConversionHelpers.TryGetPropertyType(AppKind.First, out _));
		Assert.IsFalse(ValueConversionHelpers.TryGetPropertyType(new AppPoint(1), out _));
	}

	[TestMethod]
	public void When_TryGetPropertyType_Projected_Struct_Then_OtherType()
	{
		Assert.IsTrue(ValueConversionHelpers.TryGetPropertyType(new Thickness(1), out var thicknessType));
		Assert.AreEqual(PropertyType.OtherType, thicknessType);
		Assert.IsTrue(ValueConversionHelpers.TryGetPropertyType(DateTimeOffset.UnixEpoch, out var dateType));
		Assert.AreEqual(PropertyType.DateTime, dateType);
		Assert.IsTrue(ValueConversionHelpers.TryGetPropertyType(new Point(1, 2), out var pointType));
		Assert.AreEqual(PropertyType.Point, pointType);
	}
}
