using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.CollectionViewTests
{
	[Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
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
	}
}
