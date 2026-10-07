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
}
