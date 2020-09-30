using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.CollectionViewTests
{
	[TestClass]
	public class Given_XamlBindingHelper
	{
		[TestMethod]
		[DataRow("42", (int)42)]
		[DataRow("42", (double)42)]
		[DataRow("42.42", (double)42.42)]
		public void When_StringToNumber(string value, object expected)
		{
			Assert.AreEqual(expected, XamlBindingHelper.ConvertValue(expected.GetType(), value));
		}

		[TestMethod]
		[DataRow(42, 42.0)]
		[DataRow(42.0, 42.0)]
		[DataRow(0, 0.0)]
		[DataRow(0.0, 0.0)]
		public void When_ToGridLength(object value, double expected)
		{
			Assert.AreEqual(new GridLength(expected, GridUnitType.Pixel), XamlBindingHelper.ConvertValue(typeof(GridLength), value));
		}

		[TestMethod]
		[DataRow((int)1, "00:00:01")]
		[DataRow((int)0, "00:00:00")]
		[DataRow((double)0, "00:00:00")]
		[DataRow((double)1, "00:00:01")]
		[DataRow("00:00:00", "00:00:00")]
		[DataRow("1:0:0", "01:00:00")]
		[DataRow("1:2:3.4", "01:02:03.4")]
		[DataRow("12:23:34.45", "12:23:34.45")]
		public void When_ToTimeSpan(object value, string expected)
		{
			Assert.AreEqual(TimeSpan.Parse(expected), XamlBindingHelper.ConvertValue(typeof(TimeSpan), value));
		}

		[TestMethod]
		[DataRow("4,5", 4, 5)]
		[DataRow("4.5,5.7", 4.5, 5.7)]
		[DataRow("4, 5", 4, 5)]
		[DataRow("4.5, 5.7", 4.5, 5.7)]
		[DataRow("4   ,     5", 4, 5)]
		[DataRow("4.5   ,    5.7", 4.5, 5.7)]
		[DataRow("   4   ,     5   ", 4, 5)]
		[DataRow("  4.5   ,    5.7   ", 4.5, 5.7)]
		public void When_ToPoint(string value, double expectedX, double expectedY)
		{
			Assert.AreEqual(new Point(expectedX, expectedY), XamlBindingHelper.ConvertValue(typeof(Point), value));
		}

		[TestMethod]
		[DataRow("4,5", 4f, 5f)]
		[DataRow("4.5,5.7", 4.5f, 5.7f)]
		[DataRow("4, 5", 4f, 5f)]
		[DataRow("4.5, 5.7", 4.5f, 5.7f)]
		[DataRow("4   ,     5", 4f, 5f)]
		[DataRow("4.5   ,    5.7", 4.5f, 5.7f)]
		[DataRow("   4   ,     5   ", 4f, 5f)]
		[DataRow("  4.5   ,    5.7   ", 4.5f, 5.7f)]
		public void When_ToPointF(string value, float expectedX, float expectedY)
		{
			Assert.AreEqual(new System.Drawing.PointF(expectedX, expectedY), XamlBindingHelper.ConvertValue(typeof(System.Drawing.PointF), value));
		}

		[TestMethod]
		[DataRow("", 0)]
		[DataRow("0", 0)]
		[DataRow("  0  ", 0)]
		[DataRow("  0  5", 0)]
		[DataRow("  10  5", 10)]
		[DataRow("10  5", 10)]
		[DataRow("10  ", 10)]
		[DataRow("10", 10)]
		public void When_ToInt(string value, int expected)
		{
			Assert.AreEqual(expected, XamlBindingHelper.ConvertValue(typeof(int), value));
		}

		[TestMethod]
		[DataRow("", double.NaN)]
		[DataRow("0", 0.0)]
		[DataRow("  0  ", 0.0)]
		[DataRow("  0  5", 0.0)]
		[DataRow("  10.0  5", 10.0)]
		[DataRow("10.0  5", 10.0)]
		[DataRow("10.0  ", 10.0)]
		[DataRow("10  ", 10.0)]
		[DataRow("10.0", 10.0)]
		public void When_ToDouble(string value, double expected)
		{
			Assert.AreEqual(expected, XamlBindingHelper.ConvertValue(typeof(double), value));
		}
	}
}
