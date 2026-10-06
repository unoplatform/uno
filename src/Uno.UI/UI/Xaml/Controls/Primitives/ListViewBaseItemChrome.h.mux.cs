// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItemChrome.h, tag winui3/release/2.5.1

#nullable enable

using System;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls.Primitives;

// CListViewBaseItemChrome knows how to render all layers of the chrome, including
// the layers present in the Grid/ListViewItem and in the secondary chrome.
// So we can let the chrome know which layer to render, we have this enum
// (in order of lowest layer to highest layer).
internal enum ListViewBaseItemChromeLayerPosition
{
	Base_Pre,
	PrimaryChrome_Pre,
	SecondaryChrome_Pre,
	SecondaryChrome_Post,
	PrimaryChrome_Post,
	Base_Post,
}

// TODO Uno: ListViewBaseItemAnimationCommandVisitor and the ListViewBaseItemAnimationCommand* classes (H:28-66, H:834-985) are not ported yet.

// Uno-specific: CListViewBaseItemChrome is merged into ListViewBaseItemPresenter (as CalendarViewBaseItemChrome is into CalendarViewBaseItem).
partial class ListViewBaseItemPresenter
{
	// Enums and helpers specifying the visual state groups. Each group has an appropriately-typed
	// FromString overload that parses a string state name.

	// Dead WinUI code: the legacy CommonStates, SelectionHintStates and SelectionStates enums are only parsed by the removed old-style chrome.

	internal enum FocusStates : byte
	{
		Focused,
		Unfocused,
		PointerFocused
	}

	internal enum DragStates : byte
	{
		NotDragging,
		Dragging,
		DraggingTarget,
		MultipleDraggingPrimary,
		MultipleDraggingSecondary,
		DraggedPlaceholder,
		Reordering,
		ReorderingTarget,
		MultipleReorderingPrimary,
		ReorderedPlaceholder,
		DragOver
	}

	internal enum ReorderHintStates : byte
	{
		NoReorderHint,
		BottomReorderHint,
		TopReorderHint,
		RightReorderHint,
		LeftReorderHint
	}

	internal enum DataVirtualizationStates : byte
	{
		DataAvailable,
		DataPlaceholder
	}

	// Visual states for new ListViewBaseItem styles
	internal enum CommonStates2 : byte
	{
		Normal,
		PointerOver,
		Pressed,
		Selected,
		PointerOverSelected,
		PressedSelected
	}

	internal enum DisabledStates : byte
	{
		Enabled,
		Disabled
	}

	// enum FocusStates (using the same one as above)

	internal enum MultiSelectStates : byte
	{
		MultiSelectDisabled,
		MultiSelectEnabled,
	}

	internal enum SelectionIndicatorStates : byte
	{
		SelectionIndicatorDisabled,
		SelectionIndicatorEnabled,
	}

	// A struct holding and analyzing the current set of VisualStates active in the chrome.
	internal struct VisualStates
	{
		internal FocusStates focusState;
		internal DragStates dragState;
		internal ReorderHintStates reorderHintState;
		internal DataVirtualizationStates dataVirtualizationState;

		internal CommonStates2 commonState2;
		internal DisabledStates disabledState;
		internal MultiSelectStates multiSelectState;
		internal SelectionIndicatorStates selectionIndicatorState;

		// These methods return TRUE if the set of active VisualStates contains
		// the given state.
		internal readonly bool HasState(FocusStates state) => focusState == state;
		internal readonly bool HasState(DragStates state) => dragState == state;
		internal readonly bool HasState(ReorderHintStates state) => reorderHintState == state;
		internal readonly bool HasState(DataVirtualizationStates state) => dataVirtualizationState == state;

		internal readonly bool HasState(CommonStates2 state) => commonState2 == state;
		internal readonly bool HasState(DisabledStates state) => disabledState == state;
		internal readonly bool HasState(MultiSelectStates state) => multiSelectState == state;
		internal readonly bool HasState(SelectionIndicatorStates state) => selectionIndicatorState == state;
	}

	internal readonly struct MapItem<TEnum>
		where TEnum : struct, Enum
	{
		internal MapItem(string name, TEnum enumValue)
		{
			m_pName = name;
			m_pEnumValue = enumValue;
		}

		internal readonly string m_pName;
		internal readonly TEnum m_pEnumValue;
	}

	internal static class Mapping<TEnum>
		where TEnum : struct, Enum
	{
		internal static readonly MapItem<TEnum>[] s_map = (MapItem<TEnum>[])GetMap(typeof(TEnum));
	}

	// Used by CDependencyProperty::GetDefaultValue
	internal static readonly CornerRadius s_defaultSelectionIndicatorCornerRadius = new(1.5);
	internal static readonly CornerRadius s_defaultCheckBoxCornerRadius = new(3.0);
	internal static readonly Thickness s_selectedBorderThicknessRounded = new(2.0);
	internal static readonly Thickness s_selectedBorderThickness = new(0.0);

#pragma warning disable CS0169, CS0414, CS0649 // Fields used by the chrome parts not ported yet.
	// Uno-specific: a strong reference; the item unlinks it (SetGridViewItemChrome) when retemplated.
	private ContentControl? m_pParentListViewBaseItemNoRef;
	// TODO Uno: typed as ListViewBaseItemSecondaryChrome once it is ported.
	private FrameworkElement? m_pSecondaryChrome;

	// Dead WinUI code: m_pCheckGeometryData and m_checkGeometryBounds belong to the removed checkmark path.

	// TextBlock we create on-the-fly to display our DragItemsCount.
	// TODO: Integrate text rendering into the chrome.
	// This isn't a trivial task.
	private TextBlock? m_pDragItemsCountTextBlock;

	private Border? m_multiSelectCheckBoxRectangle;
	private RectangleGeometry? m_multiSelectCheckBoxClip;
	private FontIcon? m_multiSelectCheckGlyph;

	private Border? m_backplateRectangle;
	private Border? m_selectionIndicatorRectangle;
	private Border? m_innerSelectionBorder;
	private Border? m_outerBorder;

	// Dead WinUI code: m_currentCheckBrush is only used by the removed old-style draw path.

	// TODO Uno: m_animationCommands, m_pCurrentHighestPriorityCommand and m_currentHighestCommandPriority come with the animation command queue.

	// Opacity of our swipe hint check mark.
	private float m_swipeHintCheckOpacity = s_cOpacityUnset;

	// Used for brush transitions. Saves the last brush that we used to render the background. If this brush changes, then
	// we kick off a brush transition (if the API enabled it).
	private Brush? m_previousBackgroundBrush;

	private VisualStates m_visualStates;

	private bool m_isFocusVisualDrawnByFocusManager;

	// Path rendering fields.
	// TODO Uno: m_fFillBrushDirty (set by NWSetContentDirty) has no Uno dirty-flag render model.
	private bool m_shouldRenderChrome;
	private bool m_isInIndicatorSelect; // true when states are in MultiSelectStates_MultiSelectDisabled + SelectionIndicatorStates_SelectionIndicatorEnabled
	private bool m_isInMultiSelect;     // true when states are in MultiSelectStates_MultiSelectEnabled  + SelectionIndicatorStates_SelectionIndicatorDisabled
#pragma warning restore CS0169, CS0414, CS0649

	private static bool? s_isRoundedListViewBaseItemChromeEnabled;

	// Uno-specific: WinUI stores these DPs in chrome fields shared by LVIP and GVIP (a GVIP leaves the LVIP-only ones null),
	// Uno reads the presenter's own DP.
	// Dead WinUI code: m_pCurrentEarmarkBrush is only used by the removed secondary chrome earmark.
	private Brush? m_pCheckHintBrush => GetBrush(ListViewItemPresenter.CheckHintBrushProperty, GridViewItemPresenter.CheckHintBrushProperty);
	private Brush? m_pCheckSelectingBrush => GetBrush(ListViewItemPresenter.CheckSelectingBrushProperty, GridViewItemPresenter.CheckSelectingBrushProperty);
	private Brush? m_pCheckBrush => GetBrush(ListViewItemPresenter.CheckBrushProperty, GridViewItemPresenter.CheckBrushProperty);
	private Brush? m_pCheckPressedBrush => GetBrush(ListViewItemPresenter.CheckPressedBrushProperty, null);
	private Brush? m_pCheckDisabledBrush => GetBrush(ListViewItemPresenter.CheckDisabledBrushProperty, null);
	private Brush? m_pDragBackground => GetBrush(ListViewItemPresenter.DragBackgroundProperty, GridViewItemPresenter.DragBackgroundProperty);
	private Brush? m_pDragForeground => GetBrush(ListViewItemPresenter.DragForegroundProperty, GridViewItemPresenter.DragForegroundProperty);
	private Brush? m_pFocusBorderBrush => GetBrush(ListViewItemPresenter.FocusBorderBrushProperty, GridViewItemPresenter.FocusBorderBrushProperty);
	private Brush? m_pPlaceholderBackground => GetBrush(ListViewItemPresenter.PlaceholderBackgroundProperty, GridViewItemPresenter.PlaceholderBackgroundProperty);
	private Brush? m_pPointerOverBorderBrush => GetBrush(ListViewItemPresenter.PointerOverBorderBrushProperty, null);
	private Brush? m_pPointerOverBackground => GetBrush(ListViewItemPresenter.PointerOverBackgroundProperty, GridViewItemPresenter.PointerOverBackgroundProperty);
	private Brush? m_pPointerOverForeground => GetBrush(ListViewItemPresenter.PointerOverForegroundProperty, null);
	private Brush? m_pSelectedBackground => GetBrush(ListViewItemPresenter.SelectedBackgroundProperty, GridViewItemPresenter.SelectedBackgroundProperty);
	private Brush? m_pSelectedForeground => GetBrush(ListViewItemPresenter.SelectedForegroundProperty, GridViewItemPresenter.SelectedForegroundProperty);
	private Brush? m_pSelectedPointerOverBackground => GetBrush(ListViewItemPresenter.SelectedPointerOverBackgroundProperty, GridViewItemPresenter.SelectedPointerOverBackgroundProperty);
	private Brush? m_pSelectedPointerOverBorderBrush => GetBrush(ListViewItemPresenter.SelectedPointerOverBorderBrushProperty, GridViewItemPresenter.SelectedPointerOverBorderBrushProperty);
	private Brush? m_pSelectedBorderBrush => GetBrush(ListViewItemPresenter.SelectedBorderBrushProperty, null);
	private Brush? m_pSelectedPressedBorderBrush => GetBrush(ListViewItemPresenter.SelectedPressedBorderBrushProperty, null);
	private Brush? m_pSelectedDisabledBorderBrush => GetBrush(ListViewItemPresenter.SelectedDisabledBorderBrushProperty, null);
	private Brush? m_pSelectedInnerBorderBrush => GetBrush(ListViewItemPresenter.SelectedInnerBorderBrushProperty, null);
	private Brush? m_pSelectedPressedBackground => GetBrush(ListViewItemPresenter.SelectedPressedBackgroundProperty, null);
	private Brush? m_pSelectedDisabledBackground => GetBrush(ListViewItemPresenter.SelectedDisabledBackgroundProperty, null);
	private Brush? m_pPressedBackground => GetBrush(ListViewItemPresenter.PressedBackgroundProperty, null);
	private Brush? m_pCheckBoxBrush => GetBrush(ListViewItemPresenter.CheckBoxBrushProperty, null);
	private Brush? m_pCheckBoxPointerOverBrush => GetBrush(ListViewItemPresenter.CheckBoxPointerOverBrushProperty, null);
	private Brush? m_pCheckBoxPressedBrush => GetBrush(ListViewItemPresenter.CheckBoxPressedBrushProperty, null);
	private Brush? m_pCheckBoxDisabledBrush => GetBrush(ListViewItemPresenter.CheckBoxDisabledBrushProperty, null);
	private Brush? m_pCheckBoxSelectedBrush => GetBrush(ListViewItemPresenter.CheckBoxSelectedBrushProperty, null);
	private Brush? m_pCheckBoxSelectedPointerOverBrush => GetBrush(ListViewItemPresenter.CheckBoxSelectedPointerOverBrushProperty, null);
	private Brush? m_pCheckBoxSelectedPressedBrush => GetBrush(ListViewItemPresenter.CheckBoxSelectedPressedBrushProperty, null);
	private Brush? m_pCheckBoxSelectedDisabledBrush => GetBrush(ListViewItemPresenter.CheckBoxSelectedDisabledBrushProperty, null);
	private Brush? m_pCheckBoxBorderBrush => GetBrush(ListViewItemPresenter.CheckBoxBorderBrushProperty, null);
	private Brush? m_pCheckBoxPointerOverBorderBrush => GetBrush(ListViewItemPresenter.CheckBoxPointerOverBorderBrushProperty, null);
	private Brush? m_pCheckBoxPressedBorderBrush => GetBrush(ListViewItemPresenter.CheckBoxPressedBorderBrushProperty, null);
	private Brush? m_pCheckBoxDisabledBorderBrush => GetBrush(ListViewItemPresenter.CheckBoxDisabledBorderBrushProperty, null);
	private Brush? m_pSelectionIndicatorBrush => GetBrush(ListViewItemPresenter.SelectionIndicatorBrushProperty, null);
	private Brush? m_pSelectionIndicatorPointerOverBrush => GetBrush(ListViewItemPresenter.SelectionIndicatorPointerOverBrushProperty, null);
	private Brush? m_pSelectionIndicatorPressedBrush => GetBrush(ListViewItemPresenter.SelectionIndicatorPressedBrushProperty, null);
	private Brush? m_pSelectionIndicatorDisabledBrush => GetBrush(ListViewItemPresenter.SelectionIndicatorDisabledBrushProperty, null);
	private Brush? m_pFocusSecondaryBorderBrush => GetBrush(ListViewItemPresenter.FocusSecondaryBorderBrushProperty, null);

	private Thickness m_contentMargin => (Thickness)GetValue(this is ListViewItemPresenter ? ListViewItemPresenter.ContentMarginProperty : GridViewItemPresenter.ContentMarginProperty);

	private ListViewItemPresenterCheckMode m_checkMode => this is ListViewItemPresenter ? (ListViewItemPresenterCheckMode)GetValue(ListViewItemPresenter.CheckModeProperty) : ListViewItemPresenterCheckMode.Inline;

	private Brush? GetBrush(DependencyProperty listViewItemPresenterProperty, DependencyProperty? gridViewItemPresenterProperty)
	{
		if (this is ListViewItemPresenter)
		{
			return (Brush?)GetValue(listViewItemPresenterProperty);
		}

		return gridViewItemPresenterProperty is null ? null : (Brush?)GetValue(gridViewItemPresenterProperty);
	}

	// Flag which determines if chrome gets rendered.
	internal void SetShouldRenderChrome(bool shouldRenderChrome)
	{
		if (m_shouldRenderChrome != shouldRenderChrome)
		{
			m_shouldRenderChrome = shouldRenderChrome;
			InvalidateRender();
		}
	}

	// Default corner radius of the selection indicator visual.
	// Used when the SelectionIndicatorCornerRadius property returns 0 and IsRoundedListViewBaseItemChromeForced() returns True.
	internal static float GetDefaultSelectionIndicatorCornerRadius() => 1.5f;

	// Default corner radius of the checkbox visual.
	// Used when the CheckBoxCornerRadius property returns 0 and IsRoundedListViewBaseItemChromeForced() returns True.
	internal static float GetDefaultCheckBoxCornerRadius() => 3.0f;

	internal static float GetDefaultDisabledOpacity(bool forRoundedListViewBaseItemChrome)
		=> forRoundedListViewBaseItemChrome ? 0.3f : 0.55f;

	internal static float GetDefaultDragOpacity() => 0.8f;

	internal static float GetDefaultListViewItemReorderHintOffset() => 10.0f;

	internal static float GetDefaultGridViewItemReorderHintOffset() => 16.0f;

	internal static float GetSelectedBorderThickness(bool forRoundedListViewBaseItemChrome)
		=> forRoundedListViewBaseItemChrome ? 2.0f : 0.0f;

	internal static bool GetDefaultSelectionCheckMarkVisualEnabled() => true;

	internal static Thickness GetSelectedBorderXThickness(bool forRoundedListViewBaseItemChrome)
		=> forRoundedListViewBaseItemChrome ? s_selectedBorderThicknessRounded : s_selectedBorderThickness;
}

// TODO Uno: RuntimeEnabledFeatureDetector
internal static class ListViewBaseItemChromeRuntimeFeatures
{
	internal static bool ForceRoundedListViewBaseItemChrome { get; private set; }

	internal static bool DenyRoundedListViewBaseItemChrome { get; private set; }

	internal static bool ForceSelectionIndicatorVisualEnabled { get; private set; }

	internal static bool DenySelectionIndicatorVisualEnabled { get; private set; }

	internal static bool ForceSelectionIndicatorModeInline { get; private set; }

	internal static bool ForceSelectionIndicatorModeOverlay { get; private set; }

	// Scopes must be disposed in reverse order of creation.
	internal static IDisposable Override(
		bool? forceRounded = null,
		bool? denyRounded = null,
		bool? forceSelectionIndicatorVisual = null,
		bool? denySelectionIndicatorVisual = null,
		bool? forceSelectionIndicatorModeInline = null,
		bool? forceSelectionIndicatorModeOverlay = null)
	{
		var previous = Snapshot.Capture();

		ForceRoundedListViewBaseItemChrome = forceRounded ?? previous.ForceRounded;
		DenyRoundedListViewBaseItemChrome = denyRounded ?? previous.DenyRounded;
		ForceSelectionIndicatorVisualEnabled = forceSelectionIndicatorVisual ?? previous.ForceSelectionIndicatorVisual;
		DenySelectionIndicatorVisualEnabled = denySelectionIndicatorVisual ?? previous.DenySelectionIndicatorVisual;
		ForceSelectionIndicatorModeInline = forceSelectionIndicatorModeInline ?? previous.ForceModeInline;
		ForceSelectionIndicatorModeOverlay = forceSelectionIndicatorModeOverlay ?? previous.ForceModeOverlay;
		ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();

		return new Restore(previous);
	}

	private readonly record struct Snapshot(
		bool ForceRounded,
		bool DenyRounded,
		bool ForceSelectionIndicatorVisual,
		bool DenySelectionIndicatorVisual,
		bool ForceModeInline,
		bool ForceModeOverlay)
	{
		internal static Snapshot Capture() => new(
			ForceRoundedListViewBaseItemChrome,
			DenyRoundedListViewBaseItemChrome,
			ForceSelectionIndicatorVisualEnabled,
			DenySelectionIndicatorVisualEnabled,
			ForceSelectionIndicatorModeInline,
			ForceSelectionIndicatorModeOverlay);
	}

	private sealed class Restore : IDisposable
	{
		private readonly Snapshot _snapshot;
		private bool _disposed;

		public Restore(Snapshot snapshot) => _snapshot = snapshot;

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			ForceRoundedListViewBaseItemChrome = _snapshot.ForceRounded;
			DenyRoundedListViewBaseItemChrome = _snapshot.DenyRounded;
			ForceSelectionIndicatorVisualEnabled = _snapshot.ForceSelectionIndicatorVisual;
			DenySelectionIndicatorVisualEnabled = _snapshot.DenySelectionIndicatorVisual;
			ForceSelectionIndicatorModeInline = _snapshot.ForceModeInline;
			ForceSelectionIndicatorModeOverlay = _snapshot.ForceModeOverlay;
			ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();
		}
	}
}
