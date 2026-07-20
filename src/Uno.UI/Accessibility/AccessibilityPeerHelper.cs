#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Windows.Foundation;

namespace Uno.UI;

internal sealed class AccessibilityPeerNode
{
	internal AccessibilityPeerNode(
		AutomationPeer peer,
		AutomationPeer providerPeer,
		UIElement? owner,
		int? parentIndex,
		int depth)
	{
		Peer = peer;
		ProviderPeer = providerPeer;
		Owner = owner;
		ParentIndex = parentIndex;
		Depth = depth;
	}

	internal AutomationPeer Peer { get; }

	internal AutomationPeer ProviderPeer { get; }

	internal UIElement? Owner { get; }

	internal int? ParentIndex { get; }

	internal int Depth { get; }
}

[Flags]
internal enum AccessibilityNativeTraits
{
	None = 0,
	Button = 1 << 0,
	StaticText = 1 << 1,
	NotEnabled = 1 << 2,
	Adjustable = 1 << 3,
	Link = 1 << 4,
	Image = 1 << 5,
	Header = 1 << 6,
}

internal enum AccessibilityNativeAction
{
	Activate,
	Expand,
	Collapse,
	Increment,
	Decrement,
	SetRangeValue,
	SetValue,
	ScrollForward,
	ScrollBackward,
	ScrollIntoView,
	Realize,
	Dismiss,
	ChangeView,
	Move,
	Resize,
	Rotate,
	Zoom,
	ZoomIn,
	ZoomOut,
	SetDockPosition,
	SetWindowVisualState,
}

internal enum AccessibilityNativeEventKind
{
	NodeInvalidated,
	StructureChanged,
	Announcement,
	TextChanged,
	SelectionChanged,
	WindowChanged,
}

internal sealed class AccessibilityNativeEventRecord
{
	internal AccessibilityNativeEventRecord(
		AccessibilityNativeEventKind kind,
		string? name = null,
		string? text = null)
	{
		Kind = kind;
		Name = name;
		Text = text;
	}

	internal AccessibilityNativeEventKind Kind { get; }

	internal string? Name { get; }

	internal string? Text { get; }
}

internal sealed class AccessibilityNativeActionRequest
{
	internal AccessibilityNativeActionRequest(
		AccessibilityNativeAction action,
		double number = 0,
		string? text = null,
		double number2 = 0)
	{
		Action = action;
		Number = number;
		Number2 = number2;
		Text = text;
	}

	internal AccessibilityNativeAction Action { get; }

	internal double Number { get; }

	internal double Number2 { get; }

	internal string? Text { get; }
}

internal sealed class AccessibilityNativeRangeDetails
{
	internal AccessibilityNativeRangeDetails(
		double value,
		double minimum,
		double maximum,
		double smallChange,
		double largeChange,
		bool isReadOnly,
		AutomationOrientation orientation)
	{
		Value = value;
		Minimum = minimum;
		Maximum = maximum;
		SmallChange = smallChange;
		LargeChange = largeChange;
		IsReadOnly = isReadOnly;
		Orientation = orientation;
	}

	internal double Value { get; }

	internal double Minimum { get; }

	internal double Maximum { get; }

	internal double SmallChange { get; }

	internal double LargeChange { get; }

	internal bool IsReadOnly { get; }

	internal AutomationOrientation Orientation { get; }
}

internal sealed class AccessibilityNativeTextStateDetails
{
	internal AccessibilityNativeTextStateDetails(
		bool isEditable,
		bool isReadOnly,
		bool isMultiline,
		bool hasTextSelection)
	{
		IsEditable = isEditable;
		IsReadOnly = isReadOnly;
		IsMultiline = isMultiline;
		HasTextSelection = hasTextSelection;
	}

	internal bool IsEditable { get; }

	internal bool IsReadOnly { get; }

	internal bool IsMultiline { get; }

	internal bool HasTextSelection { get; }
}

internal sealed class AccessibilityNativeScrollDetails
{
	internal AccessibilityNativeScrollDetails(
		bool isHorizontallyScrollable,
		bool isVerticallyScrollable,
		double horizontalScrollPercent,
		double verticalScrollPercent,
		double horizontalViewSize,
		double verticalViewSize)
	{
		IsHorizontallyScrollable = isHorizontallyScrollable;
		IsVerticallyScrollable = isVerticallyScrollable;
		HorizontalScrollPercent = horizontalScrollPercent;
		VerticalScrollPercent = verticalScrollPercent;
		HorizontalViewSize = horizontalViewSize;
		VerticalViewSize = verticalViewSize;
	}

	internal bool IsHorizontallyScrollable { get; }

	internal bool IsVerticallyScrollable { get; }

	internal double HorizontalScrollPercent { get; }

	internal double VerticalScrollPercent { get; }

	internal double HorizontalViewSize { get; }

	internal double VerticalViewSize { get; }
}

internal sealed class AccessibilityNativeCollectionDetails
{
	internal AccessibilityNativeCollectionDetails(
		int rowCount,
		int columnCount,
		bool canSelectMultiple,
		bool isSelectionRequired)
	{
		RowCount = rowCount;
		ColumnCount = columnCount;
		CanSelectMultiple = canSelectMultiple;
		IsSelectionRequired = isSelectionRequired;
	}

	internal int RowCount { get; }

	internal int ColumnCount { get; }

	internal bool CanSelectMultiple { get; }

	internal bool IsSelectionRequired { get; }
}

internal sealed class AccessibilityNativeCollectionItemDetails
{
	internal AccessibilityNativeCollectionItemDetails(
		int row,
		int column,
		int rowSpan,
		int columnSpan)
	{
		Row = row;
		Column = column;
		RowSpan = rowSpan;
		ColumnSpan = columnSpan;
	}

	internal int Row { get; }

	internal int Column { get; }

	internal int RowSpan { get; }

	internal int ColumnSpan { get; }
}

internal sealed class AccessibilityNativeHierarchyDetails
{
	internal AccessibilityNativeHierarchyDetails(
		int positionInSet,
		int sizeOfSet,
		int level)
	{
		PositionInSet = positionInSet;
		SizeOfSet = sizeOfSet;
		Level = level;
	}

	internal int PositionInSet { get; }

	internal int SizeOfSet { get; }

	internal int Level { get; }
}

/// <summary>
/// Relation targets projected as AutomationId strings.
/// Properties are never null; an empty list means the relation has no targets.
/// </summary>
internal sealed class AccessibilityNativeRelationDetails
{
	internal AccessibilityNativeRelationDetails(
		IReadOnlyList<string>? labeledByIds = null,
		IReadOnlyList<string>? describedByIds = null,
		IReadOnlyList<string>? controlledPeerIds = null,
		IReadOnlyList<string>? flowsFromIds = null,
		IReadOnlyList<string>? flowsToIds = null,
		IReadOnlyList<string>? annotationTypeNames = null)
	{
		LabeledByIds = labeledByIds ?? Array.Empty<string>();
		DescribedByIds = describedByIds ?? Array.Empty<string>();
		ControlledPeerIds = controlledPeerIds ?? Array.Empty<string>();
		FlowsFromIds = flowsFromIds ?? Array.Empty<string>();
		FlowsToIds = flowsToIds ?? Array.Empty<string>();
		AnnotationTypeNames = annotationTypeNames ?? Array.Empty<string>();
	}

	internal IReadOnlyList<string> LabeledByIds { get; }

	internal IReadOnlyList<string> DescribedByIds { get; }

	internal IReadOnlyList<string> ControlledPeerIds { get; }

	internal IReadOnlyList<string> FlowsFromIds { get; }

	internal IReadOnlyList<string> FlowsToIds { get; }

	/// <summary>Annotation type names (e.g. "Comment", "SpellingError").</summary>
	internal IReadOnlyList<string> AnnotationTypeNames { get; }
}

internal sealed class AccessibilityNativeFallbackDetails
{
	internal AccessibilityNativeFallbackDetails(
		IReadOnlyList<PatternInterface>? internalPatterns = null,
		IReadOnlyList<PatternInterface>? unsupportedPatterns = null)
	{
		InternalPatterns = internalPatterns ?? Array.Empty<PatternInterface>();
		UnsupportedPatterns = unsupportedPatterns ?? Array.Empty<PatternInterface>();
	}

	internal IReadOnlyList<PatternInterface> InternalPatterns { get; }

	internal IReadOnlyList<PatternInterface> UnsupportedPatterns { get; }
}

/// <summary>
/// Optional rich semantics attached to a native node snapshot.
/// Each sub-object is null when its pattern is not applicable to the element.
/// Set via the <c>details</c> parameter of <see cref="AccessibilityNativeNodeSnapshot"/>
/// or via <see cref="AccessibilityNativeNodeSnapshot.WithDetails"/>.
/// </summary>
internal sealed class AccessibilityNativeNodeDetails
{
	internal AccessibilityNativeNodeDetails(
		IReadOnlyList<AccessibilityNativeAction>? supportedActions = null,
		AccessibilityNativeRangeDetails? range = null,
		AccessibilityNativeTextStateDetails? textState = null,
		AccessibilityNativeScrollDetails? scroll = null,
		AccessibilityNativeCollectionDetails? collection = null,
		AccessibilityNativeCollectionItemDetails? collectionItem = null,
		AccessibilityNativeHierarchyDetails? hierarchy = null,
		AccessibilityNativeRelationDetails? relations = null,
		string? itemStatus = null,
		string? itemType = null,
		string? localizedControlType = null,
		string? fullDescription = null,
		bool? isRequiredForForm = null,
		bool? isDataValidForForm = null,
		int? culture = null,
		AutomationLandmarkType? landmarkType = null,
		string? localizedLandmarkType = null,
		AccessibilityNativeFallbackDetails? fallbacks = null)
	{
		SupportedActions = supportedActions ?? Array.Empty<AccessibilityNativeAction>();
		Range = range;
		TextState = textState;
		Scroll = scroll;
		Collection = collection;
		CollectionItem = collectionItem;
		Hierarchy = hierarchy;
		Relations = relations;
		ItemStatus = itemStatus;
		ItemType = itemType;
		LocalizedControlType = localizedControlType;
		FullDescription = fullDescription;
		IsRequiredForForm = isRequiredForForm;
		IsDataValidForForm = isDataValidForForm;
		Culture = culture;
		LandmarkType = landmarkType;
		LocalizedLandmarkType = localizedLandmarkType;
		Fallbacks = fallbacks;
	}

	internal IReadOnlyList<AccessibilityNativeAction> SupportedActions { get; }

	internal AccessibilityNativeRangeDetails? Range { get; }

	internal AccessibilityNativeTextStateDetails? TextState { get; }

	internal AccessibilityNativeScrollDetails? Scroll { get; }

	internal AccessibilityNativeCollectionDetails? Collection { get; }

	internal AccessibilityNativeCollectionItemDetails? CollectionItem { get; }

	internal AccessibilityNativeHierarchyDetails? Hierarchy { get; }

	internal AccessibilityNativeRelationDetails? Relations { get; }

	internal string? ItemStatus { get; }

	internal string? ItemType { get; }

	internal string? LocalizedControlType { get; }

	internal string? FullDescription { get; }

	/// <summary>Maps to <see cref="AutomationProperties.IsRequiredForFormProperty"/>.</summary>
	internal bool? IsRequiredForForm { get; }

	/// <summary>Maps to <see cref="AutomationProperties.IsDataValidForFormProperty"/>.</summary>
	internal bool? IsDataValidForForm { get; }

	/// <summary>LCID from <see cref="AutomationProperties.CultureProperty"/>.</summary>
	internal int? Culture { get; }

	internal AutomationLandmarkType? LandmarkType { get; }

	internal string? LocalizedLandmarkType { get; }

	internal AccessibilityNativeFallbackDetails? Fallbacks { get; }
}

internal sealed class AccessibilityNativeNodeSnapshot
{
	internal AccessibilityNativeNodeSnapshot(
		object nativeNode,
		string? name,
		string? className,
		string? hint,
		string? value,
		string? automationId,
		bool enabled,
		bool heading,
		bool password,
		bool checkable,
		bool? isChecked,
		AccessibilityNativeTraits traits,
		Rect bounds,
		AccessibilityNativeNodeDetails? details = null,
		string? nativeAutomationId = null,
		string? stateDescription = null,
		string? nativeRoleDescription = null)
	{
		NativeNode = nativeNode;
		Name = name;
		ClassName = className;
		Hint = hint;
		Value = value;
		AutomationId = automationId;
		Enabled = enabled;
		Heading = heading;
		Password = password;
		Checkable = checkable;
		IsChecked = isChecked;
		Traits = traits;
		Bounds = bounds;
		Details = details;
		NativeAutomationId = nativeAutomationId ?? automationId;
		StateDescription = stateDescription;
		NativeRoleDescription = nativeRoleDescription;
	}

	internal object NativeNode { get; }

	internal string? Name { get; }

	internal string? ClassName { get; }

	internal string? Hint { get; }

	internal string? Value { get; }

	internal string? AutomationId { get; }

	internal string? NativeAutomationId { get; }

	internal string? StateDescription { get; }

	internal string? NativeRoleDescription { get; }

	internal bool Enabled { get; }

	internal bool Heading { get; }

	internal bool Password { get; }

	internal bool Checkable { get; }

	internal bool? IsChecked { get; }

	internal AccessibilityNativeTraits Traits { get; }

	internal Rect Bounds { get; }

	/// <summary>
	/// Optional rich semantics projected by the native adapter.
	/// </summary>
	internal AccessibilityNativeNodeDetails? Details { get; }

	/// <summary>
	/// Returns a copy of this snapshot with the given details attached.
	/// </summary>
	internal AccessibilityNativeNodeSnapshot WithDetails(AccessibilityNativeNodeDetails details)
		=> new AccessibilityNativeNodeSnapshot(
			NativeNode, Name, ClassName, Hint, Value, AutomationId,
			Enabled, Heading, Password, Checkable, IsChecked, Traits, Bounds, details, NativeAutomationId, StateDescription, NativeRoleDescription);
}

internal static class AccessibilityPeerHelper
{
	private const int MaxTreeDepth = 1000;

	// ── Narrow internal test-access hooks ─────────────────────────
	// Set by the iOS Skia runtime when it constructs its per-window adapter.
	// The accessor receives a UIElement and returns the matching
	// UIAccessibilityElement as object? so this file stays platform-neutral.
	// Tests cast the return value to UIKit.UIAccessibilityElement inside #if __IOS__.

	/// <summary>
	/// Returns the stable native UIAccessibilityElement for the given owner element,
	/// or null if no element is registered. Set by AppleUIKitAccessibility.
	/// </summary>
	internal static Func<UIElement, object?>? IOSAccessibilityElementAccessor { get; set; }

	/// <summary>
	/// Returns the count of registered native elements for a given XamlRoot.
	/// Set by AppleUIKitAccessibility.
	/// </summary>
	internal static Func<XamlRoot, int>? IOSAccessibilityElementCountAccessor { get; set; }

	/// <summary>
	/// Returns the ordered accessibility elements for the given XamlRoot as object[]
	/// (elements are UIAccessibilityElement on iOS). Set by AppleUIKitAccessibility.
	/// </summary>
	internal static Func<XamlRoot, object[]?>? IOSAllElementsForRootAccessor { get; set; }

	internal static Func<UIElement, object?>? AndroidAccessibilityNodeAccessor { get; set; }

	internal static Func<UIElement, int?>? AndroidAccessibilityVirtualIdAccessor { get; set; }

	internal static Func<XamlRoot, object[]?>? AndroidAllNodesForRootAccessor { get; set; }

	internal static Func<UIElement, AccessibilityNativeNodeSnapshot?>? IOSAccessibilityNodeSnapshotAccessor { get; set; }

	internal static Func<XamlRoot, AccessibilityNativeNodeSnapshot[]?>? IOSAllNodeSnapshotsForRootAccessor { get; set; }

	internal static Func<UIElement, AccessibilityNativeNodeSnapshot?>? AndroidAccessibilityNodeSnapshotAccessor { get; set; }

	internal static Func<XamlRoot, AccessibilityNativeNodeSnapshot[]?>? AndroidAllNodeSnapshotsForRootAccessor { get; set; }

	internal static Func<XamlRoot, string>? AndroidAccessibilityDiagnosticsAccessor { get; set; }

	internal static Func<UIElement, AccessibilityNativeActionRequest, bool>? AndroidAccessibilityActionAccessor { get; set; }

	internal static Func<int, int, bool>? AndroidAccessibilityRawActionAccessor { get; set; }

	internal static Func<UIElement, AccessibilityNativeActionRequest, bool>? IOSAccessibilityActionAccessor { get; set; }

	internal static Func<UIElement, bool>? AndroidAccessibilityFocusAccessor { get; set; }

	internal static Func<XamlRoot, object?>? AndroidFocusedNativeNodeAccessor { get; set; }

	internal static Func<UIElement, bool>? IOSAccessibilityFocusAccessor { get; set; }

	internal static Func<XamlRoot, object?>? IOSFocusedNativeNodeAccessor { get; set; }

	internal static Func<XamlRoot, AccessibilityNativeEventRecord[]?>? AndroidAccessibilityEventsAccessor { get; set; }

	internal static Action<XamlRoot>? AndroidClearAccessibilityEventsAction { get; set; }

	internal static Func<XamlRoot, AccessibilityNativeEventRecord[]?>? IOSAccessibilityEventsAccessor { get; set; }

	internal static Action<XamlRoot>? IOSClearAccessibilityEventsAction { get; set; }

	internal static IReadOnlyList<AccessibilityPeerNode> GetPeerTree(AutomationPeer root)
	{
		ArgumentNullException.ThrowIfNull(root);

		var nodes = new List<AccessibilityPeerNode>();
		var visited = new HashSet<AutomationPeer>(ReferenceEqualityComparer.Instance);
		AppendPeer(root, parentIndex: null, depth: 0, nodes, visited);
		return nodes;
	}

	internal static IReadOnlyList<AccessibilityPeerNode> GetPeerTree(UIElement root)
	{
		ArgumentNullException.ThrowIfNull(root);

		var nodes = new List<AccessibilityPeerNode>();
		var visitedPeers = new HashSet<AutomationPeer>(ReferenceEqualityComparer.Instance);
		var visitedElements = new HashSet<UIElement>(ReferenceEqualityComparer.Instance);
		AppendElement(root, parentIndex: null, depth: 0, nodes, visitedPeers, visitedElements);
		return nodes;
	}

	internal static AutomationPeer ResolveProviderPeer(AutomationPeer peer)
	{
		ArgumentNullException.ThrowIfNull(peer);
		return peer.ResolveProviderPeer(resolveEventsSource: true);
	}

	internal static bool TryInvokeDefaultAction(AutomationPeer peer)
		=> TryPerform(() => ResolveProviderPeer(peer).InvokeAutomationPeer());

	internal static bool TryToggle(AutomationPeer peer)
		=> TryPerformProvider<IToggleProvider>(peer, PatternInterface.Toggle, static provider => provider.Toggle());

	internal static bool TrySelect(AutomationPeer peer)
		=> TryPerformProvider<ISelectionItemProvider>(peer, PatternInterface.SelectionItem, static provider => provider.Select());

	internal static bool TryAddToSelection(AutomationPeer peer)
		=> TryPerformProvider<ISelectionItemProvider>(peer, PatternInterface.SelectionItem, static provider => provider.AddToSelection());

	internal static bool TryRemoveFromSelection(AutomationPeer peer)
		=> TryPerformProvider<ISelectionItemProvider>(peer, PatternInterface.SelectionItem, static provider => provider.RemoveFromSelection());

	internal static bool TryToggleSelection(AutomationPeer peer)
	{
		var providerPeer = ResolveProviderPeer(peer);
		if (!providerPeer.IsEnabled() ||
			GetProvider<ISelectionItemProvider>(providerPeer, PatternInterface.SelectionItem) is not { } provider)
		{
			return false;
		}

		var selectionContainer = providerPeer.GetParent();
		var canSelectMultiple =
			selectionContainer?.GetPattern(PatternInterface.Selection) is ISelectionProvider
			{
				CanSelectMultiple: true,
			};

		if (!canSelectMultiple)
		{
			return TryPerform(provider.Select);
		}

		return provider.IsSelected
			? TryPerform(provider.RemoveFromSelection)
			: TryPerform(provider.AddToSelection);
	}

	internal static bool TryExpand(AutomationPeer peer)
	{
		var provider = GetProvider<IExpandCollapseProvider>(peer, PatternInterface.ExpandCollapse);
		if (provider is null)
		{
			return false;
		}

		return TryPerform(provider.Expand);
	}

	internal static bool TryCollapse(AutomationPeer peer)
	{
		var provider = GetProvider<IExpandCollapseProvider>(peer, PatternInterface.ExpandCollapse);
		if (provider is null)
		{
			return false;
		}

		return TryPerform(provider.Collapse);
	}

	internal static bool TryIncrement(AutomationPeer peer)
		=> TryAdjustRange(peer, increment: true);

	internal static bool TryDecrement(AutomationPeer peer)
		=> TryAdjustRange(peer, increment: false);

	internal static bool TrySetRangeValue(AutomationPeer peer, double value)
	{
		var provider = GetProvider<IRangeValueProvider>(peer, PatternInterface.RangeValue);
		if (provider is null || provider.IsReadOnly)
		{
			return false;
		}

		var clampedValue = Math.Max(provider.Minimum, Math.Min(provider.Maximum, value));
		return TryPerform(() => provider.SetValue(clampedValue));
	}

	internal static bool TrySetValue(AutomationPeer peer, string value)
	{
		var provider = GetProvider<IValueProvider>(peer, PatternInterface.Value);
		return provider is not null
			&& !provider.IsReadOnly
			&& TryPerform(() => provider.SetValue(value));
	}

	internal static bool TryScroll(
		AutomationPeer peer,
		ScrollAmount horizontalAmount,
		ScrollAmount verticalAmount)
		=> TryPerformProvider<IScrollProvider>(
			peer,
			PatternInterface.Scroll,
			provider => provider.Scroll(horizontalAmount, verticalAmount));

	internal static bool TrySetScrollPercent(
		AutomationPeer peer,
		double horizontalPercent,
		double verticalPercent)
		=> TryPerformProvider<IScrollProvider>(
			peer,
			PatternInterface.Scroll,
			provider => provider.SetScrollPercent(horizontalPercent, verticalPercent));

	internal static bool TryScrollIntoView(AutomationPeer peer)
		=> TryPerformProvider<IScrollItemProvider>(
			peer,
			PatternInterface.ScrollItem,
			static provider => provider.ScrollIntoView());

	internal static bool TryRealize(AutomationPeer peer)
		=> TryPerformProvider<IVirtualizedItemProvider>(
			peer,
			PatternInterface.VirtualizedItem,
			static provider => provider.Realize());

	internal static bool TryChangeView(AutomationPeer peer, int viewId)
	{
		var provider = GetProvider<IMultipleViewProvider>(peer, PatternInterface.MultipleView);
		return provider is not null
			&& provider.GetSupportedViews().Contains(viewId)
			&& TryPerform(() => provider.SetCurrentView(viewId));
	}

	internal static bool TrySetDockPosition(AutomationPeer peer, DockPosition dockPosition)
		=> TryPerformProvider<IDockProvider>(
			peer,
			PatternInterface.Dock,
			provider => provider.SetDockPosition(dockPosition));

	internal static bool TryMove(AutomationPeer peer, double x, double y)
	{
		var provider = GetTransformProvider(peer);
		return provider is not null
			&& provider.CanMove
			&& TryPerform(() => provider.Move(x, y));
	}

	internal static bool TryResize(AutomationPeer peer, double width, double height)
	{
		var provider = GetTransformProvider(peer);
		return provider is not null
			&& provider.CanResize
			&& TryPerform(() => provider.Resize(width, height));
	}

	internal static bool TryRotate(AutomationPeer peer, double degrees)
	{
		var provider = GetTransformProvider(peer);
		return provider is not null
			&& provider.CanRotate
			&& TryPerform(() => provider.Rotate(degrees));
	}

	internal static bool TryZoom(AutomationPeer peer, double zoom)
	{
		var provider = GetProvider<ITransformProvider2>(peer, PatternInterface.Transform2);
		if (provider is null || !provider.CanZoom)
		{
			return false;
		}

		var clampedZoom = Math.Max(provider.MinZoom, Math.Min(provider.MaxZoom, zoom));
		return TryPerform(() => provider.Zoom(clampedZoom));
	}

	internal static bool TryZoomByUnit(AutomationPeer peer, ZoomUnit zoomUnit)
	{
		var provider = GetProvider<ITransformProvider2>(peer, PatternInterface.Transform2);
		return provider is not null
			&& provider.CanZoom
			&& TryPerform(() => provider.ZoomByUnit(zoomUnit));
	}

	internal static bool TryClose(AutomationPeer peer)
		=> TryPerformProvider<IWindowProvider>(
			peer,
			PatternInterface.Window,
			static provider => provider.Close());

	internal static bool TrySetWindowVisualState(AutomationPeer peer, WindowVisualState state)
		=> TryPerformProvider<IWindowProvider>(
			peer,
			PatternInterface.Window,
			provider => provider.SetVisualState(state));

	internal static AccessibilityNativeFallbackDetails? GetFallbackDetails(AutomationPeer peer)
	{
		var resolvedPeer = ResolveProviderPeer(peer);
		List<PatternInterface>? internalPatterns = null;
		List<PatternInterface>? unsupportedPatterns = null;

		foreach (var pattern in s_internalFallbackPatterns)
		{
			if (resolvedPeer.GetPattern(pattern) is not null)
			{
				(internalPatterns ??= new()).Add(pattern);
			}
		}

		foreach (var pattern in s_unsupportedFallbackPatterns)
		{
			if (resolvedPeer.GetPattern(pattern) is not null)
			{
				(unsupportedPatterns ??= new()).Add(pattern);
			}
		}

		return internalPatterns is null && unsupportedPatterns is null
			? null
			: new AccessibilityNativeFallbackDetails(internalPatterns, unsupportedPatterns);
	}

	private static readonly PatternInterface[] s_internalFallbackPatterns =
	[
		PatternInterface.ItemContainer,
		PatternInterface.Text2,
		PatternInterface.TextChild,
		PatternInterface.TextRange,
		PatternInterface.Annotation,
		PatternInterface.Drag,
		PatternInterface.DropTarget,
		PatternInterface.Spreadsheet,
		PatternInterface.SpreadsheetItem,
		PatternInterface.Styles,
		PatternInterface.TextEdit,
		PatternInterface.CustomNavigation,
	];

	private static readonly PatternInterface[] s_unsupportedFallbackPatterns =
	[
		PatternInterface.ObjectModel,
		PatternInterface.SynchronizedInput,
	];

	private static bool TryAdjustRange(AutomationPeer peer, bool increment)
	{
		var provider = GetProvider<IRangeValueProvider>(peer, PatternInterface.RangeValue);
		if (provider is null || provider.IsReadOnly)
		{
			return false;
		}

		var delta = increment ? provider.SmallChange : -provider.SmallChange;
		return TrySetRangeValue(peer, provider.Value + delta);
	}

	private static ITransformProvider? GetTransformProvider(AutomationPeer peer)
		=> GetProvider<ITransformProvider2>(peer, PatternInterface.Transform2)
			?? GetProvider<ITransformProvider>(peer, PatternInterface.Transform);

	private static T? GetProvider<T>(AutomationPeer peer, PatternInterface pattern)
		where T : class
	{
		var providerPeer = ResolveProviderPeer(peer);
		return providerPeer.GetPattern(pattern) as T ?? providerPeer as T;
	}

	private static bool TryPerformProvider<T>(
		AutomationPeer peer,
		PatternInterface pattern,
		Action<T> action)
		where T : class
	{
		var provider = GetProvider<T>(peer, pattern);
		return provider is not null && TryPerform(() => action(provider));
	}

	private static bool TryPerform(Action? action)
	{
		if (action is null)
		{
			return false;
		}

		return TryPerform(() =>
		{
			action();
			return true;
		});
	}

	private static bool TryPerform(Func<bool> action)
	{
		try
		{
			return action();
		}
		catch (ElementNotEnabledException)
		{
			return false;
		}
		catch (ElementNotAvailableException)
		{
			return false;
		}
		catch (ArgumentException)
		{
			return false;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
	}

	private static void AppendPeer(
		AutomationPeer peer,
		int? parentIndex,
		int depth,
		List<AccessibilityPeerNode> nodes,
		HashSet<AutomationPeer> visited,
		IList<AutomationPeer>? prefetchedChildren = null)
	{
		if (depth > MaxTreeDepth || !visited.Add(peer))
		{
			return;
		}

		var providerPeer = ResolveProviderPeer(peer);
		var owner = GetOwner(providerPeer) ?? GetOwner(peer);
		var childParentIndex = parentIndex;

		if (peer.IsControlElement() || peer.IsContentElement())
		{
			childParentIndex = nodes.Count;
			nodes.Add(new AccessibilityPeerNode(peer, providerPeer, owner, parentIndex, depth));
		}

		var children = prefetchedChildren ?? peer.GetChildren();
		if (children is not { Count: > 0 })
		{
			return;
		}

		foreach (var child in children)
		{
			if (child is not null)
			{
				AppendPeer(child, childParentIndex, depth + 1, nodes, visited);
			}
		}

	}

	private static void AppendElement(
		UIElement element,
		int? parentIndex,
		int depth,
		List<AccessibilityPeerNode> nodes,
		HashSet<AutomationPeer> visitedPeers,
		HashSet<UIElement> visitedElements)
	{
		if (depth > MaxTreeDepth || !visitedElements.Add(element))
		{
			return;
		}

		if (element.GetOrCreateAutomationPeer() is { } peer)
		{
			var isIncluded = peer.IsControlElement() || peer.IsContentElement();
			var peerChildren = peer.GetChildren();
			if (isIncluded || peerChildren is { Count: > 0 })
			{
				AppendPeer(peer, parentIndex, depth, nodes, visitedPeers, peerChildren);
				return;
			}
		}

		foreach (var child in element.GetChildren())
		{
			if (child is UIElement uiElement)
			{
				AppendElement(uiElement, parentIndex, depth + 1, nodes, visitedPeers, visitedElements);
			}
		}
	}

	private static UIElement? GetOwner(AutomationPeer peer)
		=> peer switch
		{
			FrameworkElementAutomationPeer { Owner: { } owner } => owner,
			ItemAutomationPeer itemPeer => itemPeer.GetContainer(),
			_ => null,
		};
}
