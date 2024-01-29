#if !WINAPPSDK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_CornerRadiusConverter
	{
		[TestMethod]
		public void When_CornerRadius_With_Single_Value()
		{
			var converter = new CornerRadiusConverter();
			var value = (CornerRadius)converter.ConvertFrom("23");
			Assert.AreEqual(23, value.TopLeft);
			Assert.AreEqual(23, value.TopRight);
			Assert.AreEqual(23, value.BottomRight);
			Assert.AreEqual(23, value.BottomLeft);
		}

		[TestMethod]
		public void When_CornerRadius_With_Four_Values_Comma()
		{
			var converter = new CornerRadiusConverter();
			var value = (CornerRadius)converter.ConvertFrom("23,35,2,65");
			Assert.AreEqual(23, value.TopLeft);
			Assert.AreEqual(35, value.TopRight);
			Assert.AreEqual(2, value.BottomRight);
			Assert.AreEqual(65, value.BottomLeft);
		}

		[TestMethod]
		public void When_CornerRadius_With_Four_Values_Whitespace()
		{
			var converter = new CornerRadiusConverter();
			var value = (CornerRadius)converter.ConvertFrom("		 23   35 2				65  ");
			Assert.AreEqual(23, value.TopLeft);
			Assert.AreEqual(35, value.TopRight);
			Assert.AreEqual(2, value.BottomRight);
			Assert.AreEqual(65, value.BottomLeft);
		}
	}
}
#endif
