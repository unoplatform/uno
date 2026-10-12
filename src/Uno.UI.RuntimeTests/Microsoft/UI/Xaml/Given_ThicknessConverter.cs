#if !WINAPPSDK
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_ThicknessConverter
	{
		[TestMethod]
		public void When_Thickness_With_Single_Value()
		{
			var converter = new ThicknessConverter();
			var value = (Thickness)converter.ConvertFrom("23");
			Assert.AreEqual(23, value.Left);
			Assert.AreEqual(23, value.Right);
			Assert.AreEqual(23, value.Bottom);
			Assert.AreEqual(23, value.Top);
		}

		[TestMethod]
		public void When_Thickness_With_Two_Values_Comma()
		{
			var converter = new ThicknessConverter();
			var value = (Thickness)converter.ConvertFrom("23,35");
			Assert.AreEqual(23, value.Left);
			Assert.AreEqual(35, value.Top);
			Assert.AreEqual(23, value.Right);
			Assert.AreEqual(35, value.Bottom);
		}

		[TestMethod]
		public void When_Thickness_With_Two_Values_Whitespace()
		{
			var converter = new ThicknessConverter();
			var value = (Thickness)converter.ConvertFrom("   23		   35			 ");
			Assert.AreEqual(23, value.Left);
			Assert.AreEqual(35, value.Top);
			Assert.AreEqual(23, value.Right);
			Assert.AreEqual(35, value.Bottom);
		}

		[TestMethod]
		public void When_Thickness_With_Four_Values_Comma()
		{
			var converter = new ThicknessConverter();
			var value = (Thickness)converter.ConvertFrom("23,35,2,65");
			Assert.AreEqual(23, value.Left);
			Assert.AreEqual(35, value.Top);
			Assert.AreEqual(2, value.Right);
			Assert.AreEqual(65, value.Bottom);
		}

		[TestMethod]
		public void When_Thickness_With_Four_Values_Whitespace()
		{
			var converter = new ThicknessConverter();
			var value = (Thickness)converter.ConvertFrom("		 23   35 2				65  ");
			Assert.AreEqual(23, value.Left);
			Assert.AreEqual(35, value.Top);
			Assert.AreEqual(2, value.Right);
			Assert.AreEqual(65, value.Bottom);
		}
	}
}
#endif
