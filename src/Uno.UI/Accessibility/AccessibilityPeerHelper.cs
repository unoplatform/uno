#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Automation.Text;
using Microsoft.UI.Xaml.Controls;
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
	SetTextSelection,
	MoveTextNext,
	MoveTextPrevious,
	CopyText,
	CutText,
	PasteText,
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
		string? nativeRoleDescription = null,
		int textSelectionStart = -1,
		int textSelectionEnd = -1,
		int movementGranularities = 0,
		bool scrollable = false,
		IReadOnlyList<int>? nativeActionIds = null)
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
		TextSelectionStart = textSelectionStart;
		TextSelectionEnd = textSelectionEnd;
		MovementGranularities = movementGranularities;
		Scrollable = scrollable;
		NativeActionIds = nativeActionIds ?? Array.Empty<int>();
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

	internal int TextSelectionStart { get; }

	internal int TextSelectionEnd { get; }

	internal int MovementGranularities { get; }

	internal bool Scrollable { get; }

	internal IReadOnlyList<int> NativeActionIds { get; }

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
			Enabled, Heading, Password, Checkable, IsChecked, Traits, Bounds, details, NativeAutomationId, StateDescription, NativeRoleDescription,
			TextSelectionStart, TextSelectionEnd, MovementGranularities, Scrollable, NativeActionIds);
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

	internal static Func<XamlRoot, int>? IOSAutomationElementsCountAccessor { get; set; }

	internal static Func<XamlRoot, object[]?>? IOSAutomationElementsForRootAccessor { get; set; }

	internal static Func<UIElement, object?>? AndroidAccessibilityNodeAccessor { get; set; }

	internal static Func<UIElement, int?>? AndroidAccessibilityVirtualIdAccessor { get; set; }

	internal static Func<AutomationPeer, int?>? AndroidAccessibilityPeerVirtualIdAccessor { get; set; }

	internal static Func<XamlRoot, double, double, int?>? AndroidAccessibilityHitTestAccessor { get; set; }

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
	{
		var providerPeer = ResolveProviderPeer(peer);
		return providerPeer.IsEnabled()
			&& TryPerform(() => providerPeer.InvokeAutomationPeer());
	}

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
		var providerPeer = ResolveProviderPeer(peer);
		var provider = GetProvider<IExpandCollapseProvider>(providerPeer, PatternInterface.ExpandCollapse);
		if (!providerPeer.IsEnabled() || provider is null)
		{
			return false;
		}

		return TryPerform(provider.Expand);
	}

	internal static bool TryCollapse(AutomationPeer peer)
	{
		var providerPeer = ResolveProviderPeer(peer);
		var provider = GetProvider<IExpandCollapseProvider>(providerPeer, PatternInterface.ExpandCollapse);
		if (!providerPeer.IsEnabled() || provider is null)
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
		var providerPeer = ResolveProviderPeer(peer);
		var provider = GetProvider<IRangeValueProvider>(providerPeer, PatternInterface.RangeValue);
		if (!providerPeer.IsEnabled() ||
			provider is null ||
			provider.IsReadOnly ||
			!double.IsFinite(value) ||
			value < provider.Minimum ||
			value > provider.Maximum)
		{
			return false;
		}

		return TryPerform(() => provider.SetValue(value));
	}

	internal static bool TrySetValue(AutomationPeer peer, string value)
	{
		var providerPeer = ResolveProviderPeer(peer);
		var provider = GetProvider<IValueProvider>(providerPeer, PatternInterface.Value);
		return provider is not null
			&& CanSetText(providerPeer, provider)
			&& TryPerform(() => provider.SetValue(value));
	}

	internal static bool CanSetText(AutomationPeer peer)
	{
		var providerPeer = ResolveProviderPeer(peer);
		return GetProvider<IValueProvider>(providerPeer, PatternInterface.Value) is { } provider
			&& CanSetText(providerPeer, provider);
	}

	private static bool CanSetText(AutomationPeer providerPeer, IValueProvider provider)
		=> providerPeer.IsEnabled()
			&& !provider.IsReadOnly
			&& providerPeer is not FrameworkElementAutomationPeer { Owner: RichEditBox };

	internal static bool CanCopyText(AutomationPeer peer)
		=> !ResolveProviderPeer(peer).IsPassword() &&
			GetTextBox(peer) is { IsEnabled: true, FocusState: not FocusState.Unfocused, SelectionLength: > 0 } textBox &&
			textBox is not PasswordBox;

	internal static bool CanCutText(AutomationPeer peer)
		=> CanCopyText(peer) &&
			GetTextBox(peer) is { IsReadOnly: false };

	internal static bool CanPasteText(AutomationPeer peer)
	{
#if __SKIA__
		return GetTextBox(peer) is
		{
			IsEnabled: true,
			IsReadOnly: false,
			FocusState: not FocusState.Unfocused,
			CanPasteClipboardContent: true,
		};
#else
		return false;
#endif
	}

	internal static bool TryCopyText(AutomationPeer peer)
	{
		var textBox = GetTextBox(peer);
		return textBox is not null &&
			CanCopyText(peer) &&
			TryPerform(textBox.CopySelectionToClipboard);
	}

	internal static bool TryCutText(AutomationPeer peer)
	{
		var textBox = GetTextBox(peer);
		return textBox is not null &&
			CanCutText(peer) &&
			TryPerform(textBox.CutSelectionToClipboard);
	}

	internal static bool TryPasteText(AutomationPeer peer)
	{
		var textBox = GetTextBox(peer);
		return textBox is not null &&
			CanPasteText(peer) &&
			TryPerform(textBox.PasteFromClipboard);
	}

	internal static bool TryGetText(
		AutomationPeer peer,
		out string text,
		out bool supportsSelection)
	{
		var value = string.Empty;
		var selectionSupported = false;
		var success = TryPerform(() =>
		{
			var providerPeer = ResolveProviderPeer(peer);
			var provider = GetProvider<ITextProvider>(providerPeer, PatternInterface.Text);
			if (provider is null)
			{
				return false;
			}

			value = provider.DocumentRange.GetText(-1) ?? string.Empty;
			selectionSupported = provider.SupportedTextSelection != SupportedTextSelection.None;
			return true;
		});

		text = value;
		supportsSelection = selectionSupported;
		return success;
	}

	internal static bool TryGetTextSelection(
		AutomationPeer peer,
		out int selectionStart,
		out int selectionEnd)
	{
		var start = -1;
		var end = -1;
		var success = TryPerform(() =>
		{
			var providerPeer = ResolveProviderPeer(peer);
			var provider = GetProvider<ITextProvider>(providerPeer, PatternInterface.Text);
			if (provider is null ||
				provider.SupportedTextSelection == SupportedTextSelection.None)
			{
				return false;
			}

#if __SKIA__
			if (providerPeer is FrameworkElementAutomationPeer { Owner: TextBox textBox })
			{
				start = textBox.IsBackwardSelection
					? textBox.SelectionStart + textBox.SelectionLength
					: textBox.SelectionStart;
				end = textBox.IsBackwardSelection
					? textBox.SelectionStart
					: textBox.SelectionStart + textBox.SelectionLength;
				return true;
			}
#endif

			if (provider.GetSelection() is not { Length: > 0 } selection)
			{
				return false;
			}

			return TryGetTextRangeOffsets(provider, selection[0], out start, out end);
		});

		selectionStart = start;
		selectionEnd = end;
		return success;
	}

	internal static bool TrySetTextSelection(
		AutomationPeer peer,
		int selectionStart,
		int selectionEnd,
		bool allowReversed,
		out int actualSelectionStart,
		out int actualSelectionEnd)
	{
		var actualStart = -1;
		var actualEnd = -1;
		if (selectionStart < 0 || (!allowReversed && selectionEnd < selectionStart))
		{
			actualSelectionStart = actualStart;
			actualSelectionEnd = actualEnd;
			return false;
		}

		var success = TryPerform(() =>
		{
			var providerPeer = ResolveProviderPeer(peer);
			var provider = GetProvider<ITextProvider>(providerPeer, PatternInterface.Text);
			if (provider is null ||
				provider.SupportedTextSelection == SupportedTextSelection.None)
			{
				return false;
			}

			var documentRange = provider.DocumentRange;
			var textLength = documentRange.GetText(-1).Length;
			if (Math.Max(selectionStart, selectionEnd) > textLength)
			{
				return false;
			}

#if __SKIA__
			if (providerPeer is FrameworkElementAutomationPeer { Owner: TextBox textBox })
			{
				return textBox.SelectInternal(selectionStart, selectionEnd - selectionStart) &&
					TryGetTextSelection(providerPeer, out actualStart, out actualEnd) &&
					actualStart == selectionStart &&
					actualEnd == selectionEnd;
			}
#endif

			if (selectionEnd < selectionStart)
			{
				return false;
			}

			var range = documentRange.Clone();
			var movedStart = range.MoveEndpointByUnit(
				TextPatternRangeEndpoint.Start,
				TextUnit.Character,
				selectionStart);
			var movedEnd = range.MoveEndpointByUnit(
				TextPatternRangeEndpoint.End,
				TextUnit.Character,
				selectionEnd - textLength);
			if (movedStart != selectionStart || movedEnd != selectionEnd - textLength)
			{
				return false;
			}

			range.Select();
			return TryGetTextSelection(providerPeer, out actualStart, out actualEnd) &&
				actualStart == selectionStart &&
				actualEnd == selectionEnd;
		});

		actualSelectionStart = actualStart;
		actualSelectionEnd = actualEnd;
		return success;
	}

	internal static bool TryGetTextSegment(
		AutomationPeer peer,
		TextUnit unit,
		int position,
		bool forward,
		out int segmentStart,
		out int segmentEnd)
	{
		var start = -1;
		var end = -1;
		var success = TryPerform(() =>
		{
			var providerPeer = ResolveProviderPeer(peer);
			var provider = GetProvider<ITextProvider>(providerPeer, PatternInterface.Text);
			if (provider is null)
			{
				return false;
			}

			var text = provider.DocumentRange.GetText(-1) ?? string.Empty;
			var owner = providerPeer is FrameworkElementAutomationPeer frameworkPeer
				? frameworkPeer.Owner as FrameworkElement
				: null;
			return DirectUI.TextRangeAdapter.TryGetTextSegment(
				owner,
				text,
				unit,
				position,
				forward,
				out start,
				out end);
		});

		segmentStart = start;
		segmentEnd = end;
		return success;
	}

	internal static int GetTextMovementGranularities(AutomationPeer peer)
	{
		var providerPeer = ResolveProviderPeer(peer);
		var provider = GetProvider<ITextProvider>(providerPeer, PatternInterface.Text);
		if (provider is null)
		{
			return 0;
		}

		var text = provider.DocumentRange.GetText(-1) ?? string.Empty;
		var owner = providerPeer is FrameworkElementAutomationPeer { Owner: FrameworkElement element }
			? element
			: null;
		return DirectUI.TextRangeAdapter.GetSupportedTextGranularities(owner, text);
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
		var providerPeer = ResolveProviderPeer(peer);
		var provider = GetProvider<IMultipleViewProvider>(providerPeer, PatternInterface.MultipleView);
		return providerPeer.IsEnabled()
			&& provider is not null
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
		if (!ResolveProviderPeer(peer).IsEnabled())
		{
			return false;
		}

		var provider = GetTransformProvider(peer);
		return provider is not null
			&& provider.CanMove
			&& TryPerform(() => provider.Move(x, y));
	}

	internal static bool TryResize(AutomationPeer peer, double width, double height)
	{
		if (!ResolveProviderPeer(peer).IsEnabled())
		{
			return false;
		}

		var provider = GetTransformProvider(peer);
		return provider is not null
			&& provider.CanResize
			&& TryPerform(() => provider.Resize(width, height));
	}

	internal static bool TryRotate(AutomationPeer peer, double degrees)
	{
		if (!ResolveProviderPeer(peer).IsEnabled())
		{
			return false;
		}

		var provider = GetTransformProvider(peer);
		return provider is not null
			&& provider.CanRotate
			&& TryPerform(() => provider.Rotate(degrees));
	}

	internal static bool TryZoom(AutomationPeer peer, double zoom)
	{
		if (!ResolveProviderPeer(peer).IsEnabled())
		{
			return false;
		}

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
		if (!ResolveProviderPeer(peer).IsEnabled())
		{
			return false;
		}

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
		var providerPeer = ResolveProviderPeer(peer);
		var provider = GetProvider<IRangeValueProvider>(providerPeer, PatternInterface.RangeValue);
		if (!providerPeer.IsEnabled() ||
			provider is null ||
			provider.IsReadOnly ||
			!double.IsFinite(provider.SmallChange) ||
			provider.SmallChange <= 0)
		{
			return false;
		}

		var delta = increment ? provider.SmallChange : -provider.SmallChange;
		var target = Math.Max(
			provider.Minimum,
			Math.Min(provider.Maximum, provider.Value + delta));
		return target != provider.Value &&
			TrySetRangeValue(providerPeer, target);
	}

	private static ITransformProvider? GetTransformProvider(AutomationPeer peer)
		=> GetProvider<ITransformProvider2>(peer, PatternInterface.Transform2)
			?? GetProvider<ITransformProvider>(peer, PatternInterface.Transform);

	private static TextBox? GetTextBox(AutomationPeer peer)
		=> ResolveProviderPeer(peer) is FrameworkElementAutomationPeer { Owner: TextBox textBox }
			? textBox
			: null;

	private static bool TryGetTextRangeOffsets(
		ITextProvider provider,
		ITextRangeProvider range,
		out int start,
		out int end)
	{
		if (range is DirectUI.TextRangeAdapter adapter)
		{
			start = adapter.Start;
			end = adapter.End;
			return true;
		}

		var startPrefix = provider.DocumentRange.Clone();
		startPrefix.MoveEndpointByRange(
			TextPatternRangeEndpoint.End,
			range,
			TextPatternRangeEndpoint.Start);

		var endPrefix = provider.DocumentRange.Clone();
		endPrefix.MoveEndpointByRange(
			TextPatternRangeEndpoint.End,
			range,
			TextPatternRangeEndpoint.End);

		start = startPrefix.GetText(-1).Length;
		end = endPrefix.GetText(-1).Length;
		return end >= start;
	}

	private static T? GetProvider<T>(AutomationPeer peer, PatternInterface pattern)
		where T : class
	{
		var providerPeer = ResolveProviderPeer(peer);
		return providerPeer.GetPattern(pattern) as T;
	}

	private static bool TryPerformProvider<T>(
		AutomationPeer peer,
		PatternInterface pattern,
		Action<T> action)
		where T : class
	{
		var providerPeer = ResolveProviderPeer(peer);
		var provider = GetProvider<T>(providerPeer, pattern);
		return providerPeer.IsEnabled()
			&& provider is not null
			&& TryPerform(() => action(provider));
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
