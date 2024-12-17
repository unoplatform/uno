using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.WebAssembly;
using Windows.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.HtmlElementAttributeTests
{
	[TestClass]
	public class Given_HtmlElementAttribute
	{
		[TestMethod]
		public void Given_TagOverride()
		{
			var p = new TestControl();

			Assert.AreEqual("p", p.HtmlTag);
		}
	}

	[HtmlElement("p")]
	public class TestControl : FrameworkElement
	{

	}
}
