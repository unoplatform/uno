using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests
{
	[TestClass]
	public class Given_xBind_UserControl
	{
		[TestMethod]
		public void When_Initial_Value()
		{
			var SUT = new xBind_UserControl();
			var _MyProperty = SUT.FindName("tb1") as TextBlock;
			Assert.AreEqual("", _MyProperty.Text);

			SUT.ForceLoaded();

			Assert.AreEqual("Default string", _MyProperty.Text);
		}

		[TestMethod]
		public void When_xBind_UserControl_To_OtherControl()
		{
			var SUT = new xBind_UserControl_To_OtherControl();

			var tb1 = SUT.FindName("tb1") as TextBlock;
			Assert.IsNotNull(tb1);
			Assert.AreEqual("", tb1.Text);

			SUT.ForceLoaded();

			Assert.AreEqual("Default string", tb1.Text);

			var inner = SUT.Content as xBind_UserControl_To_OtherControl_Inner;

			Assert.IsNotNull(inner);
			Assert.AreEqual("Default string", inner.MyProperty);
			Assert.AreEqual("Default string", tb1.Text);
		}
	}
}
