using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;

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

	[TestMethod]
	public void When_FromResources()
	{
		var SUT = new FindName_FromResources();
		var btn = SUT.FindName("MyButton");
		Assert.IsNotNull(btn);
		Assert.IsInstanceOfType(btn, typeof(Button));
	}

	[TestMethod]
	public void When_ChildCanFindParent()
	{
		var SUT = new FindName_ChildCanFindParent();
		var btn = SUT.childButton;
		var result = btn.FindName("parentGrid");
		Assert.IsNotNull(result);
		Assert.IsInstanceOfType(result, typeof(Grid));
	}

	[TestMethod]
	public async Task When_Child_Is_Added_And_Removed()
	{
		var SUT = new StackPanel();
		var btn = new Button()
		{
			Name = "MyButton",
		};
		SUT.Children.Add(btn);

		await UITestHelper.Load(SUT);
		var btnViaFindName = SUT.FindName("MyButton");
		Assert.AreEqual(btn, btnViaFindName);

		btn.Name = "MyButton2";

		btnViaFindName = SUT.FindName("MyButton");
		var btnViaFindName2 = SUT.FindName("MyButton2");
		Assert.AreEqual(btn, btnViaFindName);
		Assert.AreEqual(btn, btnViaFindName2);

		SUT.Children.Clear();
		btnViaFindName = SUT.FindName("MyButton");
		btnViaFindName2 = SUT.FindName("MyButton2");
		Assert.AreEqual(btn, btnViaFindName);
		Assert.IsNull(btnViaFindName2);

		btn.Name = "MyButton2";
		btnViaFindName = SUT.FindName("MyButton");
		btnViaFindName2 = SUT.FindName("MyButton2");
		Assert.AreEqual(btn, btnViaFindName);
		Assert.IsNull(btnViaFindName2);

		btn.Name = "MyButton3";
		btnViaFindName = SUT.FindName("MyButton");
		btnViaFindName2 = SUT.FindName("MyButton2");
		var btnViaFindName3 = SUT.FindName("MyButton3");
		Assert.AreEqual(btn, btnViaFindName);
		Assert.IsNull(btnViaFindName3);
		Assert.IsNull(btnViaFindName2);
	}

	[TestMethod]
	public async Task When_Child_With_Same_Name_As_Original_Is_Added()
	{
		var SUT = new StackPanel();
		var btn = new Button()
		{
			Name = "MyButton",
		};
		SUT.Children.Add(btn);

		await UITestHelper.Load(SUT);
		var btnViaFindName = SUT.FindName("MyButton");
		Assert.AreEqual(btn, btnViaFindName);

		var btn2 = new Button()
		{
			Name = "MyButton",
		};

		SUT.Children.Add(btn2);

		btnViaFindName = SUT.FindName("MyButton");
		Assert.AreEqual(btn2, btnViaFindName);
	}

	[TestMethod]
	public async Task When_Child_With_Same_Name_As_Modified_Is_Added()
	{
		var SUT = new StackPanel();
		var btn = new Button()
		{
			Name = "MyButton",
		};
		SUT.Children.Add(btn);

		await UITestHelper.Load(SUT);
		var btnViaFindName = SUT.FindName("MyButton");
		Assert.AreEqual(btn, btnViaFindName);

		btn.Name = "MyButton2";

		btnViaFindName = SUT.FindName("MyButton");
		var btnViaFindName2 = SUT.FindName("MyButton2");
		Assert.AreEqual(btn, btnViaFindName);
		Assert.AreEqual(btn, btnViaFindName2);

		var btn2 = new Button()
		{
			Name = "MyButton",
		};

		SUT.Children.Add(btn2);

		btnViaFindName = SUT.FindName("MyButton");
		btnViaFindName2 = SUT.FindName("MyButton2");
		Assert.AreEqual(btn2, btnViaFindName);
		Assert.AreEqual(btn, btnViaFindName2);
	}

	[TestMethod]
	public async Task When_Disconnected_From_NameScope()
	{
		var btn = new Button()
		{
			Content = "Click",
			Name = "MyButton",
		};

		Assert.IsNull(btn.FindName("MyButton"));

		// Now, connect to NameScope.
		await UITestHelper.Load(btn);

		Assert.AreEqual(btn, btn.FindName("MyButton"));
	}
}
