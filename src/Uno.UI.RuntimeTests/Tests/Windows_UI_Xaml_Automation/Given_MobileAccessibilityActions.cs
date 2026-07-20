#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
public partial class Given_MobileAccessibilityActions
{
	[TestMethod]
	public void When_Toggle_Action_Requested_Then_Toggle_Provider_Is_Invoked()
	{
		var provider = new ToggleProvider();
		var peer = CreatePeer(PatternInterface.Toggle, provider);

		Assert.IsTrue(AccessibilityPeerHelper.TryToggle(peer));
		Assert.AreEqual(1, provider.ToggleCount);
	}

	[TestMethod]
	public void When_Selection_Actions_Requested_Then_Selection_Provider_Is_Invoked()
	{
		var provider = new SelectionItemProvider();
		var peer = CreatePeer(PatternInterface.SelectionItem, provider);

		Assert.IsTrue(AccessibilityPeerHelper.TrySelect(peer));
		Assert.IsTrue(AccessibilityPeerHelper.TryAddToSelection(peer));
		Assert.IsTrue(AccessibilityPeerHelper.TryRemoveFromSelection(peer));
		Assert.AreEqual(1, provider.SelectCount);
		Assert.AreEqual(1, provider.AddCount);
		Assert.AreEqual(1, provider.RemoveCount);
	}

	[TestMethod]
	public void When_Expand_And_Collapse_Requested_Then_Provider_State_Changes()
	{
		var provider = new ExpandCollapseProvider();
		var peer = CreatePeer(PatternInterface.ExpandCollapse, provider);

		Assert.IsTrue(AccessibilityPeerHelper.TryExpand(peer));
		Assert.AreEqual(ExpandCollapseState.Expanded, provider.ExpandCollapseState);
		Assert.AreEqual(1, provider.ExpandCount);
		Assert.IsTrue(AccessibilityPeerHelper.TryExpand(peer));
		Assert.AreEqual(2, provider.ExpandCount);
		Assert.IsTrue(AccessibilityPeerHelper.TryCollapse(peer));
		Assert.AreEqual(ExpandCollapseState.Collapsed, provider.ExpandCollapseState);
		Assert.AreEqual(1, provider.CollapseCount);
		Assert.IsTrue(AccessibilityPeerHelper.TryCollapse(peer));
		Assert.AreEqual(2, provider.CollapseCount);
	}

	[TestMethod]
	public void When_Range_Actions_Requested_Then_Value_Is_Clamped_And_Set()
	{
		var provider = new RangeValueProvider { Value = 5, Minimum = 0, Maximum = 10, SmallChange = 2 };
		var peer = CreatePeer(PatternInterface.RangeValue, provider);

		Assert.IsTrue(AccessibilityPeerHelper.TryIncrement(peer));
		Assert.AreEqual(7, provider.Value);
		Assert.IsTrue(AccessibilityPeerHelper.TryDecrement(peer));
		Assert.AreEqual(5, provider.Value);
		Assert.IsTrue(AccessibilityPeerHelper.TrySetRangeValue(peer, 50));
		Assert.AreEqual(10, provider.Value);
	}

	[TestMethod]
	public void When_Value_Is_ReadOnly_Then_Set_Value_Returns_False()
	{
		var provider = new ValueProvider { IsReadOnly = true };
		var peer = CreatePeer(PatternInterface.Value, provider);

		Assert.IsFalse(AccessibilityPeerHelper.TrySetValue(peer, "updated"));
		Assert.AreEqual(string.Empty, provider.Value);
	}

	[TestMethod]
	public void When_Scroll_Actions_Requested_Then_Scroll_Providers_Are_Invoked()
	{
		var scroll = new ScrollProvider();
		var item = new ScrollItemProvider();
		var peer = new TestPeer
		{
			Patterns =
			{
				[PatternInterface.Scroll] = scroll,
				[PatternInterface.ScrollItem] = item,
			},
		};

		Assert.IsTrue(AccessibilityPeerHelper.TryScroll(peer, ScrollAmount.NoAmount, ScrollAmount.SmallIncrement));
		Assert.AreEqual(ScrollAmount.SmallIncrement, scroll.VerticalAmount);
		Assert.IsTrue(AccessibilityPeerHelper.TrySetScrollPercent(peer, 25, 75));
		Assert.AreEqual(75, scroll.VerticalScrollPercent);
		Assert.IsTrue(AccessibilityPeerHelper.TryScrollIntoView(peer));
		Assert.IsTrue(item.WasInvoked);
	}

	[TestMethod]
	public void When_Realize_And_ChangeView_Requested_Then_Providers_Are_Invoked()
	{
		var virtualized = new VirtualizedItemProvider();
		var multipleView = new MultipleViewProvider();
		var peer = new TestPeer
		{
			Patterns =
			{
				[PatternInterface.VirtualizedItem] = virtualized,
				[PatternInterface.MultipleView] = multipleView,
			},
		};

		Assert.IsTrue(AccessibilityPeerHelper.TryRealize(peer));
		Assert.IsTrue(virtualized.WasInvoked);
		Assert.IsTrue(AccessibilityPeerHelper.TryChangeView(peer, 2));
		Assert.AreEqual(2, multipleView.CurrentView);
		Assert.IsFalse(AccessibilityPeerHelper.TryChangeView(peer, 99));
	}

	[TestMethod]
	public void When_Transform_Actions_Requested_Then_Transform_Provider_Is_Invoked()
	{
		var provider = new TransformProvider();
		var peer = CreatePeer(PatternInterface.Transform2, provider);

		Assert.IsTrue(AccessibilityPeerHelper.TryMove(peer, 10, 20));
		Assert.IsTrue(AccessibilityPeerHelper.TryResize(peer, 30, 40));
		Assert.IsTrue(AccessibilityPeerHelper.TryRotate(peer, 45));
		Assert.IsTrue(AccessibilityPeerHelper.TryZoom(peer, 150));
		Assert.IsTrue(AccessibilityPeerHelper.TryZoomByUnit(peer, ZoomUnit.SmallIncrement));
		Assert.AreEqual((10d, 20d), provider.LastMove);
		Assert.AreEqual(151, provider.ZoomLevel);
	}

	[TestMethod]
	public void When_Window_Actions_Requested_Then_Window_Provider_Is_Invoked()
	{
		var provider = new WindowProvider();
		var peer = CreatePeer(PatternInterface.Window, provider);

		Assert.IsTrue(AccessibilityPeerHelper.TrySetWindowVisualState(peer, WindowVisualState.Maximized));
		Assert.AreEqual(WindowVisualState.Maximized, provider.VisualState);
		Assert.IsTrue(AccessibilityPeerHelper.TryClose(peer));
		Assert.IsTrue(provider.WasClosed);
	}

	[TestMethod]
	public void When_Dock_Action_Requested_Then_Dock_Provider_Is_Invoked()
	{
		var provider = new DockProvider();
		var peer = CreatePeer(PatternInterface.Dock, provider);

		Assert.IsTrue(AccessibilityPeerHelper.TrySetDockPosition(peer, DockPosition.Right));
		Assert.AreEqual(DockPosition.Right, provider.DockPosition);
	}

	[TestMethod]
	public void When_Native_Pattern_Has_No_Transport_Then_Fallback_Is_Explicit()
	{
		var peer = new TestPeer
		{
			Patterns =
			{
				[PatternInterface.Drag] = new object(),
				[PatternInterface.TextRange] = new object(),
				[PatternInterface.ObjectModel] = new object(),
				[PatternInterface.SynchronizedInput] = new object(),
			},
		};

		var fallbacks = AccessibilityPeerHelper.GetFallbackDetails(peer);

		Assert.IsNotNull(fallbacks);
		CollectionAssert.Contains(fallbacks.InternalPatterns.ToArray(), PatternInterface.Drag);
		CollectionAssert.Contains(fallbacks.InternalPatterns.ToArray(), PatternInterface.TextRange);
		CollectionAssert.Contains(fallbacks.UnsupportedPatterns.ToArray(), PatternInterface.ObjectModel);
		CollectionAssert.Contains(fallbacks.UnsupportedPatterns.ToArray(), PatternInterface.SynchronizedInput);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaIOS)]
	public async Task When_Advanced_Actions_Are_Advertised_Then_Native_Hook_Executes_Providers()
	{
		var control = new AdvancedActionControl { Width = 100, Height = 100 };
		await UITestHelper.Load(control);

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(control);
		Assert.IsNotNull(snapshot?.Details, "The native node must expose advanced action details.");
		Assert.IsTrue(snapshot.Details.SupportedActions.Contains(AccessibilityNativeAction.ChangeView));
		Assert.IsTrue(snapshot.Details.SupportedActions.Contains(AccessibilityNativeAction.ZoomIn));
		Assert.IsTrue(snapshot.Details.SupportedActions.Contains(AccessibilityNativeAction.ZoomOut));
		Assert.IsTrue(snapshot.Details.SupportedActions.Contains(AccessibilityNativeAction.SetDockPosition));

		var execute =
			AccessibilityPeerHelper.AndroidAccessibilityActionAccessor
			?? AccessibilityPeerHelper.IOSAccessibilityActionAccessor;
		Assert.IsNotNull(execute, "The native action hook must be registered.");

		Assert.IsTrue(execute(control, new AccessibilityNativeActionRequest(
			AccessibilityNativeAction.ChangeView,
			number: 2)));
		Assert.AreEqual(2, control.Peer.CurrentView);

		Assert.IsTrue(execute(control, new AccessibilityNativeActionRequest(
			AccessibilityNativeAction.ZoomIn)));
		Assert.AreEqual(101, control.Peer.ZoomLevel);

		Assert.IsTrue(execute(control, new AccessibilityNativeActionRequest(
			AccessibilityNativeAction.SetDockPosition,
			number: (int)DockPosition.Right)));
		Assert.AreEqual(DockPosition.Right, control.Peer.DockPosition);

		Assert.IsTrue(execute(control, new AccessibilityNativeActionRequest(
			AccessibilityNativeAction.Move,
			number: 10,
			number2: 20)));
		Assert.AreEqual((10d, 20d), control.Peer.LastMove);
	}

	[TestMethod]
	public void When_Pattern_Is_Missing_Then_Action_Returns_False()
	{
		var peer = new TestPeer();

		Assert.IsFalse(AccessibilityPeerHelper.TryToggle(peer));
		Assert.IsFalse(AccessibilityPeerHelper.TryScrollIntoView(peer));
		Assert.IsFalse(AccessibilityPeerHelper.TryClose(peer));
	}

	[TestMethod]
	public void When_Provider_Reports_Disabled_Then_Action_Returns_False()
	{
		var peer = CreatePeer(PatternInterface.Toggle, new DisabledToggleProvider());

		Assert.IsFalse(AccessibilityPeerHelper.TryToggle(peer));
	}

	private static TestPeer CreatePeer(PatternInterface pattern, object provider)
		=> new() { Patterns = { [pattern] = provider } };

	private sealed class TestPeer : AutomationPeer
	{
		public Dictionary<PatternInterface, object> Patterns { get; } = new();

		protected override object? GetPatternCore(PatternInterface patternInterface)
			=> Patterns.GetValueOrDefault(patternInterface);
	}

	private sealed class ToggleProvider : IToggleProvider
	{
		public ToggleState ToggleState { get; private set; }

		public int ToggleCount { get; private set; }

		public void Toggle()
		{
			ToggleCount++;
			ToggleState = ToggleState == ToggleState.On ? ToggleState.Off : ToggleState.On;
		}

	}

	private sealed class DisabledToggleProvider : IToggleProvider
	{
		public ToggleState ToggleState => ToggleState.Off;

		public void Toggle() => throw new ElementNotEnabledException();
	}

	private sealed class SelectionItemProvider : ISelectionItemProvider
	{
		public bool IsSelected { get; private set; }

		public IRawElementProviderSimple SelectionContainer => null!;

		public int SelectCount { get; private set; }

		public int AddCount { get; private set; }

		public int RemoveCount { get; private set; }

		public void AddToSelection()
		{
			AddCount++;
			IsSelected = true;
		}

		public void RemoveFromSelection()
		{
			RemoveCount++;
			IsSelected = false;
		}

		public void Select()
		{
			SelectCount++;
			IsSelected = true;
		}
	}

	private sealed class ExpandCollapseProvider : IExpandCollapseProvider
	{
		public ExpandCollapseState ExpandCollapseState { get; private set; }

		public int ExpandCount { get; private set; }

		public int CollapseCount { get; private set; }

		public void Collapse()
		{
			CollapseCount++;
			ExpandCollapseState = ExpandCollapseState.Collapsed;
		}

		public void Expand()
		{
			ExpandCount++;
			ExpandCollapseState = ExpandCollapseState.Expanded;
		}
	}

	private sealed class RangeValueProvider : IRangeValueProvider
	{
		public bool IsReadOnly { get; set; }

		public double LargeChange { get; set; } = 5;

		public double Maximum { get; set; }

		public double Minimum { get; set; }

		public double SmallChange { get; set; }

		public double Value { get; set; }

		public void SetValue(double value) => Value = value;
	}

	private sealed class ValueProvider : IValueProvider
	{
		public bool IsReadOnly { get; set; }

		public string Value { get; private set; } = string.Empty;

		public void SetValue(string value) => Value = value;
	}

	private sealed class ScrollProvider : IScrollProvider
	{
		public double HorizontalScrollPercent { get; private set; }

		public double HorizontalViewSize => 50;

		public bool HorizontallyScrollable => true;

		public double VerticalScrollPercent { get; private set; }

		public double VerticalViewSize => 50;

		public bool VerticallyScrollable => true;

		public ScrollAmount HorizontalAmount { get; private set; }

		public ScrollAmount VerticalAmount { get; private set; }

		public void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
		{
			HorizontalAmount = horizontalAmount;
			VerticalAmount = verticalAmount;
		}

		public void SetScrollPercent(double horizontalPercent, double verticalPercent)
		{
			HorizontalScrollPercent = horizontalPercent;
			VerticalScrollPercent = verticalPercent;
		}
	}

	private sealed class ScrollItemProvider : IScrollItemProvider
	{
		public bool WasInvoked { get; private set; }

		public void ScrollIntoView() => WasInvoked = true;
	}

	private sealed class VirtualizedItemProvider : IVirtualizedItemProvider
	{
		public bool WasInvoked { get; private set; }

		public void Realize() => WasInvoked = true;
	}

	private sealed class MultipleViewProvider : IMultipleViewProvider
	{
		public int CurrentView { get; private set; } = 1;

		public int[] GetSupportedViews() => new[] { 1, 2 };

		public string GetViewName(int viewId) => viewId.ToString();

		public void SetCurrentView(int viewId) => CurrentView = viewId;
	}

	private sealed class TransformProvider : ITransformProvider2
	{
		public bool CanMove => true;

		public bool CanResize => true;

		public bool CanRotate => true;

		public bool CanZoom => true;

		public double MaxZoom => 400;

		public double MinZoom => 25;

		public double ZoomLevel { get; private set; } = 100;

		public (double X, double Y) LastMove { get; private set; }

		public void Move(double x, double y) => LastMove = (x, y);

		public void Resize(double width, double height) { }

		public void Rotate(double degrees) { }

		public void Zoom(double zoom) => ZoomLevel = zoom;

		public void ZoomByUnit(ZoomUnit zoomUnit) => ZoomLevel++;
	}

	private sealed class WindowProvider : IWindowProvider
	{
		public WindowInteractionState InteractionState => WindowInteractionState.ReadyForUserInteraction;

		public bool IsModal => false;

		public bool IsTopmost => false;

		public bool Maximizable => true;

		public bool Minimizable => true;

		public WindowVisualState VisualState { get; private set; }

		public bool WasClosed { get; private set; }

		public void Close() => WasClosed = true;

		public void SetVisualState(WindowVisualState state) => VisualState = state;

		public bool WaitForInputIdle(int milliseconds) => true;
	}

	private sealed class DockProvider : IDockProvider
	{
		public DockPosition DockPosition { get; private set; }

		public void SetDockPosition(DockPosition dockPosition)
			=> DockPosition = dockPosition;
	}

	private sealed partial class AdvancedActionControl : Grid
	{
		internal AdvancedActionPeer Peer { get; private set; } = null!;

		protected override AutomationPeer OnCreateAutomationPeer()
			=> Peer = new AdvancedActionPeer(this);
	}

	private sealed class AdvancedActionPeer :
		FrameworkElementAutomationPeer,
		IMultipleViewProvider,
		ITransformProvider2,
		IDockProvider
	{
		internal AdvancedActionPeer(FrameworkElement owner)
			: base(owner)
		{
		}

		public int CurrentView { get; private set; } = 1;

		public bool CanMove => true;

		public bool CanResize => true;

		public bool CanRotate => true;

		public bool CanZoom => true;

		public double MaxZoom => 400;

		public double MinZoom => 25;

		public double ZoomLevel { get; private set; } = 100;

		public DockPosition DockPosition { get; private set; }

		public (double X, double Y) LastMove { get; private set; }

		protected override bool IsControlElementCore() => true;

		protected override bool IsContentElementCore() => true;

		protected override string GetNameCore() => "Advanced action control";

		protected override object? GetPatternCore(PatternInterface patternInterface)
			=> patternInterface is PatternInterface.MultipleView
				or PatternInterface.Transform
				or PatternInterface.Transform2
				or PatternInterface.Dock
					? this
					: base.GetPatternCore(patternInterface);

		public int[] GetSupportedViews() => [1, 2];

		public string GetViewName(int viewId) => $"View {viewId}";

		public void SetCurrentView(int viewId) => CurrentView = viewId;

		public void Move(double x, double y) => LastMove = (x, y);

		public void Resize(double width, double height) { }

		public void Rotate(double degrees) { }

		public void Zoom(double zoom) => ZoomLevel = zoom;

		public void ZoomByUnit(ZoomUnit zoomUnit)
			=> ZoomLevel += zoomUnit == ZoomUnit.SmallDecrement ? -1 : 1;

		public void SetDockPosition(DockPosition dockPosition)
			=> DockPosition = dockPosition;
	}
}
