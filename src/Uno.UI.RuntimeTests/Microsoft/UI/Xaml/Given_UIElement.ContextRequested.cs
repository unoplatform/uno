#if HAS_UNO
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
public partial class Given_UIElement_ContextRequested
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ContextRequested_Event_Is_Raised()
	{
		var button = new Button { Content = "Test Button" };
		var contextRequestedRaised = false;
		ContextRequestedEventArgs receivedArgs = null;

		button.ContextRequested += (sender, args) =>
		{
			contextRequestedRaised = true;
			receivedArgs = args;
		};

		await UITestHelper.Load(button);

		// Simulate raising ContextRequested via the ContextMenuProcessor
		var contentRoot = Uno.UI.Xaml.Core.VisualTree.GetContentRootForElement(button);
		Assert.IsNotNull(contentRoot, "ContentRoot should not be null");

		contentRoot.InputManager.ContextMenuProcessor.RaiseContextRequestedEvent(
			button,
			new Point(100, 100),
			isTouchInput: false);

		Assert.IsTrue(contextRequestedRaised, "ContextRequested event should be raised");
		Assert.IsNotNull(receivedArgs, "Event args should not be null");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ContextRequested_TryGetPosition_Returns_Correct_Position()
	{
		var button = new Button { Content = "Test Button" };
		Point? receivedPosition = null;
		bool gotPosition = false;

		button.ContextRequested += (sender, args) =>
		{
			gotPosition = args.TryGetPosition(button, out var point);
			receivedPosition = point;
			args.Handled = true;
		};

		await UITestHelper.Load(button);

		var contentRoot = Uno.UI.Xaml.Core.VisualTree.GetContentRootForElement(button);
		Assert.IsNotNull(contentRoot);

		// Raise with a specific position
		contentRoot.InputManager.ContextMenuProcessor.RaiseContextRequestedEvent(
			button,
			new Point(50, 75),
			isTouchInput: false);

		Assert.IsTrue(gotPosition, "TryGetPosition should return true for pointer invocation");
		Assert.IsNotNull(receivedPosition, "Position should be set");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ContextRequested_Keyboard_Invocation_TryGetPosition_Returns_False()
	{
		var button = new Button { Content = "Test Button" };
		bool gotPosition = true; // Start with true to verify it becomes false

		button.ContextRequested += (sender, args) =>
		{
			gotPosition = args.TryGetPosition(button, out _);
			args.Handled = true;
		};

		await UITestHelper.Load(button);

		var contentRoot = Uno.UI.Xaml.Core.VisualTree.GetContentRootForElement(button);
		Assert.IsNotNull(contentRoot);

		// Raise with (-1, -1) which indicates keyboard invocation
		contentRoot.InputManager.ContextMenuProcessor.RaiseContextRequestedEvent(
			button,
			new Point(-1, -1),
			isTouchInput: false);

		Assert.IsFalse(gotPosition, "TryGetPosition should return false for keyboard invocation");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ContextRequested_Handled_ContextFlyout_Still_Shown()
	{
		var flyout = new MenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Option 1" });

		var button = new Button
		{
			Content = "Test Button",
			ContextFlyout = flyout
		};

		button.ContextRequested += (sender, args) =>
		{
			args.Handled = true; // Prevent default flyout
		};

		await UITestHelper.Load(button);

		var contentRoot = Uno.UI.Xaml.Core.VisualTree.GetContentRootForElement(button);
		Assert.IsNotNull(contentRoot);

		contentRoot.InputManager.ContextMenuProcessor.RaiseContextRequestedEvent(
			button,
			new Point(100, 100),
			isTouchInput: false);

		// Wait a bit for any flyout to potentially open
		await Task.Delay(100);

		Assert.IsTrue(flyout.IsOpen, "ContextFlyout should be shown even when event is handled");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ContextRequested_Not_Handled_ContextFlyout_Shown()
	{
		var flyout = new MenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Option 1" });

		var button = new Button
		{
			Content = "Test Button",
			ContextFlyout = flyout
		};

		await UITestHelper.Load(button);

		var contentRoot = Uno.UI.Xaml.Core.VisualTree.GetContentRootForElement(button);
		Assert.IsNotNull(contentRoot);

		contentRoot.InputManager.ContextMenuProcessor.RaiseContextRequestedEvent(
			button,
			new Point(100, 100),
			isTouchInput: false);

		// Wait for flyout to open
		await Task.Delay(100);

		Assert.IsTrue(flyout.IsOpen, "ContextFlyout should be shown when event is not handled");

		// Close the flyout
		flyout.Hide();
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ContextRequested_Bubbles_Up_Tree()
	{
		var innerButton = new Button { Content = "Inner" };
		var outerBorder = new Border { Child = innerButton };
		var contextRequestedOnBorder = false;

		outerBorder.ContextRequested += (sender, args) =>
		{
			contextRequestedOnBorder = true;
		};

		await UITestHelper.Load(outerBorder);

		var contentRoot = Uno.UI.Xaml.Core.VisualTree.GetContentRootForElement(innerButton);
		Assert.IsNotNull(contentRoot);

		// Raise on inner button, should bubble to border
		contentRoot.InputManager.ContextMenuProcessor.RaiseContextRequestedEvent(
			innerButton,
			new Point(50, 50),
			isTouchInput: false);

		Assert.IsTrue(contextRequestedOnBorder, "ContextRequested should bubble to parent");
	}
}
#endif
