using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.App.Xaml;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_Conditional_Xaml
	{
		[TestMethod]
		public void When_Conditional_IsApiContractPresent()
		{
			var page = new Test_Page();

			var tb = page.TestConditionalTextBlock;
			Assert.AreEqual("Contract 1", tb.Text);
		}
	}
}
