using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
public partial class Given_UIElement_ContextFlyoutParentChain
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Child_Has_No_ContextFlyout_Parent_Flyout_Shows()
	{
		var flyoutOpened = false;
		var flyout = new MenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Test Item" });
		flyout.Opened += (s, e) => flyoutOpened = true;

		var childButton = new Button { Content = "Child" };
		// Child has NO ContextFlyout

		var parentBorder = new Border
		{
			Child = childButton,
			ContextFlyout = flyout,
			Width = 200,
			Height = 100
		};

		await UITestHelper.Load(parentBorder);

		// Raise ContextRequested on the child element
		var args = new ContextRequestedEventArgs();
		args.SetGlobalPoint(new Point(50, 50));
		childButton.RaiseEvent(UIElement.ContextRequestedEvent, args);

		// Allow flyout to open
		await Task.Delay(100);

		Assert.IsTrue(args.Handled, "ContextRequested should be handled when parent has ContextFlyout");
		Assert.IsTrue(flyoutOpened, "Parent's ContextFlyout should open when child has none");

		// Close the flyout
		flyout.Hide();
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Child_Has_ContextFlyout_Child_Flyout_Shows()
	{
		var parentFlyoutOpened = false;
		var childFlyoutOpened = false;

		var parentFlyout = new MenuFlyout();
		parentFlyout.Items.Add(new MenuFlyoutItem { Text = "Parent Item" });
		parentFlyout.Opened += (s, e) => parentFlyoutOpened = true;

		var childFlyout = new MenuFlyout();
		childFlyout.Items.Add(new MenuFlyoutItem { Text = "Child Item" });
		childFlyout.Opened += (s, e) => childFlyoutOpened = true;

		var childButton = new Button
		{
			Content = "Child",
			ContextFlyout = childFlyout
		};

		var parentBorder = new Border
		{
			Child = childButton,
			ContextFlyout = parentFlyout,
			Width = 200,
			Height = 100
		};

		await UITestHelper.Load(parentBorder);

		// Raise ContextRequested on the child element
		var args = new ContextRequestedEventArgs();
		args.SetGlobalPoint(new Point(50, 50));
		childButton.RaiseEvent(UIElement.ContextRequestedEvent, args);

		// Allow flyout to open
		await Task.Delay(100);

		Assert.IsTrue(args.Handled, "ContextRequested should be handled");
		Assert.IsTrue(childFlyoutOpened, "Child's own ContextFlyout should open");
		Assert.IsFalse(parentFlyoutOpened, "Parent's ContextFlyout should NOT open when child has one");

		// Close the flyout
		childFlyout.Hide();
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_No_ContextFlyout_In_Tree_Event_Not_Handled()
	{
		var childButton = new Button { Content = "Child" };
		var parentBorder = new Border
		{
			Child = childButton,
			Width = 200,
			Height = 100
		};
		// Neither child nor parent has ContextFlyout

		await UITestHelper.Load(parentBorder);

		// Raise ContextRequested on the child element
		var args = new ContextRequestedEventArgs();
		args.SetGlobalPoint(new Point(50, 50));
		childButton.RaiseEvent(UIElement.ContextRequestedEvent, args);

		Assert.IsFalse(args.Handled, "ContextRequested should NOT be handled when no ContextFlyout in tree");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Grandparent_Has_ContextFlyout_It_Shows()
	{
		var flyoutOpened = false;
		var flyout = new MenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Grandparent Item" });
		flyout.Opened += (s, e) => flyoutOpened = true;

		var childButton = new Button { Content = "Child" };
		var parentBorder = new Border { Child = childButton };
		var grandparentGrid = new Grid
		{
			ContextFlyout = flyout,
			Width = 300,
			Height = 200
		};
		grandparentGrid.Children.Add(parentBorder);

		await UITestHelper.Load(grandparentGrid);

		// Raise ContextRequested on the child element (deepest)
		var args = new ContextRequestedEventArgs();
		args.SetGlobalPoint(new Point(50, 50));
		childButton.RaiseEvent(UIElement.ContextRequestedEvent, args);

		// Allow flyout to open
		await Task.Delay(100);

		Assert.IsTrue(args.Handled, "ContextRequested should be handled by grandparent's ContextFlyout");
		Assert.IsTrue(flyoutOpened, "Grandparent's ContextFlyout should open");

		// Close the flyout
		flyout.Hide();
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Handler_Sets_Handled_ContextFlyout_Not_Shown()
	{
		var flyoutOpened = false;
		var flyout = new MenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Test Item" });
		flyout.Opened += (s, e) => flyoutOpened = true;

		var childButton = new Button { Content = "Child" };
		var parentBorder = new Border
		{
			Child = childButton,
			ContextFlyout = flyout,
			Width = 200,
			Height = 100
		};

		await UITestHelper.Load(parentBorder);

		// Attach a handler that marks event as handled
		childButton.ContextRequested += (s, e) =>
		{
			e.Handled = true;
		};

		// Raise ContextRequested on the child element
		var args = new ContextRequestedEventArgs();
		args.SetGlobalPoint(new Point(50, 50));
		childButton.RaiseEvent(UIElement.ContextRequestedEvent, args);

		// Allow time for any flyout
		await Task.Delay(100);

		Assert.IsTrue(args.Handled, "ContextRequested should be handled by the handler");
		Assert.IsFalse(flyoutOpened, "ContextFlyout should NOT open when handler sets Handled=true");
	}
}
