using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.App.Xaml;
using Uno.UI.Tests.Windows_UI_Xaml.Controls;

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

		[TestMethod]
		public void When_Conditional_IsTypePresent()
		{
			var page = new When_Conditional_IsTypePresent_Page();

			Assert.AreEqual("Is present", page.fepresentTextBlock.Text);
			Assert.AreEqual("", page.fenotpresentTextBlock.Text);
			Assert.AreEqual("", page.fthepresentTextBlock.Text);
			Assert.AreEqual("Is not present", page.fthenotpresentTextBlock.Text);
#if NETFX_CORE
			Assert.AreEqual("Is present", page.easpresentTextBlock.Text);
			Assert.AreEqual("", page.easnotpresentTextBlock.Text);
#else
			// This will need to be adjusted if we ever implement... *checks notes*... Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation
			Assert.AreEqual("", page.easpresentTextBlock.Text);
			Assert.AreEqual("Is not present", page.easnotpresentTextBlock.Text);
#endif
		}
	}
}
