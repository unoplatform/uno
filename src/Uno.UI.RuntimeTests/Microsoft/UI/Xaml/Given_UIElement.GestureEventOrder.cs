using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.DevTools.Input;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Input.Preview.Injection;
#if HAS_UNO_WINUI
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

/// <summary>
/// Tapped / RightTapped (and the ContextRequested that follows RightTapped) are raised only once PointerReleased has bubbled to the root.
/// </summary>
[TestClass]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // The mouse injection of the WinUI test head misplaces the pointer
public partial class Given_UIElement_GestureEventOrder
{
	// The recognizer that raises Tapped / RightTapped belongs to the OWNER, while the pointer hits the INNER element below it (like a
	// picker control whose inner part is clicked). The OUTER element above observes PointerReleased bubbling.
	private static (Border outer, Border owner, Border inner) CreateTree()
	{
		var inner = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Red)
		};
		var owner = new Border
		{
			Background = new SolidColorBrush(Microsoft.UI.Colors.Blue),
			Padding = new Thickness(10),
			Child = inner
		};
		var outer = new Border
		{
			Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
			Padding = new Thickness(10),
			Child = owner
		};
		return (outer, owner, inner);
	}

	private static void TapCenter(PointerDeviceType type, FrameworkElement element)
	{
		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		var pointer = injector.GetPointer(type);
		using (pointer as IDisposable)
		{
			pointer.Tap(element.GetAbsoluteCenter());
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(PointerDeviceType.Mouse, false)]
	[DataRow(PointerDeviceType.Mouse, true)]
#if HAS_UNO // Touch injection is not supported by the WinUI test head
	[DataRow(PointerDeviceType.Touch, false)]
	[DataRow(PointerDeviceType.Touch, true)]
#endif
	public async Task When_Tapped_Then_Raised_After_PointerReleased_Bubbled(PointerDeviceType type, bool isTappedOnHitElement)
	{
		var (outer, owner, inner) = CreateTree();
		var order = new List<string>();

		inner.PointerReleased += (_, _) => order.Add("inner.PointerReleased");
		owner.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler((_, _) => order.Add("owner.PointerReleased")), handledEventsToo: true);
		outer.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler((_, _) => order.Add("outer.PointerReleased")), handledEventsToo: true);
		(isTappedOnHitElement ? inner : owner).Tapped += (_, _) => order.Add("Tapped");
		if (type is PointerDeviceType.Touch)
		{
			outer.PointerExited += (_, _) =>
			{
				if (!order.Contains("outer.PointerExited"))
				{
					order.Add("outer.PointerExited");
				}
			};
		}

		await UITestHelper.Load(outer);
		TapCenter(type, inner);
		await TestServices.WindowHelper.WaitForIdle();

		var expected = new List<string> { "inner.PointerReleased", "owner.PointerReleased", "outer.PointerReleased", "Tapped" };
		if (type is PointerDeviceType.Touch)
		{
			expected.Add("outer.PointerExited");
		}
		CollectionAssert.AreEqual(expected, order, "Tapped must be raised after PointerReleased has bubbled to the root: " + string.Join(", ", order));
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Tapped_With_Capture_Then_Raised_Before_PointerCaptureLost(bool isTappedOnHitElement)
	{
		var (outer, owner, inner) = CreateTree();
		var order = new List<string>();

		inner.PointerPressed += (_, e) => inner.CapturePointer(e.Pointer);
		inner.PointerCaptureLost += (_, _) => order.Add("inner.PointerCaptureLost");
		outer.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler((_, _) => order.Add("outer.PointerReleased")), handledEventsToo: true);
		(isTappedOnHitElement ? inner : owner).Tapped += (_, _) => order.Add("Tapped");

		await UITestHelper.Load(outer);
		TapCenter(PointerDeviceType.Mouse, inner);
		await TestServices.WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(
			new[] { "outer.PointerReleased", "Tapped", "inner.PointerCaptureLost" },
			order,
			"The capture must be released only after the Tapped: " + string.Join(", ", order));
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Tapped_Ancestor_Is_Not_Tapped_Twice()
	{
		var (outer, owner, inner) = CreateTree();
		var ownerTapped = 0;
		var outerTapped = 0;

		owner.Tapped += (_, _) => ownerTapped++;
		outer.Tapped += (_, _) => outerTapped++; // Receives the owner's Tapped by bubbling, its own recognizer must not raise it again

		await UITestHelper.Load(outer);
		TapCenter(PointerDeviceType.Mouse, inner);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(1, ownerTapped);
		Assert.AreEqual(1, outerTapped);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RightTapped_Then_Raised_After_PointerReleased_Bubbled_And_Before_ContextRequested()
	{
		var (outer, owner, inner) = CreateTree();
		var order = new List<string>();

		owner.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler((_, _) => order.Add("owner.PointerReleased")), handledEventsToo: true);
		outer.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler((_, _) => order.Add("outer.PointerReleased")), handledEventsToo: true);
		owner.RightTapped += (_, _) => order.Add("owner.RightTapped");
		owner.ContextRequested += (_, _) => order.Add("owner.ContextRequested");

		await UITestHelper.Load(outer);
		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using (var mouse = injector.GetMouse())
		{
			mouse.PressRight(inner.GetAbsoluteCenter());
			mouse.ReleaseRight();
		}
		await TestServices.WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(
			new[] { "owner.PointerReleased", "outer.PointerReleased", "owner.RightTapped", "owner.ContextRequested" },
			order,
			"RightTapped must be raised after PointerReleased has bubbled, then ContextRequested: " + string.Join(", ", order));
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // Touch injection is not supported by the WinUI test head
	public async Task When_Touch_Held_Then_RightTapped_Raised_After_PointerReleased_Bubbled()
	{
		var (outer, owner, inner) = CreateTree();
		var order = new List<string>();

		outer.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler((_, _) => order.Add("outer.PointerReleased")), handledEventsToo: true);
		owner.RightTapped += (_, _) => order.Add("owner.RightTapped");

		await UITestHelper.Load(outer);
		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using (var finger = injector.GetFinger())
		{
			var center = inner.GetAbsoluteCenter();
			finger.Press(center);
			finger.MoveTo(center, steps: 0, stepOffsetInMilliseconds: 1000); // Injected time, longer than the holding delay: the release is a right tap
			finger.Release();
		}
		await TestServices.WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(
			new[] { "outer.PointerReleased", "owner.RightTapped" },
			order,
			"RightTapped must be raised after PointerReleased has bubbled: " + string.Join(", ", order));
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // WinUI does not swallow errors of event handlers
	public async Task When_ContextRequested_Handler_Throws_Then_Release_Completes()
	{
		var (outer, owner, inner) = CreateTree();
		var order = new List<string>();

		inner.PointerPressed += (_, e) => inner.CapturePointer(e.Pointer);
		inner.PointerCaptureLost += (_, _) => order.Add("inner.PointerCaptureLost");
		owner.ContextRequested += (_, _) =>
		{
			order.Add("owner.ContextRequested");
			throw new InvalidOperationException("Error raised by the test on purpose.");
		};

		await UITestHelper.Load(outer);
		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using (var mouse = injector.GetMouse())
		{
			mouse.PressRight(inner.GetAbsoluteCenter());
			mouse.ReleaseRight();
		}
		await TestServices.WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(
			new[] { "owner.ContextRequested", "inner.PointerCaptureLost" },
			order,
			"The capture must still be released after a failing ContextRequested handler: " + string.Join(", ", order));
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(PointerDeviceType.Mouse)]
#if HAS_UNO // Touch injection is not supported by the WinUI test head
	[DataRow(PointerDeviceType.Touch)]
#endif
	public async Task When_Tapped_Handler_Moves_Focus_Outside_ScrollViewer_ScrollViewer_Does_Not_Steal_It(PointerDeviceType type)
	{
		// Mirrors a date picker inside a form: focus is outside the (default, non-tab-stop) ScrollViewer, and the tapped element
		// moves the focus (e.g. opens a flyout) in its Tapped handler. The ScrollViewer reclaims the focus on release (redirected to
		// its first focusable child): that must happen BEFORE the Tapped handler, otherwise it steals the focus the handler just set.
		var firstField = new TextBox { Header = "first" };
		var tappableInner = new Border { Height = 60, Background = new SolidColorBrush(Microsoft.UI.Colors.LightBlue) };
		var tappable = new Border { Child = tappableInner };
		var scrollViewer = new ScrollViewer
		{
			Width = 300,
			Height = 300,
			Content = new StackPanel { Children = { firstField, tappable } }
		};
		var focusTarget = new Button { Content = "focus target", Width = 150, Height = 40 };
		var initialFocus = new Button { Content = "initial focus", Width = 150, Height = 40 };
		tappable.Tapped += (_, _) => focusTarget.Focus(FocusState.Programmatic);

		await UITestHelper.Load(new StackPanel { Children = { initialFocus, focusTarget, scrollViewer } });
		Assert.IsFalse(scrollViewer.IsTabStop, "The default ScrollViewer is expected not to be a tab stop.");

		initialFocus.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreSame(initialFocus, FocusManager.GetFocusedElement(initialFocus.XamlRoot));

		TapCenter(type, tappableInner);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreSame(focusTarget, FocusManager.GetFocusedElement(focusTarget.XamlRoot), "The Tapped handler's focus change must win over the ScrollViewer's reclaim.");
	}
}
