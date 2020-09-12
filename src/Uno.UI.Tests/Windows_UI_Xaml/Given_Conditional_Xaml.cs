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

		[TestMethod]
		public void When_Conditional_IsApiContractNotPresent()
		{
			var page = new Test_Page();

			var tb = page.testConditionalTextBlock2;
			Assert.AreEqual("Not Contract", tb.Text);
		}

		[TestMethod]
		public void When_Uno_Conditional_And_No_Ignorable()
		{
			var page = new Test_Page();

			var rd = page.TestStackPanel.Resources;
			var hasWinOnlyBrush = rd.ContainsKey("WinOnlyBrush");
#if NETFX_CORE
			Assert.IsTrue(hasWinOnlyBrush);
#else
			Assert.IsFalse(hasWinOnlyBrush);
#endif
		}
	}
}
