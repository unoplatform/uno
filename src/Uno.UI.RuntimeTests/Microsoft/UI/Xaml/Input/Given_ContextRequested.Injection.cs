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
using Uno.UI.DevTools.Input;
using Uno.UI.Extensions;
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

	// The context menu of a touch hold on a draggable element is delayed (ContextMenuProcessor) to leave room for a drag:
	// a drag started from the hold supersedes it, so it must not open in the middle of the drag.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22229")]
	public async Task When_Touch_Hold_Starts_Drag_Delayed_ContextFlyout_Does_Not_Open()
	{
		var flyout = new MenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Item" });
		var flyoutOpened = 0;
		flyout.Opened += (_, _) => flyoutOpened++;

		var holding = 0;
		var dragStarting = 0;
		var dropCompleted = 0;
		var target = new Border
		{
			Background = new SolidColorBrush(Microsoft.UI.Colors.Blue),
			Width = 100,
			Height = 100,
			CanDrag = true,
			ContextFlyout = flyout,
		};
		target.Holding += (_, _) => holding++;
		target.DragStarting += (_, _) => dragStarting++;
		target.DropCompleted += (_, _) => dropCompleted++;

		await UITestHelper.Load(new Grid { Width = 400, Height = 300, Children = { target } });

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");
		using var finger = injector.GetFinger();

		try
		{
			finger.Press(target.GetAbsoluteBounds().GetCenter());
			await TestServices.WindowHelper.WaitFor(() => holding == 1, timeoutMS: 5000, message: "The hold should be recognized");
			Assert.AreEqual(0, flyoutOpened, "The context flyout of a hold on a draggable element is delayed");

			// Injected timestamps only advance by the given offsets: carry the hold's duration to the gesture recognizer,
			// which only starts a touch drag once the finger has rested past its hold delay.
			finger.MoveBy(1, 0, steps: 1, stepOffsetInMilliseconds: 1000);
			finger.MoveBy(100, 0, steps: 10, stepOffsetInMilliseconds: 10);
			await TestServices.WindowHelper.WaitFor(() => dragStarting == 1, timeoutMS: 5000, message: "Moving after the hold should start a drag");

			// Longer than what remained of the delay: a menu still pending would open here.
			await Task.Delay(800);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(0, flyoutOpened, "The context flyout must not open once a drag started from the hold");
		}
		finally
		{
			finger.Release();
			// Let the drag operation end, so it doesn't linger into the next tests.
			await TestServices.WindowHelper.WaitFor(() => dragStarting == 0 || dropCompleted == 1, timeoutMS: 5000);
			VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
		}
	}

	// As in WinUI (PointerInputProcessor.cpp, XCP_POINTERDOWN), a second touch pressed while the delayed context menu of a
	// hold on a pannable element is pending drops that menu.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22229")]
	public async Task When_Touch_Hold_Then_Second_Finger_Pressed_Delayed_ContextFlyout_Does_Not_Open()
	{
		var flyout = new MenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Item" });
		var flyoutOpened = 0;
		flyout.Opened += (_, _) => flyoutOpened++;

		var holding = 0;
		var target = new Border
		{
			Background = new SolidColorBrush(Microsoft.UI.Colors.Blue),
			Width = 100,
			Height = 50,
			ContextFlyout = flyout,
		};
		target.Holding += (_, _) => holding++;
		var other = new Border { Background = new SolidColorBrush(Microsoft.UI.Colors.Green), Width = 100, Height = 50 };

		await UITestHelper.Load(new StackPanel { Children = { new ScrollViewer { Content = target, Width = 200, Height = 100 }, other } });

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");
		using var finger1 = injector.GetFinger(id: 11);
		using var finger2 = injector.GetFinger(id: 22);

		try
		{
			finger1.Press(target.GetAbsoluteBounds().GetCenter());
			await TestServices.WindowHelper.WaitFor(() => holding == 1, timeoutMS: 5000, message: "The hold should be recognized");
			Assert.AreEqual(0, flyoutOpened, "The context flyout of a hold on a pannable element is delayed");

			// Kept pressed, as any release also drops the delayed menu.
			finger2.Press(other.GetAbsoluteBounds().GetCenter());

			// Longer than what remained of the 500 ms delay, shorter than the second finger's own hold: a menu still pending
			// would open here.
			await Task.Delay(600);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(0, flyoutOpened, "A second touch pressed during the delay must drop the delayed context flyout");
		}
		finally
		{
			finger2.Release();
			finger1.Release();
			await TestServices.WindowHelper.WaitForIdle();
			VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
		}
	}

	// Two fingers pressed less than 500 ms apart: the second press comes before the first hold starts the delayed context menu,
	// so it is the second hold that must drop it (ContextMenuProcessor.ProcessContextRequestOnHoldingGesture).
	// Each hold comes from its finger's 800 ms hold timer and takes its source from the last pointer event, so each finger
	// moves within the tap range before its hold. An attempt where the first hold's request is no longer pending at the second
	// hold, or where a hold got the other finger as source, says nothing about the regression: it is retried, a few times.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22229")]
	public async Task When_Two_Fingers_Held_Second_Hold_Drops_First_Delayed_ContextFlyout()
	{
		const int maxAttempts = 3;

		var flyout = new MenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Item" });
		var flyoutOpened = 0;
		flyout.Opened += (_, _) => flyoutOpened++;

		var target = new Border
		{
			Background = new SolidColorBrush(Microsoft.UI.Colors.Blue),
			Width = 100,
			Height = 50,
			ContextFlyout = flyout,
		};
		var other = new Border { Background = new SolidColorBrush(Microsoft.UI.Colors.Green), Width = 100, Height = 50 };

		await UITestHelper.Load(new StackPanel { Children = { new ScrollViewer { Content = target, Width = 200, Height = 100 }, other } });

		var processor = TestServices.WindowHelper.XamlRoot.VisualTree?.ContentRoot?.InputManager?.ContextMenuProcessor
			?? throw new InvalidOperationException("ContextMenuProcessor is not available.");
		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");

		object firstHoldSource = null, secondHoldSource = null;
		var firstRequestPendingAtSecondHold = false;
		target.Holding += (_, e) =>
		{
			if (e.HoldingState == Microsoft.UI.Input.HoldingState.Started)
			{
				firstHoldSource ??= e.OriginalSource;
			}
		};
		other.Holding += (_, e) =>
		{
			if (e.HoldingState == Microsoft.UI.Input.HoldingState.Started && secondHoldSource is null)
			{
				secondHoldSource = e.OriginalSource;
				// Raised before the ContextMenuProcessor handles this hold.
				firstRequestPendingAtSecondHold = processor.GetContextMenuTimer() is { IsEnabled: true } timer && timer.TargetObject == target;
			}
		};

		for (var attempt = 1; ; attempt++)
		{
			flyoutOpened = 0;
			firstHoldSource = secondHoldSource = null;
			firstRequestPendingAtSecondHold = false;

			using var finger1 = injector.GetFinger(id: 11);
			using var finger2 = injector.GetFinger(id: 22);
			try
			{
				finger1.Press(target.GetAbsoluteBounds().GetCenter());
				await Task.Delay(250);
				finger2.Press(other.GetAbsoluteBounds().GetCenter());
				finger1.MoveBy(1, 0, steps: 1);

				await TestServices.WindowHelper.WaitFor(() => firstHoldSource is not null, timeoutMS: 5000, message: "The first hold should be recognized");
				finger2.MoveBy(1, 0, steps: 1);
				await TestServices.WindowHelper.WaitFor(() => secondHoldSource is not null, timeoutMS: 5000, message: "The second hold should be recognized");

				if (firstHoldSource == target && secondHoldSource == other && firstRequestPendingAtSecondHold)
				{
					// Longer than the 500 ms delay of the first hold's request: a request still pending would open the flyout here.
					await Task.Delay(700);
					await TestServices.WindowHelper.WaitForIdle();

					Assert.AreEqual(0, flyoutOpened, "The second hold must drop the first hold's delayed context flyout");
					return;
				}
			}
			finally
			{
				finger2.Release();
				finger1.Release();
				await TestServices.WindowHelper.WaitForIdle();
				VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
				await TestServices.WindowHelper.WaitForIdle();
			}

			if (attempt == maxAttempts)
			{
				string Describe(object source) => source == target ? "first finger's element" : source == other ? "second finger's element" : source?.ToString() ?? "none";
				Assert.Fail(
					$"In {maxAttempts} attempts, the second hold never came while the first hold's request was pending. Last attempt: "
					+ $"first hold source: {Describe(firstHoldSource)}, second hold source: {Describe(secondHoldSource)}, "
					+ $"first request pending at the second hold: {firstRequestPendingAtSecondHold}.");
			}
		}
	}
}
#endif
