#if __SKIA__
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
using Windows.UI.Input.Preview.Injection;
using SamplesApp.UITests;
using Uno.Extensions;
using Uno.UI.Toolkit.DevTools.Input;
using Uno.UI.Toolkit.Extensions;
using Windows.ApplicationModel.Background;
using MUXControlsTestApp.Utilities;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Input;

/// <summary>
/// Input injection tests for ContextRequested and ContextCanceled events.
/// These tests use mouse right-click to trigger context menu.
/// </summary>
[TestClass]
[RunsOnUIThread]
public partial class Given_ContextRequested_Injection
{
	[TestMethod]
	public async Task When_RightClick_Raises_ContextRequested()
	{
		var target = new Border
		{
			Background = new SolidColorBrush(Microsoft.UI.Colors.Blue),
			Width = 100,
			Height = 100
		};

		bool contextRequestedRaised = false;
		Point? receivedPosition = null;

		target.ContextRequested += (sender, args) =>
		{
			contextRequestedRaised = true;
			if (args.TryGetPosition(target, out var pos))
			{
				receivedPosition = pos;
			}
			args.Handled = true;
		};

		await UITestHelper.Load(target);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");
		using var mouse = injector.GetMouse();

		var bounds = target.GetAbsoluteBounds();
		var center = bounds.GetCenter();

		// Right-click to trigger context menu
		mouse.PressRight(center);
		mouse.ReleaseRight();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(contextRequestedRaised, "ContextRequested should be raised on right-click");
		Assert.IsNotNull(receivedPosition, "Position should be available for mouse invocation");
	}

	[TestMethod]
	public async Task When_RightClick_Position_Is_Correct()
	{
		var target = new Border
		{
			Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
			Width = 200,
			Height = 200
		};

		Point? receivedPosition = null;

		target.ContextRequested += (sender, args) =>
		{
			args.TryGetPosition(target, out var pos);
			receivedPosition = pos;
			args.Handled = true;
		};

		await UITestHelper.Load(target);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");
		using var mouse = injector.GetMouse();

		var bounds = target.GetAbsoluteBounds();
		var center = bounds.GetCenter();

		// Right-click to trigger context menu
		mouse.PressRight(center);
		mouse.ReleaseRight();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNotNull(receivedPosition);
		// Position should be roughly at center (within element coordinates)
		Assert.IsTrue(receivedPosition.Value.X > 80 && receivedPosition.Value.X < 120,
			$"X position should be near center (100), was {receivedPosition.Value.X}");
		Assert.IsTrue(receivedPosition.Value.Y > 80 && receivedPosition.Value.Y < 120,
			$"Y position should be near center (100), was {receivedPosition.Value.Y}");
	}

	[TestMethod]
	public async Task When_RightClick_ContextFlyout_Shows()
	{
		var flyoutOpened = false;
		var flyout = new MenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Test Item" });
		flyout.Opened += (s, e) => flyoutOpened = true;

		var target = new Button
		{
			Content = "Test Button",
			Width = 100,
			Height = 50,
			ContextFlyout = flyout
		};

		await UITestHelper.Load(target);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");
		using var mouse = injector.GetMouse();

		var bounds = target.GetAbsoluteBounds();

		// Right-click to trigger context menu
		mouse.PressRight(bounds.GetCenter());
		mouse.ReleaseRight();
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(100); // Allow flyout to open

		Assert.IsTrue(flyoutOpened, "ContextFlyout should open on right-click");

		flyout.Hide();
	}

	[TestMethod]
	public async Task When_RightClick_Child_Without_ContextFlyout_Parent_Flyout_Shows()
	{
		var parentFlyoutOpened = false;
		var parentFlyout = new MenuFlyout();
		parentFlyout.Items.Add(new MenuFlyoutItem { Text = "Parent Item" });
		parentFlyout.Opened += (s, e) => parentFlyoutOpened = true;

		var childButton = new Button
		{
			Content = "Child",
			Width = 80,
			Height = 40
			// No ContextFlyout on child
		};

		var parentBorder = new Border
		{
			Child = childButton,
			ContextFlyout = parentFlyout,
			Width = 200,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray)
		};

		await UITestHelper.Load(parentBorder);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");
		using var mouse = injector.GetMouse();

		var childBounds = childButton.GetAbsoluteBounds();

		// Right-click on child to trigger context menu
		mouse.PressRight(childBounds.GetCenter());
		mouse.ReleaseRight();
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(100);

		Assert.IsTrue(parentFlyoutOpened, "Parent's ContextFlyout should open when child has none");

		parentFlyout.Hide();
	}

	[TestMethod]
	public async Task When_RightClick_Child_With_ContextFlyout_Child_Flyout_Shows()
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
			Width = 80,
			Height = 40,
			ContextFlyout = childFlyout
		};

		var parentBorder = new Border
		{
			Child = childButton,
			ContextFlyout = parentFlyout,
			Width = 200,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray)
		};

		await UITestHelper.Load(parentBorder);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");
		using var mouse = injector.GetMouse();

		var childBounds = childButton.GetAbsoluteBounds();

		// Right-click on child to trigger context menu
		mouse.PressRight(childBounds.GetCenter());
		mouse.ReleaseRight();
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(100);

		Assert.IsTrue(childFlyoutOpened, "Child's ContextFlyout should open");
		Assert.IsFalse(parentFlyoutOpened, "Parent's ContextFlyout should NOT open when child has one");

		childFlyout.Hide();
	}

	[TestMethod]
	public async Task When_RightClick_Event_Bubbles_To_Parent()
	{
		bool childHandlerCalled = false;
		bool parentHandlerCalled = false;

		var childButton = new Button
		{
			Content = "Child",
			Width = 80,
			Height = 40
		};

		var parentBorder = new Border
		{
			Child = childButton,
			Width = 200,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray)
		};

		childButton.ContextRequested += (sender, args) =>
		{
			childHandlerCalled = true;
			// Don't set Handled, let it bubble
		};

		parentBorder.ContextRequested += (sender, args) =>
		{
			parentHandlerCalled = true;
			args.Handled = true;
		};

		await UITestHelper.Load(parentBorder);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");
		using var mouse = injector.GetMouse();

		var childBounds = childButton.GetAbsoluteBounds();

		// Right-click on child to trigger context menu
		mouse.PressRight(childBounds.GetCenter());
		mouse.ReleaseRight();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(childHandlerCalled, "Child handler should be called");
		Assert.IsTrue(parentHandlerCalled, "Parent handler should be called when child doesn't handle");
	}

	[TestMethod]
	public async Task When_RightClick_Event_Does_Not_Bubble_When_Handled()
	{
		bool childHandlerCalled = false;
		bool parentHandlerCalled = false;

		var childButton = new Button
		{
			Content = "Child",
			Width = 80,
			Height = 40
		};

		var parentBorder = new Border
		{
			Child = childButton,
			Width = 200,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray)
		};

		childButton.ContextRequested += (sender, args) =>
		{
			childHandlerCalled = true;
			args.Handled = true; // Prevent bubbling
		};

		parentBorder.ContextRequested += (sender, args) =>
		{
			parentHandlerCalled = true;
		};

		await UITestHelper.Load(parentBorder);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");
		using var mouse = injector.GetMouse();

		var childBounds = childButton.GetAbsoluteBounds();

		// Right-click on child to trigger context menu
		mouse.PressRight(childBounds.GetCenter());
		mouse.ReleaseRight();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(childHandlerCalled, "Child handler should be called");
		Assert.IsFalse(parentHandlerCalled, "Parent handler should NOT be called when child handles");
	}

	[TestMethod]
	public async Task When_RightClick_OriginalSource_Is_Correct()
	{
		object capturedOriginalSource = null;

		var innerButton = new Button
		{
			Content = "Inner",
			Width = 60,
			Height = 30
		};

		var outerBorder = new Border
		{
			Child = innerButton,
			Width = 150,
			Height = 80,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Green)
		};

		outerBorder.ContextRequested += (sender, args) =>
		{
			capturedOriginalSource = args.OriginalSource;
			args.Handled = true;
		};

		await UITestHelper.Load(outerBorder);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");
		using var mouse = injector.GetMouse();

		var innerBounds = innerButton.GetAbsoluteBounds();

		// Right-click on inner button to trigger context menu
		mouse.PressRight(innerBounds.GetCenter());
		mouse.ReleaseRight();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNotNull(capturedOriginalSource, "OriginalSource should be set");
		// OriginalSource should be the innermost element that received the input
		Assert.IsTrue(innerButton.Equals(capturedOriginalSource) || VisualTreeUtils.FindVisualParentByType<Button>(capturedOriginalSource as DependencyObject) == innerButton,
			"OriginalSource should be the inner button where right-click occurred");
	}
}
#endif
