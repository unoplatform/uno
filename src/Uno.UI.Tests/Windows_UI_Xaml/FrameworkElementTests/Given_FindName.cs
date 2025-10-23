using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using AwesomeAssertions.Execution;
using Microsoft.UI.Xaml;

namespace Uno.UI.Tests.Windows_UI_Xaml.FrameworkElementTests
{
	[TestClass]
#if !IS_UNIT_TESTS
	[RuntimeTests.RunsOnUIThread]
#endif
	public class Given_FindName
	{
		[TestMethod]
		public void When_SimpleElement()
		{
			var SUT = new Grid();

			SUT.Children.Add(new Border { Name = "test" });

			Assert.AreEqual(SUT.Children.First(), SUT.FindName("test"));
		}

		[TestMethod]
		public void When_ContextFlyout()
		{
			var SUT = new Grid();

			var test1 = new MenuFlyoutItem { Name = "test1" };
			var test2 = new MenuFlyoutItem { Name = "test2" };

			SUT.ContextFlyout = new MenuFlyout
			{
				Items = {
					test1,
					test2
				}
			};

			Assert.AreEqual(test1, SUT.FindName("test1"));
			Assert.AreEqual(test2, SUT.FindName("test2"));
		}

		[TestMethod]
		public void When_ButtonFlyout()
		{
			var SUT = new Grid();
			var button = new Button() { Style = new Style() };

			SUT.Children.Add(button);

			var test1 = new MenuFlyoutItem { Name = "test1" };
			var test2 = new MenuFlyoutItem { Name = "test2" };

			button.Flyout = new MenuFlyout
			{
				Items = {
					test1,
					test2
				}
			};

			Assert.AreEqual(test1, SUT.FindName("test1"));
			Assert.AreEqual(test2, SUT.FindName("test2"));
		}
	}
}
