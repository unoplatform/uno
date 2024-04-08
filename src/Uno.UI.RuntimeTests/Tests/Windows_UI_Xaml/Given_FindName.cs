using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Uno.UI.RuntimeTests;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.Tests.Windows_UI_Xaml.FrameworkElementTests;

[TestClass]
[RunsOnUIThread]
public class Given_FindName
{
	[TestMethod]
	public void When_SimpleElement_Without_NameScope()
	{
		var SUT = new Grid();

		SUT.Children.Add(new Border { Name = "test" });

		Assert.AreEqual(null, SUT.FindName("test"));
	}

	[TestMethod]
	public void When_SimpleElement_With_NameScope()
	{
		var SUT = new FindName_SimpleElement();

		var result = SUT.FindName("test");
		Assert.IsInstanceOfType(result, typeof(Border));
		Assert.AreEqual("ExpectedBorder", ((Border)result).Tag);
	}

	[TestMethod]
	public void When_ContextFlyout_Without_NameScope()
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

		Assert.AreEqual(null, SUT.FindName("test1"));
		Assert.AreEqual(null, SUT.FindName("test2"));
	}

	[TestMethod]
	public void When_ContextFlyout_With_NameScope()
	{
		var SUT = new FindName_ContextFlyout();
		var test1 = SUT.FindName("test1");
		var test2 = SUT.FindName("test2");

		Assert.IsInstanceOfType(test1, typeof(MenuFlyoutItem));
		Assert.AreEqual("FirstFlyoutItem", ((MenuFlyoutItem)test1).Tag);
		Assert.IsInstanceOfType(test2, typeof(MenuFlyoutItem));
		Assert.AreEqual("SecondFlyoutItem", ((MenuFlyoutItem)test2).Tag);
	}

	[TestMethod]
	public void When_ButtonFlyout_Without_NameScope()
	{
		var SUT = new Grid();
		var button = new Button() { Style = new Style(typeof(Button)) };

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

		Assert.AreEqual(null, SUT.FindName("test1"));
		Assert.AreEqual(null, SUT.FindName("test2"));
	}

	[TestMethod]
	public void When_ButtonFlyout_With_NameScope()
	{
		var SUT = new FindName_ButtonFlyout();

		var test1 = SUT.FindName("test1");
		var test2 = SUT.FindName("test2");

		Assert.IsInstanceOfType(test1, typeof(MenuFlyoutItem));
		Assert.AreEqual("FirstFlyoutItem", ((MenuFlyoutItem)test1).Tag);
		Assert.IsInstanceOfType(test2, typeof(MenuFlyoutItem));
		Assert.AreEqual("SecondFlyoutItem", ((MenuFlyoutItem)test2).Tag);
	}
}
