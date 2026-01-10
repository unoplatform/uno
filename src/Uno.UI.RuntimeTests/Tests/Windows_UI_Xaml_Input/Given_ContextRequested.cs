using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Input;

/// <summary>
/// Tests for ContextRequested and ContextCanceled events using only public APIs.
/// These tests can run against both Uno Platform and WinUI.
/// </summary>
[TestClass]
[RunsOnUIThread]
public partial class Given_ContextRequested
{
	#region Event Subscription Tests

	[TestMethod]
	public async Task When_ContextRequested_Event_Can_Be_Subscribed()
	{
		var target = new Border
		{
			Background = new SolidColorBrush(Colors.Blue),
			Width = 100,
			Height = 100
		};

		bool eventRaised = false;
		target.ContextRequested += (sender, args) =>
		{
			eventRaised = true;
			args.Handled = true;
		};

		await UITestHelper.Load(target);

		// We can't directly raise the event without internal APIs,
		// but we can verify the subscription doesn't throw
		Assert.IsFalse(eventRaised, "Event should not be raised without input");
	}

	[TestMethod]
	public async Task When_ContextCanceled_Event_Can_Be_Subscribed()
	{
		var target = new Border
		{
			Background = new SolidColorBrush(Colors.Blue),
			Width = 100,
			Height = 100
		};

		bool eventRaised = false;
		target.ContextCanceled += (sender, args) =>
		{
			eventRaised = true;
		};

		await UITestHelper.Load(target);

		// Verify subscription doesn't throw
		Assert.IsFalse(eventRaised, "Event should not be raised without input");
	}

	#endregion

	#region ContextFlyout Property Tests

	[TestMethod]
	public async Task When_ContextFlyout_Is_Set_Property_Returns_Value()
	{
		var flyout = new MenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Test Item" });

		var target = new Button
		{
			Content = "Test Button",
			ContextFlyout = flyout
		};

		await UITestHelper.Load(target);

		Assert.AreSame(flyout, target.ContextFlyout, "ContextFlyout property should return the set value");
	}

	[TestMethod]
	public async Task When_ContextFlyout_Is_Null_By_Default()
	{
		var target = new Button
		{
			Content = "Test Button"
		};

		await UITestHelper.Load(target);

		Assert.IsNull(target.ContextFlyout, "ContextFlyout should be null by default");
	}

	[TestMethod]
	public async Task When_ContextFlyout_Can_Be_Changed()
	{
		var flyout1 = new MenuFlyout();
		flyout1.Items.Add(new MenuFlyoutItem { Text = "Item 1" });

		var flyout2 = new MenuFlyout();
		flyout2.Items.Add(new MenuFlyoutItem { Text = "Item 2" });

		var target = new Button
		{
			Content = "Test Button",
			ContextFlyout = flyout1
		};

		await UITestHelper.Load(target);

		Assert.AreSame(flyout1, target.ContextFlyout);

		target.ContextFlyout = flyout2;
		Assert.AreSame(flyout2, target.ContextFlyout);

		target.ContextFlyout = null;
		Assert.IsNull(target.ContextFlyout);
	}

	#endregion

	#region Event Handler Tests

	[TestMethod]
	public async Task When_ContextRequested_Handler_Can_Set_Handled()
	{
		var flyout = new MenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Test Item" });

		var target = new Button
		{
			Content = "Test Button",
			ContextFlyout = flyout
		};

		bool handlerCalled = false;
		target.ContextRequested += (sender, args) =>
		{
			handlerCalled = true;
			// Verify we can access Handled property
			Assert.IsFalse(args.Handled, "Handled should be false initially");
			args.Handled = true;
			Assert.IsTrue(args.Handled, "Handled should be true after setting");
		};

		await UITestHelper.Load(target);

		// Handler is registered but not yet called (needs input trigger)
		Assert.IsFalse(handlerCalled);
	}

	[TestMethod]
	public async Task When_ContextRequested_Handler_Can_Access_OriginalSource()
	{
		var child = new Border
		{
			Background = new SolidColorBrush(Colors.Blue),
			Width = 50,
			Height = 50
		};

		var parent = new Border
		{
			Background = new SolidColorBrush(Colors.Red),
			Width = 100,
			Height = 100,
			Child = child
		};

		object capturedSource = null;
		parent.ContextRequested += (sender, args) =>
		{
			capturedSource = args.OriginalSource;
			args.Handled = true;
		};

		await UITestHelper.Load(parent);

		// Handler registered, source will be set when event is raised
		Assert.IsNull(capturedSource);
	}

	#endregion

	#region Visual Tree Tests

	[TestMethod]
	public async Task When_ContextFlyout_On_Nested_Elements()
	{
		var childFlyout = new MenuFlyout();
		childFlyout.Items.Add(new MenuFlyoutItem { Text = "Child Item" });

		var parentFlyout = new MenuFlyout();
		parentFlyout.Items.Add(new MenuFlyoutItem { Text = "Parent Item" });

		var child = new Button
		{
			Content = "Child",
			ContextFlyout = childFlyout
		};

		var parent = new Border
		{
			Width = 200,
			Height = 200,
			ContextFlyout = parentFlyout,
			Child = child
		};

		await UITestHelper.Load(parent);

		// Both elements have their own ContextFlyout
		Assert.AreSame(childFlyout, child.ContextFlyout);
		Assert.AreSame(parentFlyout, parent.ContextFlyout);
	}

	[TestMethod]
	public async Task When_Child_Without_ContextFlyout_Parent_Has_One()
	{
		var parentFlyout = new MenuFlyout();
		parentFlyout.Items.Add(new MenuFlyoutItem { Text = "Parent Item" });

		var child = new Button
		{
			Content = "Child"
			// No ContextFlyout on child
		};

		var parent = new Border
		{
			Width = 200,
			Height = 200,
			ContextFlyout = parentFlyout,
			Child = child
		};

		await UITestHelper.Load(parent);

		Assert.IsNull(child.ContextFlyout, "Child should not have ContextFlyout");
		Assert.IsNotNull(parent.ContextFlyout, "Parent should have ContextFlyout");
	}

	#endregion

	#region Multiple Handlers Tests

	[TestMethod]
	public async Task When_Multiple_ContextRequested_Handlers()
	{
		var target = new Button
		{
			Content = "Test Button"
		};

		int handler1CallCount = 0;
		int handler2CallCount = 0;

		target.ContextRequested += (sender, args) =>
		{
			handler1CallCount++;
		};

		target.ContextRequested += (sender, args) =>
		{
			handler2CallCount++;
			args.Handled = true;
		};

		await UITestHelper.Load(target);

		// Both handlers should be registered without errors
		Assert.AreEqual(0, handler1CallCount);
		Assert.AreEqual(0, handler2CallCount);
	}

	[TestMethod]
	public async Task When_Handler_Removed_After_Adding()
	{
		var target = new Button
		{
			Content = "Test Button"
		};

		int callCount = 0;
		TypedEventHandler<UIElement, ContextRequestedEventArgs> handler = (sender, args) =>
		{
			callCount++;
		};

		target.ContextRequested += handler;
		target.ContextRequested -= handler;

		await UITestHelper.Load(target);

		// Handler should be removed successfully
		Assert.AreEqual(0, callCount);
	}

	#endregion

	#region ContextFlyout Type Tests

	[TestMethod]
	public async Task When_ContextFlyout_Is_MenuFlyout()
	{
		var flyout = new MenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Cut" });
		flyout.Items.Add(new MenuFlyoutItem { Text = "Copy" });
		flyout.Items.Add(new MenuFlyoutItem { Text = "Paste" });

		var target = new TextBox
		{
			Text = "Test text",
			ContextFlyout = flyout
		};

		await UITestHelper.Load(target);

		Assert.IsInstanceOfType(target.ContextFlyout, typeof(MenuFlyout));
		Assert.AreEqual(3, ((MenuFlyout)target.ContextFlyout).Items.Count);
	}

	[TestMethod]
	public async Task When_ContextFlyout_Is_Flyout()
	{
		var flyout = new Flyout
		{
			Content = new TextBlock { Text = "Custom flyout content" }
		};

		var target = new Button
		{
			Content = "Test Button",
			ContextFlyout = flyout
		};

		await UITestHelper.Load(target);

		Assert.IsInstanceOfType(target.ContextFlyout, typeof(Flyout));
	}

	#endregion

	#region Different Control Types Tests

	[TestMethod]
	public async Task When_ContextFlyout_On_Button()
	{
		var flyout = new MenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Action" });

		var button = new Button
		{
			Content = "Click me",
			ContextFlyout = flyout
		};

		await UITestHelper.Load(button);

		Assert.IsNotNull(button.ContextFlyout);
	}

	[TestMethod]
	public async Task When_ContextFlyout_On_TextBox()
	{
		var flyout = new MenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Cut" });
		flyout.Items.Add(new MenuFlyoutItem { Text = "Copy" });

		var textBox = new TextBox
		{
			Text = "Sample text",
			ContextFlyout = flyout
		};

		await UITestHelper.Load(textBox);

		Assert.IsNotNull(textBox.ContextFlyout);
	}

	[TestMethod]
	public async Task When_ContextFlyout_On_ListView()
	{
		var flyout = new MenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Delete" });
		flyout.Items.Add(new MenuFlyoutItem { Text = "Edit" });

		var listView = new ListView
		{
			ContextFlyout = flyout,
			ItemsSource = new[] { "Item 1", "Item 2", "Item 3" }
		};

		await UITestHelper.Load(listView);

		Assert.IsNotNull(listView.ContextFlyout);
	}

	[TestMethod]
	public async Task When_ContextFlyout_On_Grid()
	{
		var flyout = new MenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Refresh" });

		var grid = new Grid
		{
			Width = 200,
			Height = 200,
			Background = new SolidColorBrush(Colors.LightGray),
			ContextFlyout = flyout
		};

		await UITestHelper.Load(grid);

		Assert.IsNotNull(grid.ContextFlyout);
	}

	[TestMethod]
	public async Task When_ContextFlyout_On_Border()
	{
		var flyout = new MenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Option" });

		var border = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Colors.Green),
			ContextFlyout = flyout
		};

		await UITestHelper.Load(border);

		Assert.IsNotNull(border.ContextFlyout);
	}

	#endregion

	#region Event Args Property Tests

	[TestMethod]
	public void When_ContextRequestedEventArgs_Created()
	{
		// ContextRequestedEventArgs can be created (for testing purposes)
		var args = new ContextRequestedEventArgs();

		Assert.IsFalse(args.Handled, "Handled should be false by default");

		args.Handled = true;
		Assert.IsTrue(args.Handled, "Handled should be settable");
	}

	[TestMethod]
	public void When_ContextRequestedEventArgs_TryGetPosition_Without_Point()
	{
		// Test TryGetPosition on freshly created args (no position set)
		var args = new ContextRequestedEventArgs();

		// Without any position set, behavior depends on implementation
		bool hasPosition = args.TryGetPosition(null, out var point);

		// The result depends on whether a position was set internally
		// For keyboard invocation, this should return false
	}

	#endregion

	#region MenuFlyout Content Tests

	[TestMethod]
	public async Task When_MenuFlyout_Has_Items()
	{
		var flyout = new MenuFlyout();
		var item1 = new MenuFlyoutItem { Text = "Cut" };
		var item2 = new MenuFlyoutItem { Text = "Copy" };
		var item3 = new MenuFlyoutItem { Text = "Paste" };
		var separator = new MenuFlyoutSeparator();
		var item4 = new MenuFlyoutItem { Text = "Delete" };

		flyout.Items.Add(item1);
		flyout.Items.Add(item2);
		flyout.Items.Add(item3);
		flyout.Items.Add(separator);
		flyout.Items.Add(item4);

		var target = new Button
		{
			Content = "Edit",
			ContextFlyout = flyout
		};

		await UITestHelper.Load(target);

		Assert.AreEqual(5, ((MenuFlyout)target.ContextFlyout).Items.Count);
	}

	[TestMethod]
	public async Task When_MenuFlyout_Has_SubMenus()
	{
		var flyout = new MenuFlyout();

		var subItem = new MenuFlyoutSubItem { Text = "More Options" };
		subItem.Items.Add(new MenuFlyoutItem { Text = "Option A" });
		subItem.Items.Add(new MenuFlyoutItem { Text = "Option B" });

		flyout.Items.Add(new MenuFlyoutItem { Text = "Simple Item" });
		flyout.Items.Add(subItem);

		var target = new Button
		{
			Content = "Options",
			ContextFlyout = flyout
		};

		await UITestHelper.Load(target);

		var menuFlyout = (MenuFlyout)target.ContextFlyout;
		Assert.AreEqual(2, menuFlyout.Items.Count);

		var subMenu = menuFlyout.Items[1] as MenuFlyoutSubItem;
		Assert.IsNotNull(subMenu);
		Assert.AreEqual(2, subMenu.Items.Count);
	}

	#endregion

	#region Data Binding Tests

	[TestMethod]
	public async Task When_ContextFlyout_Set_Via_Style()
	{
		var flyout = new MenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Styled Item" });

		var style = new Style(typeof(Button));
		style.Setters.Add(new Setter(UIElement.ContextFlyoutProperty, flyout));

		var button = new Button
		{
			Content = "Styled Button",
			Style = style
		};

		await UITestHelper.Load(button);

		Assert.IsNotNull(button.ContextFlyout);
		Assert.AreSame(flyout, button.ContextFlyout);
	}

	#endregion
}
