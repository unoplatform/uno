// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItemChrome.cpp, tag winui3/release/2.5.1

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using DirectUI;
using Uno.UI.Xaml.Core.Scaling;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ListViewBaseItemPresenter
{
	// Dead WinUI code: s_selectionCheckMarkVisualSize, s_focusBorderThickness, s_checkmarkOffset, s_swipeHintOffset,
	// s_swipingCheckSteadyStateOpacity and s_selectingSwipingCheckSteadyStateOpacity are only used by the removed old-style chrome.

	// Special value for opacity not being set.
	internal const float s_cOpacityUnset = -1.0f;

	// Default corner radius of the outer selection border or backplate visual used when
	// GetCornerRadius() returns 0 and IsRoundedListViewBaseItemChromeForced() returns True.
	private const float s_generalCornerRadius = 4.0f;

	// Minimum corner radius of inner border visual.
	private const float s_innerBorderCornerRadius = 3.0f;

	// Size of selection indicator rectangular visual.
	private static readonly Size s_selectionIndicatorSize = new(3.0, 16.0);

	// Selection indicator height decrease when pressed.
	private const float s_selectionIndicatorHeightShrinkage = 6.0f;

	// Margin for the selection indicator rectangular visual.
	private static readonly Thickness s_selectionIndicatorMargin = new(4.0, 20.0, 0.0, 20.0);

	// Size of multiselect square visual.
	private static readonly Size s_multiSelectSquareSize = new(20.0, 20.0);

	// Border thickness for the multiselect square.
	private static readonly Thickness s_multiSelectSquareThickness = new(2.0, 2.0, 2.0, 2.0);

	// Border thickness for the multiselect square when rounded corners are applied.
	private static readonly Thickness s_multiSelectRoundedSquareThickness = new(1.0, 1.0, 1.0, 1.0);

	// Margin for the multiselect square for inline (ListView)
	private static readonly Thickness s_multiSelectSquareInlineMargin = new(12.0, 0.0, 0.0, 0.0);

	// Margin for the multiselect square for inline (ListView)
	private static readonly Thickness s_multiSelectRoundedSquareInlineMargin = new(14.0, 0.0, 0.0, 0.0);

	// Margin for the multiselect square for overlay (GridView)
	private static readonly Thickness s_multiSelectSquareOverlayMargin = new(0.0, 2.0, 2.0, 0.0);

	// Backplate margin.
	private static readonly Thickness s_backplateMargin = new(4.0, 2.0, 4.0, 2.0);

	// Unselected outer border thickness.
	private static readonly Thickness s_borderThickness = new(1.0, 1.0, 1.0, 1.0);

	// Extra inner selection border thickness compared to outer border.
	private static readonly Thickness s_innerSelectionBorderThickness = new(1.0, 1.0, 1.0, 1.0);

	// Offset of ListViewItem multiselect content.
	private const float s_listViewItemMultiSelectContentOffset = 32.0f;

	// Offset of ListViewItem multiselect content when rounded corners are applied.
	private const float s_multiSelectRoundedContentOffset = 28.0f;

	// Dead WinUI code: s_checkMarkPoints belongs to the removed checkmark path.

	// ListViewItem focus border thickness
	private const float s_listViewItemFocusBorderThickness = 1.0f;

	// ListViewItem focus border thickness
	private const float s_gridViewItemFocusBorderThickness = 2.0f;

	// FontSize of the CheckMark glyph
	private const float s_checkMarkGlyphFontSize = 16.0f;

	private const string c_strCheckMarkGlyphStorage = "\uE73E";

	// Template specializations for string to enum mappings.
	// Dead WinUI code: the CommonStates, SelectionHintStates and SelectionStates tables.
	private static Array GetMap(Type enumType)
	{
		if (enumType == typeof(FocusStates))
		{
			return new MapItem<FocusStates>[]
			{
				new("Focused",        FocusStates.Focused),
				new("Unfocused",      FocusStates.Unfocused),
				new("PointerFocused", FocusStates.PointerFocused),
			};
		}

		if (enumType == typeof(DragStates))
		{
			return new MapItem<DragStates>[]
			{
				new("NotDragging",               DragStates.NotDragging),
				new("Dragging",                  DragStates.Dragging),
				new("DraggingTarget",            DragStates.DraggingTarget),
				new("MultipleDraggingPrimary",   DragStates.MultipleDraggingPrimary),
				new("MultipleDraggingSecondary", DragStates.MultipleDraggingSecondary),
				new("DraggedPlaceholder",        DragStates.DraggedPlaceholder),
				new("Reordering",                DragStates.Reordering),
				new("ReorderingTarget",          DragStates.ReorderingTarget),
				new("MultipleReorderingPrimary", DragStates.MultipleReorderingPrimary),
				new("ReorderedPlaceholder",      DragStates.ReorderedPlaceholder),
				new("DragOver",                  DragStates.DragOver),
			};
		}

		if (enumType == typeof(ReorderHintStates))
		{
			return new MapItem<ReorderHintStates>[]
			{
				new("NoReorderHint",     ReorderHintStates.NoReorderHint),
				new("BottomReorderHint", ReorderHintStates.BottomReorderHint),
				new("TopReorderHint",    ReorderHintStates.TopReorderHint),
				new("RightReorderHint",  ReorderHintStates.RightReorderHint),
				new("LeftReorderHint",   ReorderHintStates.LeftReorderHint),
			};
		}

		if (enumType == typeof(DataVirtualizationStates))
		{
			return new MapItem<DataVirtualizationStates>[]
			{
				new("DataAvailable",   DataVirtualizationStates.DataAvailable),
				new("DataPlaceholder", DataVirtualizationStates.DataPlaceholder),
			};
		}

		if (enumType == typeof(CommonStates2))
		{
			return new MapItem<CommonStates2>[]
			{
				new("Normal",              CommonStates2.Normal),
				new("PointerOver",         CommonStates2.PointerOver),
				new("Pressed",             CommonStates2.Pressed),
				new("Selected",            CommonStates2.Selected),
				new("PointerOverSelected", CommonStates2.PointerOverSelected),
				new("PressedSelected",     CommonStates2.PressedSelected),
			};
		}

		if (enumType == typeof(DisabledStates))
		{
			return new MapItem<DisabledStates>[]
			{
				new("Enabled",  DisabledStates.Enabled),
				new("Disabled", DisabledStates.Disabled),
			};
		}

		if (enumType == typeof(MultiSelectStates))
		{
			return new MapItem<MultiSelectStates>[]
			{
				new("MultiSelectDisabled", MultiSelectStates.MultiSelectDisabled),
				new("MultiSelectEnabled",  MultiSelectStates.MultiSelectEnabled),
			};
		}

		if (enumType == typeof(SelectionIndicatorStates))
		{
			return new MapItem<SelectionIndicatorStates>[]
			{
				new("SelectionIndicatorDisabled", SelectionIndicatorStates.SelectionIndicatorDisabled),
				new("SelectionIndicatorEnabled",  SelectionIndicatorStates.SelectionIndicatorEnabled),
			};
		}

		throw new ArgumentOutOfRangeException(nameof(enumType));
	}

	// Helpers

	private void LayoutRoundHelper(ref Size size)
	{
		size.Width = LayoutRound(size.Width);
		size.Height = LayoutRound(size.Height);
	}

	private void LayoutRoundHelper(ref Thickness thickness)
	{
		thickness.Left = LayoutRound(thickness.Left);
		thickness.Right = LayoutRound(thickness.Right);
		thickness.Top = LayoutRound(thickness.Top);
		thickness.Bottom = LayoutRound(thickness.Bottom);
	}

	private Rect LayoutRoundHelper(Rect bounds)
	{
		Rect newBounds = default;

		newBounds.X = LayoutRound(bounds.X);
		newBounds.Y = LayoutRound(bounds.Y);
		var right = LayoutRound(bounds.Width + bounds.X);
		var bottom = LayoutRound(bounds.Height + bounds.Y);
		newBounds.Width = right - newBounds.X;
		newBounds.Height = bottom - newBounds.Y;

		return newBounds;
	}

	private bool ShouldUseLayoutRounding()
	{
		// Similar to what Borders do, but we don't care about corner radius (ours is always 0).
		var scale = RootScale.GetRasterizationScaleForElement(this);
		return (scale != 1.0f) && GetUseLayoutRounding();
	}

	private static void AddRectangle(
		ChromeContentRenderer pContentRenderer,
		Rect bounds,
		Brush pBrush,
		UIElement? pUIElement
		)
	{
		if (!IsNullCompositionBrush(pBrush))
		{
			pContentRenderer.AddRectangle(bounds, pBrush);
		}
	}

	private static void AddBorder(
		ChromeContentRenderer pContentRenderer,
		Rect bounds,
		Thickness thickness,
		Brush pBrush,
		ListViewBaseItemPresenter? listViewItemChrome
		)
	{
		if (!IsNullCompositionBrush(pBrush))
		{
			// Uno-specific: a hollow rectangle at the bounds, in place of WinUI's ninegrid translated to (X, Y).
			pContentRenderer.AddBorder(bounds, thickness, pBrush);
		}
	}

	// Uno-specific: the Inline state fill goes to the element background (IBorderInfoProvider.Background), which carries BackgroundTransition.
	private static void AddElementBackgroundRectangle(
		ChromeContentRenderer pContentRenderer,
		Brush pBrush
		)
	{
		if (!IsNullCompositionBrush(pBrush))
		{
			pContentRenderer.AddElementBackground(pBrush);
		}
	}

	// Dead WinUI code: AddChromeAssociatedPath (C:331-376).
}

// Commands

partial class ListViewBaseItemAnimationCommand
{
	private protected ListViewBaseItemAnimationCommand(bool isStarting, bool steadyStateOnly)
	{
		m_isStarting = isStarting;
		m_steadyStateOnly = steadyStateOnly;
	}
}

partial class ListViewBaseItemAnimationCommand_Pressed
{
	internal ListViewBaseItemAnimationCommand_Pressed(
		bool pressed,
		WeakReference<UIElement> pAnimationTarget,
		bool isStarting,
		bool steadyStateOnly)
		: base(isStarting, steadyStateOnly)
	{
		m_pressed = pressed;
		m_pAnimationTarget = pAnimationTarget;
	}

	internal override void Accept(IListViewBaseItemAnimationCommandVisitor visitor) => visitor.VisitAnimationCommand(this);

	internal override ListViewBaseItemAnimationCommand Clone()
		=> new ListViewBaseItemAnimationCommand_Pressed(m_pressed, m_pAnimationTarget, m_isStarting, m_steadyStateOnly);

	internal override int GetPriority() => 3;
}

partial class ListViewBaseItemAnimationCommand_ReorderHint
{
	internal ListViewBaseItemAnimationCommand_ReorderHint(
		float offsetX,
		float offsetY,
		WeakReference<ListViewBaseItemPresenter> pAnimationTarget,
		bool isStarting,
		bool steadyStateOnly)
		: base(isStarting, steadyStateOnly)
	{
		m_pAnimationTarget = pAnimationTarget;
		m_offsetX = offsetX;
		m_offsetY = offsetY;
	}

	internal override void Accept(IListViewBaseItemAnimationCommandVisitor visitor) => visitor.VisitAnimationCommand(this);

	internal override ListViewBaseItemAnimationCommand Clone()
		=> new ListViewBaseItemAnimationCommand_ReorderHint(m_offsetX, m_offsetY, m_pAnimationTarget, m_isStarting, m_steadyStateOnly);

	internal override int GetPriority() => 1;
}

partial class ListViewBaseItemAnimationCommand_DragDrop
{
	internal ListViewBaseItemAnimationCommand_DragDrop(
		DragDropState state,
		WeakReference<ListViewBaseItemPresenter> pBaseAnimationTarget,
		WeakReference<FrameworkElement> pFadeOutAnimationTarget,
		bool isStarting,
		bool steadyStateOnly)
		: base(isStarting, steadyStateOnly)
	{
		m_pBaseAnimationTarget = pBaseAnimationTarget;
		m_pFadeOutAnimationTarget = pFadeOutAnimationTarget;
		m_state = state;
	}

	internal override void Accept(IListViewBaseItemAnimationCommandVisitor visitor) => visitor.VisitAnimationCommand(this);

	internal override ListViewBaseItemAnimationCommand Clone()
		=> new ListViewBaseItemAnimationCommand_DragDrop(m_state, m_pBaseAnimationTarget, m_pFadeOutAnimationTarget, m_isStarting, m_steadyStateOnly);

	internal override int GetPriority() => (m_state == DragDropState.Target) ? 1 : 0;
}

partial class ListViewBaseItemAnimationCommand_MultiSelect
{
	internal ListViewBaseItemAnimationCommand_MultiSelect(
		bool isRoundedListViewBaseItemChromeEnabled,
		bool entering,
		double checkBoxTranslationX,
		double contentTranslationX,
		ListViewItemPresenterCheckMode checkMode,
		WeakReference<UIElement> multiSelectCheckBox,
		WeakReference<UIElement> contentPresenter,
		bool isStarting,
		bool steadyStateOnly)
		: base(isStarting, steadyStateOnly)
	{
		m_isRoundedListViewBaseItemChromeEnabled = isRoundedListViewBaseItemChromeEnabled;
		m_entering = entering;
		m_checkBoxTranslationX = checkBoxTranslationX;
		m_contentTranslationX = contentTranslationX;
		m_checkMode = checkMode;
		m_multiSelectCheckBox = multiSelectCheckBox;
		m_contentPresenter = contentPresenter;
	}

	internal override void Accept(IListViewBaseItemAnimationCommandVisitor visitor) => visitor.VisitAnimationCommand(this);

	internal override ListViewBaseItemAnimationCommand Clone()
		=> new ListViewBaseItemAnimationCommand_MultiSelect(
			m_isRoundedListViewBaseItemChromeEnabled,
			m_entering,
			m_checkBoxTranslationX,
			m_contentTranslationX,
			m_checkMode,
			m_multiSelectCheckBox,
			m_contentPresenter,
			m_isStarting,
			m_steadyStateOnly);

	internal override int GetPriority() => 3;
}

partial class ListViewBaseItemAnimationCommand_IndicatorSelect
{
	internal ListViewBaseItemAnimationCommand_IndicatorSelect(
		bool entering,
		double translationX,
		ListViewItemPresenterSelectionIndicatorMode selectionIndicatorMode,
		WeakReference<UIElement> selectionIndicator,
		WeakReference<UIElement> contentPresenter,
		bool isStarting,
		bool steadyStateOnly)
		: base(isStarting, steadyStateOnly)
	{
		m_entering = entering;
		m_translationX = translationX;
		m_selectionIndicatorMode = selectionIndicatorMode;
		m_selectionIndicator = selectionIndicator;
		m_contentPresenter = contentPresenter;
	}

	internal override void Accept(IListViewBaseItemAnimationCommandVisitor visitor) => visitor.VisitAnimationCommand(this);

	internal override ListViewBaseItemAnimationCommand Clone()
		=> new ListViewBaseItemAnimationCommand_IndicatorSelect(
			m_entering,
			m_translationX,
			m_selectionIndicatorMode,
			m_selectionIndicator,
			m_contentPresenter,
			m_isStarting,
			m_steadyStateOnly);

	internal override int GetPriority() => 3;
}

partial class ListViewBaseItemAnimationCommand_SelectionIndicatorVisibility
{
	internal ListViewBaseItemAnimationCommand_SelectionIndicatorVisibility(
		bool selected,
		double fromScale,
		WeakReference<UIElement> selectionIndicator,
		bool isStarting,
		bool steadyStateOnly)
		: base(isStarting, steadyStateOnly)
	{
		m_selected = selected;
		m_fromScale = fromScale;
		m_selectionIndicator = selectionIndicator;
	}

	internal override void Accept(IListViewBaseItemAnimationCommandVisitor visitor) => visitor.VisitAnimationCommand(this);

	internal override ListViewBaseItemAnimationCommand Clone()
		=> new ListViewBaseItemAnimationCommand_SelectionIndicatorVisibility(
			m_selected,
			m_fromScale,
			m_selectionIndicator,
			m_isStarting,
			m_steadyStateOnly);

	internal override int GetPriority() => 3;
}

// CListViewBaseItemChrome

partial class ListViewBaseItemPresenter
{
	internal ListViewBaseItemPresenter()
	{
		m_shouldRenderChrome = true;
		m_isInIndicatorSelect = false;
		m_isInMultiSelect = false;

		m_visualStates.focusState = FocusStates.Unfocused;
		m_visualStates.dragState = DragStates.NotDragging;
		m_visualStates.reorderHintState = ReorderHintStates.NoReorderHint;
		m_visualStates.dataVirtualizationState = DataVirtualizationStates.DataAvailable;

		m_visualStates.commonState2 = CommonStates2.Normal;
		m_visualStates.disabledState = DisabledStates.Enabled;
		m_visualStates.multiSelectState = MultiSelectStates.MultiSelectDisabled;
		m_visualStates.selectionIndicatorState = SelectionIndicatorStates.SelectionIndicatorDisabled;
	}

	// Uno-specific: the C++ destructor only deletes the queued animation commands and releases brushes; the GC owns both here.

	// Given a string state name and a pointer to a visual state:
	// - tries to parse the string as the appropriate VisualState type.
	// - If parsing successful, updates the value at the pointer.
	// - If the value at the pointer was updated, return TRUE. FALSE otherwise.
	internal static bool UpdateVisualStateGroup<TVisualState>(string name, ref TVisualState state, out bool stateFound)
		where TVisualState : struct, Enum
	{
		var map = Mapping<TVisualState>.s_map;
		stateFound = false;

		for (var i = 0; i < map.Length; ++i)
		{
			ref readonly var item = ref map[i];

			if (string.Equals(item.m_pName, name, StringComparison.Ordinal))
			{
				stateFound = true;

				if (!EqualityComparer<TVisualState>.Default.Equals(item.m_pEnumValue, state))
				{
					state = item.m_pEnumValue;
					return true;
				}

				return false;
			}
		}

		return false;
	}

	// Dead WinUI code: AppendCheckmarkTransform and AppendEarmarkTransform (C:766-818).

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize) => MeasureNewStyle(availableSize);

	/// <inheritdoc />
	protected override Size ArrangeOverride(Size finalSize) => ArrangeNewStyle(finalSize);

	private protected sealed override bool HasTemplateChild() => GetTemplateChildIfExists() != null;

	private protected override void AddTemplateChild(UIElement child)
	{
		global::System.Diagnostics.Debug.Assert(GetTemplateChildIfExists() == null);

		AddChild(child, m_backplateRectangle is not null ? 1 : 0);
	}

	private protected override void RemoveTemplateChild()
	{
		var templateChild = GetTemplateChildIfExists();

		if (templateChild != null)
		{
			RemoveChild(templateChild);
		}
	}

	private protected override UIElement? GetTemplateChildNoRef() => GetTemplateChildIfExists();

	internal UIElement? GetTemplateChildIfExists()
	{
		var children = GetChildren();
		var templateChild = children.Count > 0 ? children[0] : null;

		if (m_backplateRectangle is not null && templateChild == m_backplateRectangle)
		{
			if (children.Count > 1)
			{
				templateChild = children[1];
			}
		}

		if (templateChild != m_pDragItemsCountTextBlock &&
			templateChild != m_pSecondaryChrome &&
			templateChild != m_multiSelectCheckBoxRectangle &&
			templateChild != m_selectionIndicatorRectangle &&
			templateChild != m_innerSelectionBorder &&
			templateChild != m_outerBorder &&
			templateChild != m_backplateRectangle)
		{
			return templateChild;
		}
		else
		{
			return null;
		}
	}

	// Raise exception if no appropriate parent is set.
	private void SetWrongParentError()
	{
		string? message = null;

		if (this is GridViewItemPresenter)
		{
			message = string.Format(global::System.Globalization.CultureInfo.InvariantCulture, c_wrongParentErrorFormat, "GridViewItemPresenter", "GridViewItem");
		}
		else if (this is ListViewItemPresenter)
		{
			message = string.Format(global::System.Globalization.CultureInfo.InvariantCulture, c_wrongParentErrorFormat, "ListViewItemPresenter", "ListViewItem");
		}

		throw message is null ? new InvalidOperationException() : new InvalidOperationException(message);
	}

	// AG_E_RUNTIME_CHROME_WRONG_PARENT (agcore.debug/CommonErrors.rc)
	private const string c_wrongParentErrorFormat = "{0} can only be used as the first child in the template for a {1}.";

	// Retrieve parent item or raise exception if no parent is set.
	private ContentControl GetParentListViewBaseItemNoRef()
	{
		if (m_pParentListViewBaseItemNoRef is null)
		{
			SetWrongParentError();
		}

		return m_pParentListViewBaseItemNoRef!;
	}

	private static ListViewBase? GetParentListViewBase(ContentControl? listViewBaseItem)
	{
		DependencyObject? parent = listViewBaseItem;
		while (parent != null && parent is not ListViewBase)
		{
			// TODO Uno: WinUI uses GetParentFollowPopups.
			parent = parent.GetParent() as DependencyObject;
		}

		// Uno-specific: recycled containers are detached while they are prepared, so fall back to the owning ItemsControl.
		if (parent is null && listViewBaseItem is not null)
		{
			return ItemsControl.ItemsControlFromItemContainer(listViewBaseItem) as ListViewBase;
		}

		return parent as ListViewBase;
	}

	private protected void GoToChromedState(
		string pStateName,
		bool useTransitions,
		out bool pWentToState)
		=> GoToChromedStateNewStyle(pStateName, useTransitions, out pWentToState);

	// Dead WinUI code: DrawBaseLayer, DrawUnderContentLayer, DrawOverContentLayer and DrawDragOverlayLayer (C:995-1341).

	private void RenderLayer(
		ChromeContentRenderer pContentRenderer,
		ListViewBaseItemChromeLayerPosition layer
		)
	{
		if (m_shouldRenderChrome && ActualWidth > 0 && ActualHeight > 0)
		{
			Rect bounds = new(0.0f, 0.0f, ActualWidth, ActualHeight);

			if (ShouldUseLayoutRounding())
			{
				bounds = LayoutRoundHelper(bounds);
			}

			if (ShouldDrawUnderContentLayerHere(layer))
			{
				DrawUnderContentLayerNewStyle(pContentRenderer, bounds);
			}

			if (ShouldDrawOverContentLayerHere(layer))
			{
				DrawOverContentLayerNewStyle(pContentRenderer, bounds);
			}
		}
	}

	// Dead WinUI code: ShouldDrawBaseLayerHere (C:1372-1377).

	// Returns TRUE if the under-content layer graphics should be drawn at the given layer.
	private static bool ShouldDrawUnderContentLayerHere(ListViewBaseItemChromeLayerPosition layer)
		=> layer == ListViewBaseItemChromeLayerPosition.PrimaryChrome_Pre;

	// Returns TRUE if the over-content layer graphics should be drawn at the given layer.
	private bool ShouldDrawOverContentLayerHere(ListViewBaseItemChromeLayerPosition layer)
	{
		// Order of precedence matters!
		// Default to Primary Chrome Post.
		var targetLayer = ListViewBaseItemChromeLayerPosition.PrimaryChrome_Post;

		// Some states require us to draw the over-content layer on the secondary chrome instead, so we can animate it.
		if (m_visualStates.HasState(DragStates.MultipleDraggingPrimary) || m_visualStates.HasState(DragStates.Dragging) ||
			m_visualStates.HasState(DragStates.MultipleReorderingPrimary) || m_visualStates.HasState(DragStates.Reordering))
		{
			targetLayer = ListViewBaseItemChromeLayerPosition.SecondaryChrome_Post;
		}

		return layer == targetLayer;
	}

	// Obtains the next animation command to execute, or NULL if none exists. Note that the ref
	// to the returned item is still controlled by the chrome - see UnlockLayersForAnimationAndDisposeCommand.
	internal ListViewBaseItemAnimationCommand? GetNextPendingAnimation() => DequeueAnimationCommand();

	// Tells us to assign the layer positions of the various chrome visuals to match the requirements of the given
	// command. Returns TRUE if the layers could be rearranged (meaning the animation can proceed), FALSE otherwise.
	internal bool LockLayersForAnimation(ListViewBaseItemAnimationCommand pCommand)
	{
		var commandPriority = pCommand.GetPriority();
		bool shouldProceed;

		if (m_currentHighestCommandPriority >= commandPriority)
		{
			shouldProceed = true;

			// Generate a stop command for the current animation if we're supplanting it.
			if (m_pCurrentHighestPriorityCommand is not null &&
				m_currentHighestCommandPriority != commandPriority)
			{
				var animationCommand = m_pCurrentHighestPriorityCommand.Clone();

				animationCommand.m_isStarting = false;

				EnqueueAnimationCommand(animationCommand);
			}

			m_pCurrentHighestPriorityCommand = pCommand;
			m_currentHighestCommandPriority = commandPriority;
		}
		else
		{
			shouldProceed = false;
		}

		return shouldProceed;
	}

	// Unlocks the layers for the given command, and releases the command.
	internal void UnlockLayersForAnimationAndDisposeCommand(ListViewBaseItemAnimationCommand? command)
	{
		if (m_pCurrentHighestPriorityCommand == command)
		{
			m_pCurrentHighestPriorityCommand = null; // Will delete below.
			m_currentHighestCommandPriority = int.MaxValue;
		}

		// Uno-specific: no delete, the GC collects the command.
	}

	// Reparents the inner selection border if needed and updates its affected properties.
	// According to the visual design, the inner and outer borders are meant to be overlapping each other. In order to avoid bleed through at the borders' edges though,
	// we can afford to host a bloated inner border inside the outer border when the latter is opaque. That trick is not applicable when the outer border is semi-transparent,
	// and that is OK since the bleed through is not obvious in those cases.
	internal void UpdateBordersParenting()
	{
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
		global::System.Diagnostics.Debug.Assert(m_outerBorder is not null);

		if (m_innerSelectionBorder is null)
		{
			return;
		}

		var isOuterBorderBrushOpaque = IsOuterBorderBrushOpaque();
		var areBordersNested = m_outerBorder == m_innerSelectionBorder.GetParent();

		if (isOuterBorderBrushOpaque != areBordersNested)
		{
			if (areBordersNested)
			{
				m_outerBorder!.Child = null;
			}
			else
			{
				RemoveChild(m_innerSelectionBorder);
			}

			if (isOuterBorderBrushOpaque)
			{
				m_outerBorder!.Child = m_innerSelectionBorder;
			}
			else
			{
				AddChild(m_innerSelectionBorder);
			}

			SetInnerSelectionBorderProperties();
		}
	}

	// If necessary, creates all the TransitionTargets necessary for our animation targets.
	private void EnsureTransitionTarget()
	{
		var pTemplateChild = GetTemplateChildIfExists();

		if (!HasTransitionTarget())
		{
			TransitionTarget = new TransitionTarget();
		}

		if (m_pSecondaryChrome is not null &&
			!m_pSecondaryChrome.HasTransitionTarget())
		{
			m_pSecondaryChrome.TransitionTarget = new TransitionTarget();
		}

		if (m_pParentListViewBaseItemNoRef is not null &&
			!m_pParentListViewBaseItemNoRef.HasTransitionTarget())
		{
			m_pParentListViewBaseItemNoRef.TransitionTarget = new TransitionTarget();
		}

		if (pTemplateChild is not null &&
			!pTemplateChild.HasTransitionTarget())
		{
			pTemplateChild.TransitionTarget = new TransitionTarget();
		}
	}

	// The "ListViewBaseItemRoundedChromeEnabled" theme resource value is used to turn on/off the rendering with rounded corners.
	// For performance reasons, the resource is only evaluated once. TAEF tests can invalidate the cache by calling TestServices::Utilities::DeleteResourceDictionaryCaches().
	internal bool IsRoundedListViewBaseItemChromeEnabled()
	{
		s_isRoundedListViewBaseItemChromeEnabled ??= IsRoundedListViewBaseItemChromeEnabledStatic();

		return s_isRoundedListViewBaseItemChromeEnabled.Value;
	}

	internal bool IsChromeForGridViewItem() => m_pParentListViewBaseItemNoRef is GridViewItem;

	internal bool IsChromeForListViewItem() => m_pParentListViewBaseItemNoRef is ListViewItem;

	// Uses the ListViewItemPresenter.SelectionIndicatorVisualEnabled property as well as RuntimeEnabledFeature's DenySelectionIndicatorVisualEnabled/ForceSelectionIndicatorVisualEnabled
	// overwrites to determine whether a ListViewItem should render the selection indicator or not.
	internal bool IsSelectionIndicatorVisualEnabled()
	{
		if (!IsChromeForListViewItem() ||
			this is not ListViewItemPresenter listViewItemPresenter)
		{
			return false;
		}

		if (ListViewBaseItemChromeRuntimeFeatures.DenySelectionIndicatorVisualEnabled)
		{
			return false;
		}

		return (IsRoundedListViewBaseItemChromeEnabled() && listViewItemPresenter.SelectionIndicatorVisualEnabled) || ListViewBaseItemChromeRuntimeFeatures.ForceSelectionIndicatorVisualEnabled;
	}

	// Indicates whether the selection indicator is allowed to be rendered and the SelectionMode is either Single or Extended.
	internal bool IsInSelectionIndicatorMode()
	{
		if (!IsSelectionIndicatorVisualEnabled())
		{
			return false;
		}

		var listViewBaseNoRef = GetParentListViewBase(m_pParentListViewBaseItemNoRef);

		if (listViewBaseNoRef is null)
		{
			return false;
		}

		var selectionMode = listViewBaseNoRef.SelectionMode;

		return selectionMode == ListViewSelectionMode.Single || selectionMode == ListViewSelectionMode.Extended;
	}

	// Returns True when the GridViewItem's outer border is present and uses an opaque brush.
	private bool IsOuterBorderBrushOpaque()
	{
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());

		if (m_outerBorder is null)
		{
			return false;
		}

		var outerBorderBrush = m_outerBorder.BorderBrush;

		return outerBorderBrush is not null && IsOpaqueBrush(outerBorderBrush);
	}

	// Sets the border CornerRadius based on value returned by GetGeneralCornerRadius.
	private void SetGeneralCornerRadius(Border border)
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(border is not null);

		var cornerRadius = GetGeneralCornerRadius();

		border!.CornerRadius = cornerRadius;
	}

	// Returns this ListViewBaseItemPresenter's CornerRadius when set,
	// or the default 4.0 value when rounded corner rendering is forced.
	internal CornerRadius GetGeneralCornerRadius()
	{
		var cornerRadius = CornerRadius;

		if (IsRoundedListViewBaseItemChromeForced() &&
			cornerRadius.BottomLeft == 0.0f &&
			cornerRadius.BottomRight == 0.0f &&
			cornerRadius.TopLeft == 0.0f &&
			cornerRadius.TopRight == 0.0f)
		{
			cornerRadius = new CornerRadius(s_generalCornerRadius);
		}

		return cornerRadius;
	}

	// Returns this ListViewBaseItemPresenter's CheckBoxCornerRadius when set,
	// or the default 3.0 value when rounded corner rendering is forced.
	internal CornerRadius GetCheckBoxCornerRadius()
	{
		CornerRadius cornerRadius = default;

		if (this is ListViewItemPresenter listViewItemPresenter)
		{
			cornerRadius = listViewItemPresenter.CheckBoxCornerRadius;

			if (IsRoundedListViewBaseItemChromeForced() &&
				cornerRadius.BottomLeft == 0.0f &&
				cornerRadius.BottomRight == 0.0f &&
				cornerRadius.TopLeft == 0.0f &&
				cornerRadius.TopRight == 0.0f)
			{
				cornerRadius = new CornerRadius(GetDefaultCheckBoxCornerRadius());
			}
		}

		return cornerRadius;
	}

	// Returns this ListViewBaseItemPresenter's SelectionIndicatorCornerRadius when set,
	// or the default 1.5 value when rounded corner rendering is forced.
	internal CornerRadius GetSelectionIndicatorCornerRadius()
	{
		CornerRadius cornerRadius = default;

		if (this is ListViewItemPresenter listViewItemPresenter)
		{
			cornerRadius = listViewItemPresenter.SelectionIndicatorCornerRadius;

			if (IsRoundedListViewBaseItemChromeForced() &&
				cornerRadius.BottomLeft == 0.0f &&
				cornerRadius.BottomRight == 0.0f &&
				cornerRadius.TopLeft == 0.0f &&
				cornerRadius.TopRight == 0.0f)
			{
				cornerRadius = new CornerRadius(GetDefaultSelectionIndicatorCornerRadius());
			}
		}

		return cornerRadius;
	}

	// Returns the selection indicator's desired height given the ListViewItem's available height.
	internal float GetSelectionIndicatorHeightFromAvailableHeight(float availableHeight)
	{
		if (availableHeight <= s_selectionIndicatorSize.Height)
		{
			return availableHeight;
		}

		return (float)Math.Max(s_selectionIndicatorSize.Height, availableHeight - s_selectionIndicatorMargin.Top - s_selectionIndicatorMargin.Bottom);
	}

	// Uses the ListViewItemPresenter.SelectionIndicatorMode property as well as RuntimeEnabledFeature's ForceSelectionIndicatorModeInline/ForceSelectionIndicatorModeOverlay
	// overwrites to determine whether the selection indicator must rendered inline or overlayed.
	internal ListViewItemPresenterSelectionIndicatorMode GetSelectionIndicatorMode()
	{
		if (ListViewBaseItemChromeRuntimeFeatures.ForceSelectionIndicatorModeInline)
		{
			return ListViewItemPresenterSelectionIndicatorMode.Inline;
		}

		if (ListViewBaseItemChromeRuntimeFeatures.ForceSelectionIndicatorModeOverlay)
		{
			return ListViewItemPresenterSelectionIndicatorMode.Overlay;
		}

		// WinUI reads the sparse ListViewItemPresenter DP regardless of type, so a GridViewItemPresenter gets its default.
		if (this is not ListViewItemPresenter)
		{
			return ListViewItemPresenterSelectionIndicatorMode.Overlay;
		}

		return (ListViewItemPresenterSelectionIndicatorMode)GetValue(ListViewItemPresenter.SelectionIndicatorModeProperty);
	}

	internal Brush? GetRevealBackgroundBrushNoRef()
	{
		if (this is not ListViewItemPresenter listViewItemPresenter)
		{
			return null;
		}

		return listViewItemPresenter.RevealBackground;
	}

	internal Brush? GetRevealBorderBrushNoRef()
	{
		if (this is not ListViewItemPresenter listViewItemPresenter)
		{
			return null;
		}

		return listViewItemPresenter.RevealBorderBrush;
	}

	internal bool GetRevealBackgroundShowsAboveContent()
	{
		if (this is not ListViewItemPresenter listViewItemPresenter)
		{
			return false;
		}

		return listViewItemPresenter.RevealBackgroundShowsAboveContent;
	}

	internal bool GetSelectionCheckMarkVisualEnabled()
		=> (bool)GetValue(this is ListViewItemPresenter ? ListViewItemPresenter.SelectionCheckMarkVisualEnabledProperty : GridViewItemPresenter.SelectionCheckMarkVisualEnabledProperty);

	internal Thickness GetRevealBorderThickness()
	{
		global::System.Diagnostics.Debug.Assert(this is ListViewItemPresenter);

		return (Thickness)GetValue(ListViewItemPresenter.RevealBorderThicknessProperty);
	}

	internal Thickness GetSelectedBorderThickness()
		=> (Thickness)GetValue(this is ListViewItemPresenter ? ListViewItemPresenter.SelectedBorderThicknessProperty : GridViewItemPresenter.SelectedBorderThicknessProperty);

	// Dead WinUI code: GetPointerOverBackgroundMargin (C:1860-1869) is only used by the removed old-style chrome.

	// WinUI quirk: the opacities and the offset are stored as floats.
	internal float GetDisabledOpacity()
		=> (float)(double)GetValue(this is ListViewItemPresenter ? ListViewItemPresenter.DisabledOpacityProperty : GridViewItemPresenter.DisabledOpacityProperty);

	internal float GetDragOpacity()
		=> (float)(double)GetValue(this is ListViewItemPresenter ? ListViewItemPresenter.DragOpacityProperty : GridViewItemPresenter.DragOpacityProperty);

	internal float GetReorderHintOffset()
		=> (float)(double)GetValue(this is ListViewItemPresenter ? ListViewItemPresenter.ReorderHintOffsetProperty : GridViewItemPresenter.ReorderHintOffsetProperty);

	// TODO Uno: NWSetContentDirty / NWCleanDirtyFlags (C:1904-1916) have no Uno dirty-flag render model.

	// Dead WinUI code: PrepareCheckPath, AddLineSegmentToSegmentCollection and GetCheckMarkBounds (C:1919-2007).

	// Sets whether or not the drag overlay text block is shown.
	internal void SetDragOverlayTextBlockVisible(bool isVisible)
	{
		if (isVisible)
		{
			EnsureDragOverlayTextBlock();

			EnsureMultiSelectCheckBox();

			// For RS1, drag items count text will show inside a border, and a background is also applied to the text.
			// So, we re-use the multi-select checkbox to have the drag count textblock as its child. This will produce the effect of a border around the text.
			// WinUI quirk: the check box Clip and child are never restored when the overlay is hidden again.
			m_multiSelectCheckBoxRectangle!.Clip = null;
			m_multiSelectCheckBoxRectangle.Child = m_pDragItemsCountTextBlock;

			const string c_borderBrush = "SystemControlBackgroundChromeWhiteBrush";
			var borderBrush = Uno.UI.Xaml.Core.CoreServices.Instance.LookupThemeResource(c_borderBrush);
			if (borderBrush != null)
			{
				m_multiSelectCheckBoxRectangle.BorderBrush = borderBrush as Brush;
			}

			const string c_background = "SystemControlBackgroundAccentBrush";
			var backgroundBrush = Uno.UI.Xaml.Core.CoreServices.Instance.LookupThemeResource(c_background);
			if (backgroundBrush != null)
			{
				m_multiSelectCheckBoxRectangle.Background = backgroundBrush as Brush;
			}

			m_multiSelectCheckBoxRectangle.Visibility = Visibility.Visible;

			if (m_checkMode == ListViewItemPresenterCheckMode.Overlay)
			{
				// For GridView, the item count should show in the center just like in File Explorer
				m_multiSelectCheckBoxRectangle.BorderThickness = new Thickness(2.0, 2.0, 2.0, 2.0);

				m_multiSelectCheckBoxRectangle.VerticalAlignment = VerticalAlignment.Center;

				m_multiSelectCheckBoxRectangle.HorizontalAlignment = HorizontalAlignment.Center;
			}

			InvalidateMeasure();
		}

		if (m_pDragItemsCountTextBlock is not null)
		{
			m_pDragItemsCountTextBlock.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
		}
	}

	// Sets the drag count display.
	internal void SetDragItemsCount(uint dragItemsCount)
	{
		EnsureDragOverlayTextBlock();

		m_pDragItemsCountTextBlock!.Text = dragItemsCount.ToString(CultureInfo.InvariantCulture);
	}

	internal void SetSwipeHintCheckOpacity(float opacity)
	{
		if (m_swipeHintCheckOpacity != opacity)
		{
			m_swipeHintCheckOpacity = opacity;

			// TODO Uno: CContentControl::NWSetContentDirty(m_pParentListViewBaseItemNoRef, DirtyFlags::Bounds) has no Uno dirty-flag render model.
		}
	}

	// Sets up and adds the drag overlay text block to the tree.
	private void EnsureDragOverlayTextBlock()
	{
		if (m_pDragItemsCountTextBlock is null)
		{
			m_pDragItemsCountTextBlock = new TextBlock();
			SetDragOverlayTextBlockProperties();
		}
	}

	private void SetDragOverlayTextBlockProperties()
	{
		if (m_pDragItemsCountTextBlock is not null)
		{
			m_pDragItemsCountTextBlock.IsHitTestVisible = false;

			AutomationProperties.SetAccessibilityView(m_pDragItemsCountTextBlock, AccessibilityView.Raw);

			// Keep the drag items count textblock collapsed by default. It is made visible on demand when we change visual states.
			m_pDragItemsCountTextBlock.Visibility = Visibility.Collapsed;

			const string c_style = "CaptionTextBlockStyle";
			var style = Uno.UI.Xaml.Core.CoreServices.Instance.LookupThemeResource(c_style);
			if (style != null)
			{
				m_pDragItemsCountTextBlock.Style = style as Style;
			}

			m_pDragItemsCountTextBlock.HorizontalAlignment = HorizontalAlignment.Center;

			m_pDragItemsCountTextBlock.VerticalAlignment = VerticalAlignment.Center;
		}
	}

	// Sets up the secondary chrome and adds it to the tree.
	internal void AddSecondaryChrome()
	{
		if (m_pSecondaryChrome is null)
		{
			m_pSecondaryChrome = new ListViewBaseItemSecondaryChrome();

			m_pSecondaryChrome.m_pPrimaryChromeNoRef = this;
			AddChild(m_pSecondaryChrome);
		}
	}

	// Lets us know we have a parent ListViewBaseItem. We don't take a ref.
	internal void SetChromedListViewBaseItem(UIElement? parent)
	{
		m_pParentListViewBaseItemNoRef = null;

		if (parent is not null)
		{
			m_pParentListViewBaseItemNoRef = parent as ContentControl ?? throw new InvalidOperationException("The chromed item must be a ContentControl.");
		}
	}

	// Enqueue the given command. It will be presented in FIFO order by DequeueAnimationCommand.
	// This function takes control of the command's lifetime. To enforce this, it will clear the
	// given pointer when called (no exceptions).
	private void EnqueueAnimationCommand(ListViewBaseItemAnimationCommand command)
	{
		EnsureTransitionTarget();

		m_animationCommands.Add(command);
	}

	// Dequeues an animation command. You now own the ref (note that GetNextPendingAnimation, a caller
	// to this function, takes logical ownership itself).
	private ListViewBaseItemAnimationCommand? DequeueAnimationCommand()
	{
		ListViewBaseItemAnimationCommand? command;

		if (m_animationCommands.Count != 0)
		{
			command = m_animationCommands[0];
			m_animationCommands.RemoveAt(0);
		}
		else
		{
			command = null;
		}

		return command;
	}

	// Given a reorder hint state, figure out where the hint animation should animate to.
	private Point ComputeReorderHintOffset(ReorderHintStates state)
	{
		var reorderHintOffset = GetReorderHintOffset();
		Point offset = default;

		switch (state)
		{
			case ReorderHintStates.BottomReorderHint:
				offset.Y = reorderHintOffset;
				break;

			case ReorderHintStates.TopReorderHint:
				offset.Y = -reorderHintOffset;
				break;

			case ReorderHintStates.LeftReorderHint:
				offset.X = -reorderHintOffset;
				break;

			case ReorderHintStates.RightReorderHint:
				offset.X = reorderHintOffset;
				break;
		}

		return offset;
	}

	// Dead WinUI code: ComputeSwipeHintOffset (C:2252-2271) is only used by the removed old-style chrome.

	// TODO Uno: GenerateContentBounds (C:2273-2286) has no Uno equivalent; the chrome draws within {0,0,ActualWidth,ActualHeight}.

	// Uno-specific: HitTestLocalInternal (C:2288-2338) is the HitTest override in ListViewBaseItemPresenter.cs.
	// TODO Uno: HitTestLocalInternalPostChildren (C:2340-2360) has no post-children hit test hook.

	internal void InvalidateRender()
	{
		// Uno-specific: WinUI dirties this, the parent item and the secondary chrome (NWSetContentDirty) for the next render walk;
		// Uno has no per-frame walk of the chrome, so the layers are configured right away.
		RenderLayers();
	}

	// Uno-specific: CListViewBaseItemChrome::OnPropertyChanged (C:2377-2383) calls the base then OnPropertyChangedNewStyle;
	// Uno routes the presenter DPs through OnChromePropertyChanged and ContentPresenter.CornerRadius through OnCornerRadiusChanged.

	private Size MeasureNewStyle(Size availableSize)
	{
		var contentMargin = m_contentMargin;
		Thickness controlBorderThickness = default;
		Size totalSize = default;

		if (m_backplateRectangle is null && IsRoundedListViewBaseItemChromeEnabled())
		{
			EnsureBackplate();
		}

		var pTemplateChild = GetTemplateChildIfExists();

		// We can't be used without a parent LVB.
		var pParentListViewBaseItemNoRef = GetParentListViewBaseItemNoRef();

		controlBorderThickness = pParentListViewBaseItemNoRef.BorderThickness;

		if (ShouldUseLayoutRounding())
		{
			LayoutRoundHelper(ref contentMargin);
			LayoutRoundHelper(ref controlBorderThickness);
		}

		if (pTemplateChild is not null)
		{
			var contentAvailableSize = availableSize;
			var contentPrefixWidth = 0.0f;

			// Size available for content is availableSize - content margin - control border
			CSizeUtil.Deflate(ref contentAvailableSize, contentMargin);
			CSizeUtil.Deflate(ref contentAvailableSize, controlBorderThickness);

			// Check to see if we need to offset the content due to the potential CheckBox in Inline MultiSelect state.
			if (GetSelectionCheckMarkVisualEnabled() &&
				m_visualStates.HasState(MultiSelectStates.MultiSelectEnabled) &&
				m_checkMode == ListViewItemPresenterCheckMode.Inline)
			{
				contentPrefixWidth = IsRoundedListViewBaseItemChromeEnabled() ? s_multiSelectRoundedContentOffset : s_listViewItemMultiSelectContentOffset;
			}

			if (IsInSelectionIndicatorMode() &&
				GetSelectionIndicatorMode() == ListViewItemPresenterSelectionIndicatorMode.Inline)
			{
				contentPrefixWidth = Math.Max(contentPrefixWidth, (float)(s_selectionIndicatorMargin.Left + s_selectionIndicatorSize.Width + s_selectionIndicatorMargin.Right));
			}

			// subtract the offset to have the child arrange using the new width
			contentAvailableSize.Width -= contentPrefixWidth;

			pTemplateChild.Measure(contentAvailableSize);
			// Uno-specific: as in ContentPresenter.MeasureOverride.
			pTemplateChild.EnsureLayoutStorage();

			// Use child's desired size for our size.
			totalSize = pTemplateChild.DesiredSize;

			// If SelectionMode is Multiple and a CheckBox is potentially visible (Inline mode), we add the buffer of the CheckBox back.
			// If SelectionMode is Single or Extended and a SelectionIndicator is potentially visible (Inline mode), we add the buffer of the SelectionIndicator back.
			totalSize.Width += contentPrefixWidth;
		}

		// border should be accounted for regardless if there is content.
		CSizeUtil.Inflate(ref totalSize, contentMargin);
		CSizeUtil.Inflate(ref totalSize, controlBorderThickness);

		if (GetSelectionCheckMarkVisualEnabled() && m_multiSelectCheckBoxRectangle is not null)
		{
			m_multiSelectCheckBoxRectangle.Measure(totalSize);
		}

		if (IsSelectionIndicatorVisualEnabled() && m_selectionIndicatorRectangle is not null)
		{
			m_selectionIndicatorRectangle.Measure(totalSize);
		}

		if (m_backplateRectangle is not null)
		{
			global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
			m_backplateRectangle.Measure(totalSize);
		}

		if (m_innerSelectionBorder is not null && m_outerBorder != m_innerSelectionBorder.GetParent())
		{
			global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
			global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
			m_innerSelectionBorder.Measure(totalSize);
		}

		if (m_outerBorder is not null)
		{
			global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
			global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
			m_outerBorder.Measure(totalSize);
		}

		// Minimum size
		{
			var minimumSize = new Size(pParentListViewBaseItemNoRef.MinWidth, pParentListViewBaseItemNoRef.MinHeight);

			if (ShouldUseLayoutRounding())
			{
				LayoutRoundHelper(ref minimumSize);
			}

			totalSize.Width = Math.Max(totalSize.Width, minimumSize.Width);
			totalSize.Height = Math.Max(totalSize.Height, minimumSize.Height);
		}

		return totalSize;
	}

	private Size ArrangeNewStyle(Size finalSize)
	{
		var contentMargin = m_contentMargin;
		var finalBounds = new Rect(0.0, 0.0, finalSize.Width, finalSize.Height);
		Rect controlBorderBounds = default;

		if (ShouldUseLayoutRounding())
		{
			LayoutRoundHelper(ref contentMargin);
		}


		// Peel off content margin whitespace.
		controlBorderBounds = finalBounds;
		CSizeUtil.Deflate(ref controlBorderBounds, contentMargin);

		var contentPrefixWidth = 0.0f;

		if (GetSelectionCheckMarkVisualEnabled() && m_visualStates.HasState(MultiSelectStates.MultiSelectEnabled))
		{
			var multiSelectSquareSize = s_multiSelectSquareSize;

			if (ShouldUseLayoutRounding())
			{
				LayoutRoundHelper(ref multiSelectSquareSize);
			}

			// If checkmark is visible, make sure there's at least enough space to show it.
			finalBounds.Width = Math.Max(finalBounds.Width, multiSelectSquareSize.Width);
			finalBounds.Height = Math.Max(finalBounds.Height, multiSelectSquareSize.Height);

			// Check to see if we need to offset the content due to the CheckBox in MultiSelect state.
			if (m_checkMode == ListViewItemPresenterCheckMode.Inline)
			{
				contentPrefixWidth = IsRoundedListViewBaseItemChromeEnabled() ? s_multiSelectRoundedContentOffset : s_listViewItemMultiSelectContentOffset;
			}
		}

		if (IsInSelectionIndicatorMode())
		{
			var selectionIndicatorSize = new Size(s_selectionIndicatorMargin.Left + s_selectionIndicatorSize.Width + s_selectionIndicatorMargin.Right, s_selectionIndicatorHeightShrinkage + 1.0f);

			if (ShouldUseLayoutRounding())
			{
				LayoutRoundHelper(ref selectionIndicatorSize);
			}

			// If a selection indicator is potentially visible, make sure there's at least enough space to show it.
			finalBounds.Width = Math.Max(finalBounds.Width, selectionIndicatorSize.Width);
			finalBounds.Height = Math.Max(finalBounds.Height, selectionIndicatorSize.Height);

			// Check to see if we need to offset the content due to the Inline selection indicator.
			if (GetSelectionIndicatorMode() == ListViewItemPresenterSelectionIndicatorMode.Inline)
			{
				contentPrefixWidth = (float)Math.Max(contentPrefixWidth, selectionIndicatorSize.Width);
			}
		}

		if (contentPrefixWidth != 0)
		{
			// will be used by ArrangeTemplateChild
			controlBorderBounds.X += contentPrefixWidth;

			// subtract the offset to have the child arrange using the new width
			controlBorderBounds.Width -= contentPrefixWidth;
		}

		if (m_multiSelectCheckBoxRectangle is not null)
		{
			m_multiSelectCheckBoxRectangle.Arrange(finalBounds);
		}

		if (m_selectionIndicatorRectangle is not null)
		{
			var selectionIndicatorBounds = finalBounds;
			var selectionIndicatorHeight = GetSelectionIndicatorHeightFromAvailableHeight((float)finalBounds.Height);

			if (m_visualStates.HasState(CommonStates2.Pressed) ||
				m_visualStates.HasState(CommonStates2.PressedSelected))
			{
				global::System.Diagnostics.Debug.Assert(selectionIndicatorHeight > s_selectionIndicatorHeightShrinkage);
				selectionIndicatorHeight -= s_selectionIndicatorHeightShrinkage;
			}

			var excessAvailableHeight = (float)finalBounds.Height - selectionIndicatorHeight;

			if (excessAvailableHeight > 0)
			{
				selectionIndicatorBounds.Y += excessAvailableHeight / 2.0f;
				selectionIndicatorBounds.Height -= excessAvailableHeight;
			}

			m_selectionIndicatorRectangle.Arrange(selectionIndicatorBounds);
		}

		if (m_backplateRectangle is not null)
		{
			global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
			m_backplateRectangle.Arrange(finalBounds);
		}

		if (m_innerSelectionBorder is not null && m_outerBorder != m_innerSelectionBorder.GetParent())
		{
			global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
			global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
			m_innerSelectionBorder.Arrange(finalBounds);
		}

		if (m_outerBorder is not null)
		{
			global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
			global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
			m_outerBorder.Arrange(finalBounds);
		}

		// TODO Uno: WinUI never lays out the secondary chrome; Uno does not render a never-arranged element, so it gets the primary bounds.
		m_pSecondaryChrome?.Arrange(new Rect(0.0, 0.0, finalBounds.Width, finalBounds.Height));

		var newFinalSize = new Size(finalBounds.Width, finalBounds.Height);

		ArrangeTemplateChild(controlBorderBounds);

		return newFinalSize;
	}

	private void ArrangeTemplateChild(Rect controlBorderBounds)
	{
		var pTemplateChild = GetTemplateChildIfExists();

		if (pTemplateChild is not null)
		{
			var contentAvailableSize = new Size(controlBorderBounds.Width, controlBorderBounds.Height);
			Size contentSize = default;
			Rect contentArrangedBounds = default;
			Thickness controlBorderThickness = default;

			var horizontalContentAlignment = HorizontalContentAlignment;
			var verticalContentAlignment = VerticalContentAlignment;

			// We can't be used without a parent LVB.
			var pParentListViewBaseItemNoRef = GetParentListViewBaseItemNoRef();

			controlBorderThickness = pParentListViewBaseItemNoRef.BorderThickness;

			if (ShouldUseLayoutRounding())
			{
				LayoutRoundHelper(ref controlBorderThickness);
			}

			// control border is not going to be available for content.
			CSizeUtil.Deflate(ref contentAvailableSize, controlBorderThickness);

			// If alignment is Stretch, use entire available size, otherwise control's desired size.
			contentSize.Width = (horizontalContentAlignment == HorizontalAlignment.Stretch) ? contentAvailableSize.Width : pTemplateChild.DesiredSize.Width;
			contentSize.Height = (verticalContentAlignment == VerticalAlignment.Stretch) ? contentAvailableSize.Height : pTemplateChild.DesiredSize.Height;

			FrameworkElement.ComputeAlignmentOffset(
				horizontalContentAlignment,
				verticalContentAlignment,
				contentAvailableSize,
				contentSize,
				out var offsetX,
				out var offsetY);
			contentArrangedBounds.X = offsetX;
			contentArrangedBounds.Y = offsetY;

			// making sure we are still within the boundaries of the control
			if (contentSize.Width > contentAvailableSize.Width)
			{
				contentSize.Width = contentAvailableSize.Width;
			}

			if (contentSize.Height > contentAvailableSize.Height)
			{
				contentSize.Height = contentAvailableSize.Height;
			}

			// Adjust top/left coordinate to account for space used for chrome.
			contentArrangedBounds.X += controlBorderThickness.Left + controlBorderBounds.X;
			contentArrangedBounds.Y += controlBorderThickness.Top + controlBorderBounds.Y;
			contentArrangedBounds.Width = contentSize.Width;
			contentArrangedBounds.Height = contentSize.Height;

			// Uno-specific: as in ContentPresenter.ArrangeOverride.
			pTemplateChild.EnsureLayoutStorage();
			pTemplateChild.Arrange(contentArrangedBounds);
		}
	}

	private void GoToChromedStateNewStyle(
		string pStateName,
		bool useTransitions,
		out bool pWentToState)
	{
		var dirty = false;
		var needsMeasure = false;
		var needsArrange = false;
		var disabledChanged = false;
		var templateChild = GetTemplateChildIfExists();

		var isRoundedListViewBaseItemChromeEnabled = IsRoundedListViewBaseItemChromeEnabled();
		var oldCommonState2 = m_visualStates.commonState2;
		var listViewItemSelectionIndicatorContentOffset = s_selectionIndicatorMargin.Left + s_selectionIndicatorSize.Width + s_selectionIndicatorMargin.Right;

		if (UpdateVisualStateGroup(pStateName, ref m_visualStates.commonState2, out pWentToState))
		{
			ListViewBaseItemAnimationCommand animationCommand;

			var selected =
				m_visualStates.HasState(CommonStates2.Selected) ||
				m_visualStates.HasState(CommonStates2.PressedSelected) ||
				m_visualStates.HasState(CommonStates2.PointerOverSelected);

			var roundedGridViewItem =
				isRoundedListViewBaseItemChromeEnabled && IsChromeForGridViewItem();

			if (selected)
			{
				if (m_multiSelectCheckGlyph is not null)
				{
					m_multiSelectCheckGlyph.Opacity = 1.0;
				}

				if (roundedGridViewItem)
				{
					if (m_outerBorder is null)
					{
						EnsureOuterBorder();
					}
					else
					{
						SetOuterBorderBrush();
						SetOuterBorderThickness();
					}

					if (m_innerSelectionBorder is null)
					{
						EnsureInnerSelectionBorder();
					}
					else
					{
						SetInnerSelectionBorderBrush();
					}
				}
			}
			else
			{
				if (m_multiSelectCheckGlyph is not null)
				{
					m_multiSelectCheckGlyph.Opacity = 0.0;
				}

				if (roundedGridViewItem)
				{
					if (m_innerSelectionBorder is not null)
					{
						RemoveInnerSelectionBorder();
					}

					var renderOuterBorder = !m_visualStates.HasState(DisabledStates.Disabled) && m_visualStates.HasState(CommonStates2.PointerOver);

					if (m_outerBorder is null)
					{
						if (renderOuterBorder)
						{
							EnsureOuterBorder();
						}
					}
					else
					{
						if (!renderOuterBorder)
						{
							RemoveOuterBorder();
						}
						else
						{
							SetOuterBorderBrush();
							SetOuterBorderThickness();
						}
					}
				}
			}

			if (m_multiSelectCheckBoxRectangle is not null)
			{
				SetMultiSelectCheckBoxBackground();
				if (isRoundedListViewBaseItemChromeEnabled)
				{
					SetMultiSelectCheckBoxBorder();
				}
			}

			var isInSelectionIndicatorMode = IsInSelectionIndicatorMode();

			if (isInSelectionIndicatorMode || m_selectionIndicatorRectangle is not null)
			{
				var oldPressed =
					oldCommonState2 == CommonStates2.Pressed ||
					oldCommonState2 == CommonStates2.PressedSelected;
				var pressed =
					m_visualStates.HasState(CommonStates2.Pressed) ||
					m_visualStates.HasState(CommonStates2.PressedSelected);

				var updateSelectionIndicatorVisibility = true;
				var showSelectionIndicator = true;
				var fromScale = 0.0f;

				if (isInSelectionIndicatorMode)
				{
					var oldSelected = oldCommonState2 == CommonStates2.Selected || oldCommonState2 == CommonStates2.PressedSelected || oldCommonState2 == CommonStates2.PointerOverSelected;

					updateSelectionIndicatorVisibility = oldSelected != selected;
					showSelectionIndicator = selected;
				}
				else
				{
					showSelectionIndicator = false;
				}

				if (updateSelectionIndicatorVisibility)
				{
					if (showSelectionIndicator)
					{
						EnsureSelectionIndicator();
					}

					// Trigger animation to show/hide the selection indicator.
					animationCommand = new ListViewBaseItemAnimationCommand_SelectionIndicatorVisibility(
						showSelectionIndicator /*selected*/,
						0.0 /*fromScale*/,
						GetWeakRef<UIElement>(m_selectionIndicatorRectangle),
						true /*isStarting*/,
						!useTransitions /*steadyStateOnly*/);
					EnqueueAnimationCommand(animationCommand);
				}
				else if (m_selectionIndicatorRectangle is not null && showSelectionIndicator && oldPressed != pressed)
				{
					var currentHeight = (float)m_selectionIndicatorRectangle.ActualHeight;

					if (pressed && currentHeight <= s_selectionIndicatorHeightShrinkage)
					{
						// s_selectionIndicatorHeightShrinkage equals 6.  currentHeight may be the rounded down value of s_selectionIndicatorHeightShrinkage + 1,
						// but it is not expected to be smaller than or equal to s_selectionIndicatorHeightShrinkage.
						global::System.Diagnostics.Debug.Assert(false);
						fromScale = 1.0f;
					}
					else
					{
						fromScale = pressed ? currentHeight / (currentHeight - s_selectionIndicatorHeightShrinkage)
											: currentHeight / (currentHeight + s_selectionIndicatorHeightShrinkage);
					}

					// Trigger animation to shrink/expand the selection indicator.
					animationCommand = new ListViewBaseItemAnimationCommand_SelectionIndicatorVisibility(
						true /*selected*/,
						fromScale,
						GetWeakRef<UIElement>(m_selectionIndicatorRectangle),
						true /*isStarting*/,
						!useTransitions /*steadyStateOnly*/);
					EnqueueAnimationCommand(animationCommand);
				}

				if (showSelectionIndicator)
				{
					var pointerOver = m_visualStates.HasState(CommonStates2.PointerOverSelected);
					var updateSelectionIndicatorBackground = updateSelectionIndicatorVisibility;

					if (!updateSelectionIndicatorVisibility)
					{
						updateSelectionIndicatorBackground = ((oldCommonState2 == CommonStates2.PointerOverSelected) != pointerOver) || ((oldCommonState2 == CommonStates2.PressedSelected) != pressed);
					}

					if (updateSelectionIndicatorBackground)
					{
						SetSelectionIndicatorBackground();
					}
				}

				if (oldPressed != pressed)
				{
					needsArrange = true;
				}
			}

			SetForegroundBrush();

			if (!isRoundedListViewBaseItemChromeEnabled)
			{
				if (m_visualStates.HasState(CommonStates2.Pressed) ||
					m_visualStates.HasState(CommonStates2.PressedSelected))
				{
					animationCommand = new ListViewBaseItemAnimationCommand_Pressed(
						true /* pressed */,
						GetWeakRef(templateChild),
						true /*isStarting*/,
						!useTransitions);
					EnqueueAnimationCommand(animationCommand);
				}
				else if (m_visualStates.HasState(CommonStates2.Normal) ||
					m_visualStates.HasState(CommonStates2.PointerOver) ||
					m_visualStates.HasState(CommonStates2.Selected) ||
					m_visualStates.HasState(CommonStates2.PointerOverSelected))
				{
					if (oldCommonState2 == CommonStates2.Pressed ||
						oldCommonState2 == CommonStates2.PressedSelected)
					{
						animationCommand = new ListViewBaseItemAnimationCommand_Pressed(
							false /* pressed */,
							GetWeakRef(templateChild),
							true /*isStarting*/,
							!useTransitions);
						EnqueueAnimationCommand(animationCommand);
					}
				}
				else
				{
					animationCommand = new ListViewBaseItemAnimationCommand_Pressed(
						false /* pressed */,
						GetWeakRef(templateChild),
						false /*isStarting*/,
						!useTransitions);
					EnqueueAnimationCommand(animationCommand);
				}
			}

			if (m_backplateRectangle is not null)
			{
				global::System.Diagnostics.Debug.Assert(isRoundedListViewBaseItemChromeEnabled);
				SetBackplateBackground();
			}

			dirty = true;
		}

		if (!pWentToState &&
			UpdateVisualStateGroup(pStateName, ref m_visualStates.disabledState, out pWentToState))
		{
			var roundedGridViewItem = isRoundedListViewBaseItemChromeEnabled && IsChromeForGridViewItem();

			if (roundedGridViewItem)
			{
				var disabled =
					m_visualStates.HasState(DisabledStates.Disabled);
				var pointerOver =
					m_visualStates.HasState(CommonStates2.PointerOver);
				var selected =
					m_visualStates.HasState(CommonStates2.Selected) ||
					m_visualStates.HasState(CommonStates2.PressedSelected) ||
					m_visualStates.HasState(CommonStates2.PointerOverSelected);

				if (m_outerBorder is null)
				{
					if (selected || (!disabled && pointerOver))
					{
						EnsureOuterBorder();
					}
				}
				else
				{
					if (selected || (!disabled && pointerOver))
					{
						SetOuterBorderBrush();
					}
					else
					{
						RemoveOuterBorder();
					}
				}

				if (m_innerSelectionBorder is null)
				{
					if (selected)
					{
						EnsureInnerSelectionBorder();
					}
				}
				else
				{
					if (selected)
					{
						SetInnerSelectionBorderBrush();
					}
					else
					{
						RemoveInnerSelectionBorder();
					}
				}
			}

			if (isRoundedListViewBaseItemChromeEnabled)
			{
				if (m_backplateRectangle is not null)
				{
					SetBackplateBackground();
				}

				if (m_multiSelectCheckGlyph is not null)
				{
					SetMultiSelectCheckBoxForeground();
				}

				if (m_multiSelectCheckBoxRectangle is not null)
				{
					SetMultiSelectCheckBoxBackground();
					SetMultiSelectCheckBoxBorder();
				}

				if (m_selectionIndicatorRectangle is not null)
				{
					SetSelectionIndicatorBackground();
				}
			}

			disabledChanged = true;
			dirty = true;
		}

		if (!pWentToState &&
			UpdateVisualStateGroup(pStateName, ref m_visualStates.focusState, out pWentToState))
		{
			dirty = true;
		}

		if (!pWentToState &&
			UpdateVisualStateGroup(pStateName, ref m_visualStates.multiSelectState, out pWentToState))
		{
			var entering = m_visualStates.HasState(MultiSelectStates.MultiSelectEnabled);
			double contentTranslationX = isRoundedListViewBaseItemChromeEnabled ? s_multiSelectRoundedContentOffset : s_listViewItemMultiSelectContentOffset;
			ListViewBaseItemAnimationCommand animationCommand;

			if (entering)
			{
				if (m_isInIndicatorSelect)
				{
					contentTranslationX -= listViewItemSelectionIndicatorContentOffset;
				}
				m_isInIndicatorSelect = false;
				m_isInMultiSelect = true;
				EnsureMultiSelectCheckBox();
			}
			else if (IsInSelectionIndicatorMode())
			{
				contentTranslationX -= listViewItemSelectionIndicatorContentOffset;
			}

			animationCommand = new ListViewBaseItemAnimationCommand_MultiSelect(
				isRoundedListViewBaseItemChromeEnabled,
				entering,
				s_multiSelectSquareSize.Width /*checkBoxTranslationX*/,
				contentTranslationX,
				m_checkMode,
				GetWeakRef<UIElement>(m_multiSelectCheckBoxRectangle),
				GetWeakRef(templateChild),
				true /*isStarting*/,
				!useTransitions);
			EnqueueAnimationCommand(animationCommand);

			needsMeasure = true;
			needsArrange = true;
			dirty = true;
		}

		if (!pWentToState &&
			UpdateVisualStateGroup(pStateName, ref m_visualStates.selectionIndicatorState, out pWentToState))
		{
			var entering = m_visualStates.HasState(SelectionIndicatorStates.SelectionIndicatorEnabled);
			var enqueueAnimationCommand = !m_isInMultiSelect;

			if (entering)
			{
				m_isInIndicatorSelect = true;
				m_isInMultiSelect = false;
			}
			else if (m_visualStates.HasState(MultiSelectStates.MultiSelectDisabled))
			{
				m_isInIndicatorSelect = false;
				m_isInMultiSelect = false;
			}

			if (enqueueAnimationCommand)
			{
				ListViewBaseItemAnimationCommand animationCommand;

				animationCommand = new ListViewBaseItemAnimationCommand_IndicatorSelect(
					entering,
					listViewItemSelectionIndicatorContentOffset,
					GetSelectionIndicatorMode(),
					GetWeakRef<UIElement>(m_selectionIndicatorRectangle),
					GetWeakRef(templateChild),
					true /*isStarting*/,
					!useTransitions);
				EnqueueAnimationCommand(animationCommand);
			}

			needsMeasure = true;
			needsArrange = true;
			dirty = true;
		}

		var oldReorderHintState = m_visualStates.reorderHintState;

		if (!pWentToState &&
			UpdateVisualStateGroup(pStateName, ref m_visualStates.reorderHintState, out pWentToState))
		{
			ListViewBaseItemAnimationCommand animationCommand;

			if (m_visualStates.HasState(ReorderHintStates.NoReorderHint))
			{
				var offset = ComputeReorderHintOffset(oldReorderHintState);
				animationCommand = new ListViewBaseItemAnimationCommand_ReorderHint(
					(float)offset.X,
					(float)offset.Y,
					GetWeakRef<ListViewBaseItemPresenter>(this),
					false /*isStarting*/,
					!useTransitions);
				EnqueueAnimationCommand(animationCommand);
			}
			else
			{
				Point offset = default;

				if (oldReorderHintState != ReorderHintStates.NoReorderHint)
				{
					// Gotta clear out the old hint first.
					offset = ComputeReorderHintOffset(oldReorderHintState);
					animationCommand = new ListViewBaseItemAnimationCommand_ReorderHint(
						(float)offset.X,
						(float)offset.Y,
						GetWeakRef<ListViewBaseItemPresenter>(this),
						false /*isStarting*/,
						true /*steadyStateOnly*/);
					EnqueueAnimationCommand(animationCommand);
				}

				offset = ComputeReorderHintOffset(m_visualStates.reorderHintState);
				animationCommand = new ListViewBaseItemAnimationCommand_ReorderHint(
					(float)offset.X,
					(float)offset.Y,
					GetWeakRef<ListViewBaseItemPresenter>(this),
					true /*isStarting*/,
					!useTransitions);
				EnqueueAnimationCommand(animationCommand);
			}
			dirty = true;
		}

		var oldDragDropState = m_visualStates.dragState;

		if (!pWentToState &&
			UpdateVisualStateGroup(pStateName, ref m_visualStates.dragState, out pWentToState))
		{
			ListViewBaseItemAnimationCommand animationCommand;

			var dragCountTextBlockVisible = false;
			if (m_visualStates.HasState(DragStates.NotDragging))
			{
				animationCommand = new ListViewBaseItemAnimationCommand_DragDrop(
					ListViewBaseItemAnimationCommand_DragDrop.DragDropState.Target,
					GetWeakRef<ListViewBaseItemPresenter>(this),
					GetWeakRef<FrameworkElement>(m_pSecondaryChrome),
					false /*isStarting*/,
					!useTransitions);
				EnqueueAnimationCommand(animationCommand);
			}
			else
			{
				// Uno-specific: C++ leaves state uninitialized; every non-NotDragging state assigns it below.
				ListViewBaseItemAnimationCommand_DragDrop.DragDropState state = default;
				var pBaseAnimationTarget = GetWeakRef<ListViewBaseItemPresenter>(this);
				WeakReference<FrameworkElement> pFadeOutAnimationTarget;

				// We need to have the secondary chrome _now_, since we need it as a target!
				AddSecondaryChrome();
				pFadeOutAnimationTarget = GetWeakRef<FrameworkElement>(m_pSecondaryChrome);

				if (m_visualStates.HasState(DragStates.Dragging))
				{
					state = ListViewBaseItemAnimationCommand_DragDrop.DragDropState.SinglePrimary;
				}
				else if (m_visualStates.HasState(DragStates.MultipleDraggingPrimary))
				{
					dragCountTextBlockVisible = true;
					state = ListViewBaseItemAnimationCommand_DragDrop.DragDropState.MultiPrimary;
				}
				else if (m_visualStates.HasState(DragStates.MultipleDraggingSecondary))
				{
					state = ListViewBaseItemAnimationCommand_DragDrop.DragDropState.MultiSecondary;
					pFadeOutAnimationTarget = GetWeakRef<FrameworkElement>(this);
				}
				else if (m_visualStates.HasState(DragStates.DraggingTarget))
				{
					state = ListViewBaseItemAnimationCommand_DragDrop.DragDropState.Target;
				}
				else if (m_visualStates.HasState(DragStates.DraggedPlaceholder))
				{
					state = ListViewBaseItemAnimationCommand_DragDrop.DragDropState.DraggedPlaceholder;
					pFadeOutAnimationTarget = GetWeakRef<FrameworkElement>(this);

					if (m_visualStates.HasState(MultiSelectStates.MultiSelectEnabled))
					{
						EnsureMultiSelectCheckBox();
					}
					else if (m_multiSelectCheckBoxRectangle is not null)
					{
						// Extended selection mode, we suppress item count border for the placeholder
						RemoveMultiSelectCheckBox();
					}
				}
				else if (m_visualStates.HasState(DragStates.Reordering))
				{
					state = ListViewBaseItemAnimationCommand_DragDrop.DragDropState.ReorderingSinglePrimary;
				}
				else if (m_visualStates.HasState(DragStates.MultipleReorderingPrimary))
				{
					dragCountTextBlockVisible = true;
					state = ListViewBaseItemAnimationCommand_DragDrop.DragDropState.ReorderingMultiPrimary;
				}
				else if (m_visualStates.HasState(DragStates.ReorderingTarget))
				{
					state = ListViewBaseItemAnimationCommand_DragDrop.DragDropState.ReorderingTarget;
				}
				else if (m_visualStates.HasState(DragStates.ReorderedPlaceholder))
				{
					state = ListViewBaseItemAnimationCommand_DragDrop.DragDropState.ReorderedPlaceholder;
					pFadeOutAnimationTarget = GetWeakRef<FrameworkElement>(this);

					if (m_visualStates.HasState(MultiSelectStates.MultiSelectEnabled))
					{
						EnsureMultiSelectCheckBox();
					}
					else if (m_multiSelectCheckBoxRectangle is not null)
					{
						// Extended selection mode, we suppress item count border for the placeholder
						RemoveMultiSelectCheckBox();
					}
				}
				else if (m_visualStates.HasState(DragStates.DragOver))
				{
					state = ListViewBaseItemAnimationCommand_DragDrop.DragDropState.DragOver;
				}

				if (oldDragDropState != DragStates.NotDragging)
				{
					// Gotta clear out the old state first.
					// WinUI quirk: the stop command carries the NEW state.
					animationCommand = new ListViewBaseItemAnimationCommand_DragDrop(
						state,
						pBaseAnimationTarget,
						pFadeOutAnimationTarget,
						false /*isStarting*/,
						true /*steadyStateOnly*/);
					EnqueueAnimationCommand(animationCommand);
				}

				animationCommand = new ListViewBaseItemAnimationCommand_DragDrop(
					state,
					pBaseAnimationTarget,
					pFadeOutAnimationTarget,
					true /*isStarting*/,
					!useTransitions);

				EnqueueAnimationCommand(animationCommand);
			}

			SetDragOverlayTextBlockVisible(dragCountTextBlockVisible);
			dirty = true;
		}

		if (!pWentToState &&
			UpdateVisualStateGroup(pStateName, ref m_visualStates.dataVirtualizationState, out pWentToState))
		{
			dirty = true;
		}

		if (disabledChanged)
		{
			float opacityValue;

			if (m_visualStates.HasState(DisabledStates.Disabled))
			{
				opacityValue = GetDisabledOpacity();
			}
			else
			{
				global::System.Diagnostics.Debug.Assert(m_visualStates.HasState(DisabledStates.Enabled));
				opacityValue = 1.0f;
			}

			if (isRoundedListViewBaseItemChromeEnabled)
			{
				if (templateChild is not null)
				{
					templateChild.Opacity = opacityValue;
				}
			}
			else
			{
				var parentListViewBaseItemNoRef = GetParentListViewBaseItemNoRef();
				parentListViewBaseItemNoRef.Opacity = opacityValue;
			}
		}

		if (needsMeasure)
		{
			InvalidateMeasure();
		}

		if (needsArrange)
		{
			InvalidateArrange();
		}

		if (dirty)
		{
			InvalidateRender();
		}
	}

	// Uno-specific: xref::get_weakref, which also accepts a null target.
	private static WeakReference<T> GetWeakRef<T>(T? target) where T : class => new(target!);

	// Draws the below-content layer
	private void DrawUnderContentLayerNewStyle(
		ChromeContentRenderer pContentRenderer,
		Rect bounds
		)
	{
		var controlBorderBounds = bounds;

		if (!IsRoundedListViewBaseItemChromeEnabled())
		{
			var pParentListViewBaseItemNoRef = GetParentListViewBaseItemNoRef();

			// Control background
			if (pParentListViewBaseItemNoRef.Background is { } background)
			{
				AddRectangle(
					pContentRenderer,
					controlBorderBounds,
					background,
					this
					);
			}

			// Border background
			// Responsible for the different visual states in CommonStates2
			// For ListViewItem, we render a filled rectangle under the content
			// For GridViewItem, we render a hollow rectangle on top of the content
			if (m_checkMode == ListViewItemPresenterCheckMode.Inline)
			{
				Brush? backgroundBrush = null;

				if (m_visualStates.HasState(CommonStates2.PointerOver))
				{
					backgroundBrush = m_pPointerOverBackground;
				}
				else if (m_visualStates.HasState(CommonStates2.Pressed))
				{
					backgroundBrush = m_pPressedBackground;
				}
				else if (m_visualStates.HasState(CommonStates2.Selected))
				{
					backgroundBrush = m_pSelectedBackground;
				}
				else if (m_visualStates.HasState(CommonStates2.PointerOverSelected))
				{
					backgroundBrush = m_pSelectedPointerOverBackground;
				}
				else if (m_visualStates.HasState(CommonStates2.PressedSelected))
				{
					backgroundBrush = m_pSelectedPressedBackground;
				}

				if (backgroundBrush is not null)
				{
					if (m_previousBackgroundBrush != backgroundBrush)
					{
						var isAnimationEnabled = global::Uno.UI.Helpers.WinUI.SharedHelpers.IsAnimationsEnabled();

						if (isAnimationEnabled)
						{
							var backgroundTransition = BackgroundTransition;

							if (backgroundTransition is not null)
							{
								BorderHelper.SetUpBrushTransitionIfAllowed(
									ChromeVisual,
									m_previousBackgroundBrush /* from */,
									backgroundBrush /* to */,
									backgroundTransition,
									isAnimation: false);
							}
							else
							{
								// TODO Uno: WUCBrushManager::CleanUpBrushTransition has no Uno equivalent; a running transition completes.
							}
						}

						m_previousBackgroundBrush = backgroundBrush;
					}

					AddElementBackgroundRectangle(
						pContentRenderer,
						backgroundBrush
						);
				}
				else
				{
					m_previousBackgroundBrush = null;
				}
			}
		}

		// Draw Reveal Background brush below content
		if (GetRevealBackgroundBrushNoRef() is not null && !GetRevealBackgroundShowsAboveContent())
		{
			DrawRevealBackground(pContentRenderer, controlBorderBounds);
		}
	}

	// Draws the above-content layer
	private void DrawOverContentLayerNewStyle(
		ChromeContentRenderer pContentRenderer,
		Rect bounds
		)
	{
		Rect controlBorderBounds = default;
		Thickness controlBorderThickness = default;

		var pParentListViewBaseItemNoRef = GetParentListViewBaseItemNoRef();

		controlBorderThickness = pParentListViewBaseItemNoRef.BorderThickness;

		if (ShouldUseLayoutRounding())
		{
			LayoutRoundHelper(ref controlBorderThickness);
		}

		controlBorderBounds = bounds;

		// Placeholder
		if (m_visualStates.HasState(DataVirtualizationStates.DataPlaceholder) && m_pPlaceholderBackground is { } placeholderBackground)
		{
			AddRectangle(
				pContentRenderer,
				controlBorderBounds,
				placeholderBackground,
				this
				);
		}

		// Control border
		if (pParentListViewBaseItemNoRef.BorderBrush is { } controlBorderBrush)
		{
			AddBorder(
				pContentRenderer,
				controlBorderBounds,
				controlBorderThickness,
				controlBorderBrush,
				this
				);
		}

		// Border background
		// Responsible for the different visual states in CommonStates2
		// For ListViewItem, we render a filled rectangle under the content
		// For GridViewItem, we render a hollow rectangle (of thickness 2) on top of the content
		if (!IsRoundedListViewBaseItemChromeEnabled() && m_checkMode == ListViewItemPresenterCheckMode.Overlay)
		{
			// if item is not focused currently, we want chrome to draw border. so, clearing the boolean for m_isFocusVisualDrawnByFocusManager
			if (IsFocusVisualDrawnByFocusManager())
			{
				// if we get here, it means the item has focus and the focus manager has drawn the inner border. chrome should not draw any border.
			}
			else
			{
				// We want the chrome to draw selection/hover/press visual border only if focus manager did not draw the border
				Brush? borderBrush = null;

				if (m_visualStates.HasState(CommonStates2.PointerOver))
				{
					borderBrush = m_pPointerOverBackground;
				}
				else if (m_visualStates.HasState(CommonStates2.Pressed))
				{
					borderBrush = m_pPressedBackground;
				}
				else if (m_visualStates.HasState(CommonStates2.Selected))
				{
					borderBrush = m_pSelectedBackground;
				}
				else if (m_visualStates.HasState(CommonStates2.PointerOverSelected))
				{
					borderBrush = m_pSelectedPointerOverBackground;
				}
				else if (m_visualStates.HasState(CommonStates2.PressedSelected))
				{
					borderBrush = m_pSelectedPressedBackground;
				}

				if (borderBrush is not null)
				{
					FocusRectangleOptions focusOptions = default;

					focusOptions.drawFirst = true;
					focusOptions.firstThickness = new Thickness(s_gridViewItemFocusBorderThickness);
					// TODO Uno: FocusRectangleOptions only holds a SolidColorBrush; WinUI's static_cast still strokes with any brush.
					focusOptions.firstBrush = (borderBrush as SolidColorBrush)!;
					// TODO Uno: focusOptions.isContinuous = true; (FocusRectangleOptions has no isContinuous, the chrome only renders continuous rectangles).

					RenderFocusRectangle(
						pContentRenderer,
						focusOptions);
				}
			}
		}

		// TH2 Focus Rectangle (dotted lines) - In this case, the chrome draws focus rectangles, and overrides the FocusRectManager.
		if (!m_visualStates.HasState(DisabledStates.Disabled) && m_visualStates.HasState(FocusStates.Focused))
		{
			var shouldDrawDottedLines = ShouldDrawDottedLinesFocusVisual();
			if (shouldDrawDottedLines)
			{
				// TODO Uno: DottedLine focus visuals (C:3565-3603: FocusBorderBrush / FocusSecondaryBorderBrush dotted rectangle).
			}
		}

		{
			// disable hittest for reveal brushes which are above content
			// Uno-specific: the over-content layer visual is never hit-tested.

			// Draw Reveal Background brush over content
			if (GetRevealBackgroundBrushNoRef() is not null && GetRevealBackgroundShowsAboveContent())
			{
				DrawRevealBackground(pContentRenderer, controlBorderBounds);
			}

			// Reveal border is above everything
			var revealBorderBrush = GetRevealBorderBrushNoRef();

			if (revealBorderBrush is not null)
			{
				AddBorder(
					pContentRenderer,
					controlBorderBounds,
					GetRevealBorderThickness(),
					revealBorderBrush,
					this
				);
			}
		}
	}

	// TODO Uno: wired by FocusRectManager.CallCustomizationFunction (the focus visual pipeline does not call it yet).
	internal void CustomizeFocusRectangle(
		ref FocusRectangleOptions options,
		out bool shouldDrawFocusRect)
	{
		// Callback from CFocusRectManager at beginning of render walk.  It's an opportunity to tell
		// CFocusRectManager how to render the focus rectangle, and whether or not it should draw it
		// at all.
		var shouldDrawDottedLines = ShouldDrawDottedLinesFocusVisual();
		if (shouldDrawDottedLines)
		{
			shouldDrawFocusRect = false;
		}
		else
		{
			if (m_checkMode == ListViewItemPresenterCheckMode.Overlay)
			{
				if (m_visualStates.HasState(CommonStates2.PointerOver))
				{
					options.secondThickness = new Thickness(s_gridViewItemFocusBorderThickness);
					options.secondBrush = (m_pPointerOverBackground as SolidColorBrush)!;
				}
				else if (m_visualStates.HasState(CommonStates2.Pressed))
				{
					options.secondThickness = new Thickness(s_gridViewItemFocusBorderThickness);
					options.secondBrush = (m_pPressedBackground as SolidColorBrush)!;
				}
				else if (m_visualStates.HasState(CommonStates2.Selected))
				{
					options.secondThickness = new Thickness(s_gridViewItemFocusBorderThickness);
					options.secondBrush = (m_pSelectedBackground as SolidColorBrush)!;
				}
				else if (m_visualStates.HasState(CommonStates2.PointerOverSelected))
				{
					options.secondThickness = new Thickness(s_gridViewItemFocusBorderThickness);
					options.secondBrush = (m_pSelectedPointerOverBackground as SolidColorBrush)!;
				}
				else if (m_visualStates.HasState(CommonStates2.PressedSelected))
				{
					options.secondThickness = new Thickness(s_gridViewItemFocusBorderThickness);
					options.secondBrush = (m_pSelectedPressedBackground as SolidColorBrush)!;
				}
			}

			shouldDrawFocusRect = true;
			// Uno-specific: m_isFocusVisualDrawnByFocusManager = true is derived by IsFocusVisualDrawnByFocusManager() (A33).
		}
	}

	private bool ShouldDrawDottedLinesFocusVisual()
	{
		var shouldDrawDottedLines = false;

		var parentListViewBaseItem = GetParentListViewBaseItemNoRef();

		var useSystemFocusVisuals = parentListViewBaseItem.UseSystemFocusVisuals;

		var focusVisualKind = Application.Current?.FocusVisualKind ?? FocusVisualKind.HighVisibility;

		if (useSystemFocusVisuals && focusVisualKind == FocusVisualKind.DottedLine)
		{
			shouldDrawDottedLines = true;
		}

		return shouldDrawDottedLines;
	}

	// Removes the multi-select checkbox from the tree.
	internal void RemoveMultiSelectCheckBox()
	{
		global::System.Diagnostics.Debug.Assert(m_multiSelectCheckBoxRectangle is not null);

		RemoveChild(m_multiSelectCheckBoxRectangle!);

		m_multiSelectCheckGlyph = null;
		m_multiSelectCheckBoxRectangle = null;
		m_multiSelectCheckBoxClip = null;
	}

	// Removes the selection indicator from the tree.
	internal void RemoveSelectionIndicator()
	{
		global::System.Diagnostics.Debug.Assert(m_selectionIndicatorRectangle is not null);

		RemoveChild(m_selectionIndicatorRectangle!);

		m_selectionIndicatorRectangle = null;
	}

	// Removes the inner selection border from the tree.
	internal void RemoveInnerSelectionBorder()
	{
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
		global::System.Diagnostics.Debug.Assert(m_innerSelectionBorder is not null);

		// WinUI quirk: the DisabledStates branch calls RemoveOuterBorder first, so a nested inner border is then
		// "removed" from this presenter (a no-op) and stays inside the detached outer border.
		var areBordersNested = m_outerBorder == m_innerSelectionBorder!.GetParent();

		if (areBordersNested)
		{
			m_outerBorder!.Child = null;
		}
		else
		{
			RemoveChild(m_innerSelectionBorder);
		}

		m_innerSelectionBorder = null;
	}

	// Removes the outer border from the tree.
	internal void RemoveOuterBorder()
	{
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
		global::System.Diagnostics.Debug.Assert(m_outerBorder is not null);

		RemoveChild(m_outerBorder!);

		m_outerBorder = null;

		if (m_backplateRectangle is not null)
		{
			SetBackplateMargin();
		}
	}

	// Sets up and adds the multi-select checkbox to the tree.
	internal void EnsureMultiSelectCheckBox()
	{
		var selected =
			m_visualStates.HasState(CommonStates2.Selected) ||
			m_visualStates.HasState(CommonStates2.PointerOverSelected) ||
			m_visualStates.HasState(CommonStates2.PressedSelected);

		if (m_multiSelectCheckBoxRectangle is null)
		{
			var multiSelectSquareSize = s_multiSelectSquareSize;

			if (ShouldUseLayoutRounding())
			{
				LayoutRoundHelper(ref multiSelectSquareSize);
			}

			m_multiSelectCheckBoxRectangle = new Border();

			m_multiSelectCheckBoxRectangle.IsHitTestVisible = false;

			m_multiSelectCheckBoxRectangle.MinWidth = multiSelectSquareSize.Width;

			m_multiSelectCheckBoxRectangle.Height = multiSelectSquareSize.Height;

			m_multiSelectCheckBoxRectangle.TransitionTarget = new TransitionTarget();

			AddChild(m_multiSelectCheckBoxRectangle);
		}

		// create checkmark glyph
		if (m_multiSelectCheckGlyph is null)
		{
			var strCheckMarkGlyph = c_strCheckMarkGlyphStorage;

			m_multiSelectCheckGlyph = new FontIcon();

			m_multiSelectCheckGlyph.IsHitTestVisible = false;

			m_multiSelectCheckGlyph.Opacity = selected ? 1.0 : 0.0;

			m_multiSelectCheckGlyph.FontSize = s_checkMarkGlyphFontSize;

			// Setting the glyph for the check mark
			m_multiSelectCheckGlyph.Glyph = strCheckMarkGlyph;
		}

		// add the glyph to the check box children
		m_multiSelectCheckBoxRectangle.Child = m_multiSelectCheckGlyph;

		SetMultiSelectCheckBoxProperties();
	}

	// Sets up and adds the selection indicator to the tree.
	internal void EnsureSelectionIndicator()
	{
		global::System.Diagnostics.Debug.Assert(IsInSelectionIndicatorMode());

		if (m_selectionIndicatorRectangle is null)
		{
			var selectionIndicatorSize = s_selectionIndicatorSize;
			var zeroThickness = default(Thickness);
			var selectionIndicatorMargin = new Thickness(s_selectionIndicatorMargin.Left, 0.0, s_selectionIndicatorMargin.Right, 0.0);

			if (ShouldUseLayoutRounding())
			{
				LayoutRoundHelper(ref selectionIndicatorSize);
				LayoutRoundHelper(ref selectionIndicatorMargin);
			}

			m_selectionIndicatorRectangle = new Border();

			m_selectionIndicatorRectangle.IsHitTestVisible = false;

			m_selectionIndicatorRectangle.Margin = selectionIndicatorMargin;

			m_selectionIndicatorRectangle.Width = selectionIndicatorSize.Width;

			m_selectionIndicatorRectangle.TransitionTarget = new TransitionTarget();

			m_selectionIndicatorRectangle.BorderBrush = null;

			m_selectionIndicatorRectangle.BorderThickness = zeroThickness;

			m_selectionIndicatorRectangle.VerticalAlignment = VerticalAlignment.Stretch;

			m_selectionIndicatorRectangle.HorizontalAlignment = HorizontalAlignment.Left;

			AddChild(m_selectionIndicatorRectangle);
		}

		SetSelectionIndicatorBackground();
		SetSelectionIndicatorCornerRadius();
	}

	// Sets up and adds the backplate to the tree.
	internal void EnsureBackplate()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());

		if (m_backplateRectangle is null)
		{
			var zeroThickness = default(Thickness);

			m_backplateRectangle = new Border();

			m_backplateRectangle.IsHitTestVisible = false;

			m_backplateRectangle.BorderBrush = null;

			if (IsChromeForListViewItem())
			{
				var backplateMargin = s_backplateMargin;

				if (ShouldUseLayoutRounding())
				{
					LayoutRoundHelper(ref backplateMargin);
				}

				m_backplateRectangle.Margin = backplateMargin;
			}

			m_backplateRectangle.BorderThickness = zeroThickness;

			m_backplateRectangle.VerticalAlignment = VerticalAlignment.Stretch;

			m_backplateRectangle.HorizontalAlignment = HorizontalAlignment.Stretch;

			// Inserting the backplate into first position so it is rendered underneath the content
			AddChild(m_backplateRectangle, 0);
		}

		SetBackplateCornerRadius();
		SetBackplateBackground();

		if (IsChromeForGridViewItem())
		{
			SetBackplateMargin();
		}
	}

	// Sets up and adds the inner selection border to the tree.
	internal void EnsureInnerSelectionBorder()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());

		if (m_innerSelectionBorder is null)
		{
			m_innerSelectionBorder = new Border();

			m_innerSelectionBorder.IsHitTestVisible = false;

			m_innerSelectionBorder.Background = null;

			m_innerSelectionBorder.VerticalAlignment = VerticalAlignment.Stretch;

			m_innerSelectionBorder.HorizontalAlignment = HorizontalAlignment.Stretch;

			if (IsOuterBorderBrushOpaque())
			{
				// When the outer border is opaque, it hosts the inner border to avoid any bleed through at the edges.
				m_outerBorder!.Child = m_innerSelectionBorder;
			}
			else
			{
				// Appending the border into last position so it is rendered over the content
				AddChild(m_innerSelectionBorder);
			}
		}

		SetInnerSelectionBorderProperties();
	}

	// Sets up and adds the outer border to the tree.
	internal void EnsureOuterBorder()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());

		if (m_outerBorder is null)
		{
			m_outerBorder = new Border();

			m_outerBorder.IsHitTestVisible = false;
			m_outerBorder.Background = null;

			m_outerBorder.VerticalAlignment = VerticalAlignment.Stretch;

			m_outerBorder.HorizontalAlignment = HorizontalAlignment.Stretch;

			// Appending the border into last position so it is rendered over the content
			AddChild(m_outerBorder);
		}

		SetOuterBorderProperties();
	}

	// Sets up the multi-select checkbox to the tree. (colors, alignment, clip)
	internal void SetMultiSelectCheckBoxProperties()
	{
		var isRoundedListViewBaseItemChromeEnabled = IsRoundedListViewBaseItemChromeEnabled();

		var multiSelectSquareMargin = default(Thickness);

		if (m_checkMode == ListViewItemPresenterCheckMode.Inline)
		{
			// ListViewItemBase case
			if (m_multiSelectCheckBoxClip is null)
			{
				var multiSelectSquareBounds = new Rect(0.0, 0.0, s_multiSelectSquareSize.Width, s_multiSelectSquareSize.Height);

				m_multiSelectCheckBoxClip = new RectangleGeometry();

				m_multiSelectCheckBoxClip.Rect = multiSelectSquareBounds;
			}

			multiSelectSquareMargin = isRoundedListViewBaseItemChromeEnabled ? s_multiSelectRoundedSquareInlineMargin : s_multiSelectSquareInlineMargin;

			m_multiSelectCheckBoxRectangle!.VerticalAlignment = VerticalAlignment.Center;

			m_multiSelectCheckBoxRectangle.HorizontalAlignment = HorizontalAlignment.Left;

			m_multiSelectCheckBoxRectangle.Clip = m_multiSelectCheckBoxClip;
		}
		else
		{
			global::System.Diagnostics.Debug.Assert(m_checkMode == ListViewItemPresenterCheckMode.Overlay);

			// GridViewItemBase case
			if (isRoundedListViewBaseItemChromeEnabled)
			{
				var selectedBorderThickness = GetSelectedBorderThickness();

				multiSelectSquareMargin.Top = s_innerSelectionBorderThickness.Top + selectedBorderThickness.Top + 1.0;
				multiSelectSquareMargin.Right = s_innerSelectionBorderThickness.Right + selectedBorderThickness.Right + 1.0;
			}
			else
			{
				multiSelectSquareMargin = s_multiSelectSquareOverlayMargin;
			}

			m_multiSelectCheckBoxRectangle!.VerticalAlignment = VerticalAlignment.Top;

			m_multiSelectCheckBoxRectangle.HorizontalAlignment = HorizontalAlignment.Right;
		}

		if (isRoundedListViewBaseItemChromeEnabled)
		{
			var cornerRadius = GetCheckBoxCornerRadius();

			m_multiSelectCheckBoxRectangle.CornerRadius = cornerRadius;
		}

		if (ShouldUseLayoutRounding())
		{
			LayoutRoundHelper(ref multiSelectSquareMargin);
		}
		m_multiSelectCheckBoxRectangle.Margin = multiSelectSquareMargin;

		// set the check box background brush
		SetMultiSelectCheckBoxBackground();

		// set the check box border brush and thickness
		SetMultiSelectCheckBoxBorder();

		// set the glyph's foreground brush
		SetMultiSelectCheckBoxForeground();

		if (!isRoundedListViewBaseItemChromeEnabled)
		{
			SetForegroundBrush();
		}
	}

	// Sets up the inner selection border visual variable properties.
	internal void SetInnerSelectionBorderProperties()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
		global::System.Diagnostics.Debug.Assert(m_innerSelectionBorder is not null);

		SetInnerSelectionBorderBrush();
		SetInnerSelectionBorderCornerRadius();
		SetInnerSelectionBorderThickness();
	}

	// Sets up the outer border visual variable properties.
	internal void SetOuterBorderProperties()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
		global::System.Diagnostics.Debug.Assert(m_outerBorder is not null);

		SetOuterBorderBrush();
		SetOuterBorderCornerRadius();
		SetOuterBorderThickness();
	}

	// Sets the backplate Background brush based on current visual state.
	internal void SetBackplateBackground()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(m_backplateRectangle is not null);

		var selected =
			m_visualStates.HasState(CommonStates2.Selected) ||
			m_visualStates.HasState(CommonStates2.PointerOverSelected) ||
			m_visualStates.HasState(CommonStates2.PressedSelected);
		var pointerOver =
			m_visualStates.HasState(CommonStates2.PointerOver) ||
			m_visualStates.HasState(CommonStates2.PointerOverSelected);
		var pressed =
			m_visualStates.HasState(CommonStates2.Pressed) ||
			m_visualStates.HasState(CommonStates2.PressedSelected);

		Brush? backplateRectangleBackground = null;

		if (m_visualStates.HasState(DisabledStates.Disabled))
		{
			if (selected)
			{
				backplateRectangleBackground = m_pSelectedDisabledBackground;
			}
		}
		else if (pressed)
		{
			if (selected)
			{
				backplateRectangleBackground = m_pSelectedPressedBackground;
			}
			else
			{
				backplateRectangleBackground = m_pPressedBackground;
			}
		}
		else if (pointerOver)
		{
			if (selected)
			{
				backplateRectangleBackground = m_pSelectedPointerOverBackground;
			}
			else
			{
				backplateRectangleBackground = m_pPointerOverBackground;
			}
		}
		else if (selected)
		{
			backplateRectangleBackground = m_pSelectedBackground;
		}
		else
		{
			var parentListViewBaseItemNoRef = GetParentListViewBaseItemNoRef();

			if (parentListViewBaseItemNoRef is not null)
			{
				// WinUI quirk: item Background changes only invalidate render (LBI:1984-2010), so this stays stale until the next state change.
				backplateRectangleBackground = parentListViewBaseItemNoRef.Background;
			}
		}

		m_backplateRectangle!.Background = backplateRectangleBackground;
	}

	// Sets up the backplate CornerRadius.
	internal void SetBackplateCornerRadius()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(m_backplateRectangle is not null);

		SetGeneralCornerRadius(m_backplateRectangle!);
	}

	// Sets up the backplate Margin for GridViewItem.
	internal void SetBackplateMargin()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
		global::System.Diagnostics.Debug.Assert(m_backplateRectangle is not null);

		// When the outer border is present and opaque, the backplate gets a 1px margin to avoid any bleed through at the edges.
		var zeroThickness = default(Thickness);
		var oneThickness = new Thickness(1.0, 1.0, 1.0, 1.0);
		var backplateMargin = IsOuterBorderBrushOpaque() ? oneThickness : zeroThickness;

		if (ShouldUseLayoutRounding())
		{
			LayoutRoundHelper(ref backplateMargin);
		}

		m_backplateRectangle!.Margin = backplateMargin;
	}

	// Sets the inner selection border's BorderBrush based on current SelectionBorderBrush property.
	internal void SetInnerSelectionBorderBrush()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
		global::System.Diagnostics.Debug.Assert(m_innerSelectionBorder is not null);

#if DEBUG
		var selected =
			m_visualStates.HasState(CommonStates2.Selected) ||
			m_visualStates.HasState(CommonStates2.PointerOverSelected) ||
			m_visualStates.HasState(CommonStates2.PressedSelected);
		global::System.Diagnostics.Debug.Assert(selected);
#endif

		m_innerSelectionBorder!.BorderBrush = m_pSelectedInnerBorderBrush;
	}

	// Sets the inner selection border's CornerRadius.
	internal void SetInnerSelectionBorderCornerRadius()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
		global::System.Diagnostics.Debug.Assert(m_innerSelectionBorder is not null);

		var cornerRadius = GetGeneralCornerRadius();
		var areBordersNested = m_outerBorder == m_innerSelectionBorder!.GetParent();

		if (areBordersNested)
		{
			var selectedBorderThickness = GetSelectedBorderThickness();

			// Decrease inner border corner radius to account for outer border thickness.
			cornerRadius.BottomLeft = Math.Max(s_innerBorderCornerRadius, cornerRadius.BottomLeft - selectedBorderThickness.Left);
			cornerRadius.BottomRight = Math.Max(s_innerBorderCornerRadius, cornerRadius.BottomRight - selectedBorderThickness.Right);
			cornerRadius.TopLeft = Math.Max(s_innerBorderCornerRadius, cornerRadius.TopLeft - selectedBorderThickness.Left);
			cornerRadius.TopRight = Math.Max(s_innerBorderCornerRadius, cornerRadius.TopRight - selectedBorderThickness.Right);
		}

		m_innerSelectionBorder.CornerRadius = cornerRadius;
	}

	// Sets the outer selection border's BorderBrush based on current visual state.
	internal void SetOuterBorderBrush()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
		global::System.Diagnostics.Debug.Assert(m_outerBorder is not null);

		var selected =
			m_visualStates.HasState(CommonStates2.Selected) ||
			m_visualStates.HasState(CommonStates2.PointerOverSelected) ||
			m_visualStates.HasState(CommonStates2.PressedSelected);

		global::System.Diagnostics.Debug.Assert(selected || (!m_visualStates.HasState(DisabledStates.Disabled) && m_visualStates.HasState(CommonStates2.PointerOver)));

		Brush? borderBrush = null;

		if (selected)
		{
			if (m_visualStates.HasState(DisabledStates.Disabled))
			{
				borderBrush = m_pSelectedDisabledBorderBrush;
			}
			else if (m_visualStates.HasState(CommonStates2.PressedSelected))
			{
				borderBrush = m_pSelectedPressedBorderBrush;
			}
			else if (m_visualStates.HasState(CommonStates2.PointerOverSelected))
			{
				borderBrush = m_pSelectedPointerOverBorderBrush;
			}
			else
			{
				borderBrush = m_pSelectedBorderBrush;
			}
		}
		else
		{
			borderBrush = m_pPointerOverBorderBrush;
		}

		m_outerBorder!.BorderBrush = borderBrush;

		UpdateBordersParenting();

		if (m_backplateRectangle is not null)
		{
			SetBackplateMargin();
		}
	}

	// Sets the outer border's CornerRadius.
	internal void SetOuterBorderCornerRadius()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(m_outerBorder is not null);

		SetGeneralCornerRadius(m_outerBorder!);
	}

	// Sets the inner selection border's BorderThickness based on current SelectedBorderThickness property.
	internal void SetInnerSelectionBorderThickness()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
		global::System.Diagnostics.Debug.Assert(m_innerSelectionBorder is not null);

#if DEBUG
		var selected =
			m_visualStates.HasState(CommonStates2.Selected) ||
			m_visualStates.HasState(CommonStates2.PointerOverSelected) ||
			m_visualStates.HasState(CommonStates2.PressedSelected);
		global::System.Diagnostics.Debug.Assert(selected);
#endif

		var areBordersNested = m_outerBorder == m_innerSelectionBorder!.GetParent();
		var innerSelectionBorderThickness = s_innerSelectionBorderThickness;

		if (areBordersNested)
		{
			// When the outer border is opaque, it hosts the inner border which is expanded all around by a pixel
			// to avoid any bleed through at the edges and in-between the borders.
			innerSelectionBorderThickness.Left += 1.0;
			innerSelectionBorderThickness.Top += 1.0;
			innerSelectionBorderThickness.Right += 1.0;
			innerSelectionBorderThickness.Bottom += 1.0;

			var innerSelectionBorderMargin = new Thickness(-1.0, -1.0, -1.0, -1.0);

			if (ShouldUseLayoutRounding())
			{
				LayoutRoundHelper(ref innerSelectionBorderMargin);
			}

			m_innerSelectionBorder.Margin = innerSelectionBorderMargin;
		}
		else
		{
			var selectedBorderThickness = GetSelectedBorderThickness();

			innerSelectionBorderThickness.Left += selectedBorderThickness.Left;
			innerSelectionBorderThickness.Top += selectedBorderThickness.Top;
			innerSelectionBorderThickness.Right += selectedBorderThickness.Right;
			innerSelectionBorderThickness.Bottom += selectedBorderThickness.Bottom;
		}

		if (ShouldUseLayoutRounding())
		{
			LayoutRoundHelper(ref innerSelectionBorderThickness);
		}

		m_innerSelectionBorder.BorderThickness = innerSelectionBorderThickness;
	}

	// Sets the outer selection border's BorderThickness based on current visual state.
	internal void SetOuterBorderThickness()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
		global::System.Diagnostics.Debug.Assert(m_outerBorder is not null);

		var selected =
			m_visualStates.HasState(CommonStates2.Selected) ||
			m_visualStates.HasState(CommonStates2.PointerOverSelected) ||
			m_visualStates.HasState(CommonStates2.PressedSelected);

		global::System.Diagnostics.Debug.Assert(selected || (!m_visualStates.HasState(DisabledStates.Disabled) && m_visualStates.HasState(CommonStates2.PointerOver)));

		var outerBorderThickness = selected ? GetSelectedBorderThickness() : s_borderThickness;

		if (ShouldUseLayoutRounding())
		{
			LayoutRoundHelper(ref outerBorderThickness);
		}

		m_outerBorder!.BorderThickness = outerBorderThickness;
	}

	// Sets the multi-select checkbox background brush.
	internal void SetMultiSelectCheckBoxBackground()
	{
		global::System.Diagnostics.Debug.Assert(m_multiSelectCheckBoxRectangle is not null);
		global::System.Diagnostics.Debug.Assert(m_checkMode == ListViewItemPresenterCheckMode.Inline || m_checkMode == ListViewItemPresenterCheckMode.Overlay);

		var selected =
			m_visualStates.HasState(CommonStates2.Selected) ||
			m_visualStates.HasState(CommonStates2.PointerOverSelected) ||
			m_visualStates.HasState(CommonStates2.PressedSelected);

		Brush? checkBoxBrush = null;

		if (IsRoundedListViewBaseItemChromeEnabled())
		{
			if (m_visualStates.HasState(DisabledStates.Disabled))
			{
				checkBoxBrush = selected ? m_pCheckBoxSelectedDisabledBrush : m_pCheckBoxDisabledBrush;
			}
			else if (m_visualStates.HasState(CommonStates2.PressedSelected))
			{
				checkBoxBrush = m_pCheckBoxSelectedPressedBrush;
			}
			else if (m_visualStates.HasState(CommonStates2.Pressed))
			{
				checkBoxBrush = m_pCheckBoxPressedBrush;
			}
			else if (m_visualStates.HasState(CommonStates2.PointerOverSelected))
			{
				checkBoxBrush = m_pCheckBoxSelectedPointerOverBrush;
			}
			else if (m_visualStates.HasState(CommonStates2.PointerOver))
			{
				checkBoxBrush = m_pCheckBoxPointerOverBrush;
			}
			else if (m_visualStates.HasState(CommonStates2.Selected))
			{
				checkBoxBrush = m_pCheckBoxSelectedBrush;
			}
			else
			{
				checkBoxBrush = m_pCheckBoxBrush;
			}
		}
		else if (m_checkMode == ListViewItemPresenterCheckMode.Overlay)
		{
			checkBoxBrush = selected ? m_pSelectedBackground : m_pCheckBoxBrush;
		}

		m_multiSelectCheckBoxRectangle!.Background = checkBoxBrush;
	}

	// Sets the multi-select checkbox border brush and thickness.
	internal void SetMultiSelectCheckBoxBorder()
	{
		global::System.Diagnostics.Debug.Assert(m_multiSelectCheckBoxRectangle is not null);
		global::System.Diagnostics.Debug.Assert(m_checkMode == ListViewItemPresenterCheckMode.Inline || m_checkMode == ListViewItemPresenterCheckMode.Overlay);

		var isRoundedListViewBaseItemChromeEnabled = IsRoundedListViewBaseItemChromeEnabled();
		var zeroThickness = default(Thickness);
		var multiSelectSquareThickness = s_multiSelectRoundedSquareThickness;
		Thickness value;
		Brush? checkBoxBorderBrush = null;

		if (isRoundedListViewBaseItemChromeEnabled)
		{
			var selected =
				m_visualStates.HasState(CommonStates2.Selected) ||
				m_visualStates.HasState(CommonStates2.PointerOverSelected) ||
				m_visualStates.HasState(CommonStates2.PressedSelected);

			if (selected)
			{
				value = zeroThickness;
			}
			else
			{
				if (ShouldUseLayoutRounding())
				{
					LayoutRoundHelper(ref multiSelectSquareThickness);
				}

				value = multiSelectSquareThickness;

				if (m_visualStates.HasState(DisabledStates.Disabled))
				{
					checkBoxBorderBrush = m_pCheckBoxDisabledBorderBrush;
				}
				else if (m_visualStates.HasState(CommonStates2.Pressed))
				{
					checkBoxBorderBrush = m_pCheckBoxPressedBorderBrush;
				}
				else if (m_visualStates.HasState(CommonStates2.PointerOver))
				{
					checkBoxBorderBrush = m_pCheckBoxPointerOverBorderBrush;
				}
				else
				{
					checkBoxBorderBrush = m_pCheckBoxBorderBrush;
				}
			}
		}
		else
		{
			if (m_checkMode == ListViewItemPresenterCheckMode.Inline)
			{
				// ListViewItemBase case
				value = s_multiSelectSquareThickness;

				checkBoxBorderBrush = m_pCheckBoxBrush;
			}
			else
			{
				// GridViewItemBase case, m_checkMode == ListViewItemPresenterCheckMode.Overlay
				value = zeroThickness;
			}
		}

		m_multiSelectCheckBoxRectangle!.BorderThickness = value;
		m_multiSelectCheckBoxRectangle.BorderBrush = checkBoxBorderBrush;
	}

	// Sets the multi-select checkbox glyph's foreground brush.
	internal void SetMultiSelectCheckBoxForeground()
	{
		global::System.Diagnostics.Debug.Assert(m_multiSelectCheckGlyph is not null);

		// set the Glyph's brush to m_pCheckBrush, m_pCheckPressedBrush or m_pCheckDisabledBrush
		var checkBrush = m_pCheckBrush;

		if (IsRoundedListViewBaseItemChromeEnabled())
		{
			if (m_visualStates.HasState(DisabledStates.Disabled))
			{
				checkBrush = m_pCheckDisabledBrush;
			}
			else if (m_visualStates.HasState(CommonStates2.PressedSelected))
			{
				checkBrush = m_pCheckPressedBrush;
			}
		}

		m_multiSelectCheckGlyph!.Foreground = checkBrush!;
	}

	// Sets the selection indicator background brush.
	internal void SetSelectionIndicatorBackground()
	{
		global::System.Diagnostics.Debug.Assert(IsSelectionIndicatorVisualEnabled());
		global::System.Diagnostics.Debug.Assert(m_selectionIndicatorRectangle is not null);

		var disabled = m_visualStates.HasState(DisabledStates.Disabled);
		var pressed = m_visualStates.HasState(CommonStates2.PressedSelected);
		var pointerOver = m_visualStates.HasState(CommonStates2.PointerOverSelected);

		global::System.Diagnostics.Debug.Assert(m_visualStates.HasState(CommonStates2.Selected) || pointerOver || pressed);

		Brush? selectionIndicatorRectangleBackground = null;

		if (disabled)
		{
			selectionIndicatorRectangleBackground = m_pSelectionIndicatorDisabledBrush;
		}
		else if (pressed)
		{
			selectionIndicatorRectangleBackground = m_pSelectionIndicatorPressedBrush;
		}
		else if (pointerOver)
		{
			selectionIndicatorRectangleBackground = m_pSelectionIndicatorPointerOverBrush;
		}
		else
		{
			selectionIndicatorRectangleBackground = m_pSelectionIndicatorBrush;
		}

		m_selectionIndicatorRectangle!.Background = selectionIndicatorRectangleBackground;
	}

	// Sets the selection indicator corner radius.
	internal void SetSelectionIndicatorCornerRadius()
	{
		global::System.Diagnostics.Debug.Assert(IsSelectionIndicatorVisualEnabled());
		global::System.Diagnostics.Debug.Assert(m_selectionIndicatorRectangle is not null);

		var cornerRadius = GetSelectionIndicatorCornerRadius();

		m_selectionIndicatorRectangle!.CornerRadius = cornerRadius;
	}

	// Sets / clears content's foreground brush in Common visual states.
	// This function is only called for new styles
	internal void SetForegroundBrush()
	{
		// Uno-specific: stands in for the CValue's IsUnset() state.
		var isForegroundValueSet = false;
		Brush? foregroundValue = null;

		if (m_visualStates.HasState(CommonStates2.Selected) ||
			m_visualStates.HasState(CommonStates2.PressedSelected) ||
			m_visualStates.HasState(CommonStates2.PointerOverSelected))
		{
			if (m_pSelectedForeground is not null)
			{
				foregroundValue = m_pSelectedForeground;
				isForegroundValueSet = true;
			}
		}
		else if (m_visualStates.HasState(CommonStates2.PointerOver) ||
			m_visualStates.HasState(CommonStates2.Pressed))
		{
			// PointerOverForeground is a Threshold property added to ListViewItemPresenter
			// GridViewItemPresenter does not have the PointerOverForeground property
			// Calling IsPropertyDefault on a property that does not exist will cause a crash
			if (this is not GridViewItemPresenter)
			{
				// We want to explicitly set the brush in 1 of 2 cases
				// 1- Brush not NULL meaning it is set to some color
				// 2- Developer explicitly sets it to NULL (By default, if the developer does not set the property, it is NULL)
				if (m_pPointerOverForeground is not null || GetCurrentHighestValuePrecedence(ListViewItemPresenter.PointerOverForegroundProperty) != DependencyPropertyValuePrecedences.DefaultValue)
				{
					foregroundValue = m_pPointerOverForeground;
					isForegroundValueSet = true;
				}
			}
		}

		// if the value is not set, we clear the Brush value
		if (!isForegroundValueSet)
		{
			ClearValue(ForegroundProperty, DependencyPropertyValuePrecedences.Animations);

			if (!IsRoundedListViewBaseItemChromeEnabled() &&
				m_checkMode == ListViewItemPresenterCheckMode.Inline &&
				m_multiSelectCheckBoxRectangle is not null)
			{
				// set the CheckBox's brush
				m_multiSelectCheckBoxRectangle.BorderBrush = m_pCheckBoxBrush;

				// set the CheckMark Glyph's brush
				m_multiSelectCheckGlyph!.Foreground = m_pCheckBrush!;
			}
		}
		else
		{
			SetValue(ForegroundProperty, foregroundValue, DependencyPropertyValuePrecedences.Animations);

			// in the case of Selection or PointerOver, we want the CheckBox and the glyph to have the same color as the item's Foreground
			if (!IsRoundedListViewBaseItemChromeEnabled() &&
				m_checkMode == ListViewItemPresenterCheckMode.Inline &&
				m_multiSelectCheckBoxRectangle is not null)
			{
				// set the CheckBox's brush
				m_multiSelectCheckBoxRectangle.BorderBrush = foregroundValue;

				// set the CheckMark Glyph's brush
				m_multiSelectCheckGlyph!.Foreground = foregroundValue!;
			}
		}
	}

	// Uno-specific: GetValue / SetValue (C:4918-4996) forward the deprecated alias DPs, see ListViewBaseItemPresenter.ForwardAlias.

	// Handles the property changed for the new ListViewBaseItem style for Threshold
	// WinUI quirk: the cases are mostly LVIP ids, so most GVIP brush changes don't refresh the chrome children.
	private protected void OnPropertyChangedNewStyle(DependencyPropertyChangedEventArgs args)
	{
		var property = args.Property;

		// Brushes used for both Threshold and Blue
		if (property == ListViewItemPresenter.SelectedForegroundProperty ||
			property == GridViewItemPresenter.SelectedForegroundProperty ||
			property == ListViewItemPresenter.PointerOverForegroundProperty)
		{
			SetForegroundBrush();
		}
		else if (property == ListViewItemPresenter.SelectionIndicatorVisualEnabledProperty ||
			property == ListViewItemPresenter.SelectionCheckMarkVisualEnabledProperty ||
			property == GridViewItemPresenter.SelectionCheckMarkVisualEnabledProperty ||
			property == ListViewItemPresenter.CheckHintBrushProperty ||
			property == GridViewItemPresenter.CheckHintBrushProperty ||
			property == ListViewItemPresenter.CheckSelectingBrushProperty ||
			property == GridViewItemPresenter.CheckSelectingBrushProperty ||
			property == ListViewItemPresenter.SelectionIndicatorModeProperty)
		{
			InvalidateRender();
		}
		else if (property == ListViewItemPresenter.CheckModeProperty ||
			property == ListViewItemPresenter.CheckBrushProperty ||
			property == ListViewItemPresenter.CheckPressedBrushProperty ||
			property == ListViewItemPresenter.CheckDisabledBrushProperty ||
			property == ListViewItemPresenter.CheckBoxBrushProperty ||
			property == ListViewItemPresenter.CheckBoxBorderBrushProperty ||
			property == ListViewItemPresenter.CheckBoxPressedBorderBrushProperty ||
			property == ListViewItemPresenter.CheckBoxDisabledBorderBrushProperty ||
			property == ListViewItemPresenter.CheckBoxCornerRadiusProperty ||
			property == ListViewItemPresenter.SelectedBackgroundProperty ||
			property == ListViewItemPresenter.SelectedPointerOverBackgroundProperty ||
			property == ListViewItemPresenter.SelectedPressedBackgroundProperty ||
			property == ListViewItemPresenter.SelectedDisabledBackgroundProperty)
		{
			// WinUI quirk: CheckBoxPointerOverBorderBrush has no case, so changing it does not refresh the check box.
			// only update changes if the checkbox already exists
			if (m_multiSelectCheckBoxRectangle is not null)
			{
				SetMultiSelectCheckBoxProperties();
				InvalidateRender();
			}

			if (m_backplateRectangle is not null &&
				(property == ListViewItemPresenter.SelectedBackgroundProperty ||
				 property == ListViewItemPresenter.SelectedPointerOverBackgroundProperty ||
				 property == ListViewItemPresenter.SelectedPressedBackgroundProperty ||
				 property == ListViewItemPresenter.SelectedDisabledBackgroundProperty))
			{
				SetBackplateBackground();
				InvalidateRender();
			}
		}
		else if (property == ListViewItemPresenter.SelectedInnerBorderBrushProperty)
		{
			if (m_innerSelectionBorder is not null)
			{
				SetInnerSelectionBorderBrush();
				InvalidateRender();
			}
		}
		else if (property == ListViewItemPresenter.CheckBoxPointerOverBrushProperty ||
			property == ListViewItemPresenter.CheckBoxPressedBrushProperty ||
			property == ListViewItemPresenter.CheckBoxDisabledBrushProperty ||
			property == ListViewItemPresenter.CheckBoxSelectedBrushProperty ||
			property == ListViewItemPresenter.CheckBoxSelectedPointerOverBrushProperty ||
			property == ListViewItemPresenter.CheckBoxSelectedPressedBrushProperty ||
			property == ListViewItemPresenter.CheckBoxSelectedDisabledBrushProperty)
		{
			if (m_multiSelectCheckBoxRectangle is not null)
			{
				SetMultiSelectCheckBoxBackground();
				InvalidateRender();
			}
		}
		else if (property == ListViewItemPresenter.PointerOverBorderBrushProperty ||
			property == ListViewItemPresenter.SelectedBorderBrushProperty ||
			property == ListViewItemPresenter.SelectedPointerOverBorderBrushProperty ||
			property == ListViewItemPresenter.SelectedPressedBorderBrushProperty ||
			property == ListViewItemPresenter.SelectedDisabledBorderBrushProperty)
		{
			if (m_outerBorder is not null)
			{
				SetOuterBorderBrush();
				InvalidateRender();
			}
		}
		else if (property == ListViewItemPresenter.SelectedBorderThicknessProperty)
		{
			if (m_outerBorder is not null)
			{
				SetOuterBorderThickness();
				InvalidateRender();
			}
			if (m_innerSelectionBorder is not null)
			{
				SetInnerSelectionBorderThickness();
				InvalidateRender();
			}
		}
		else if (property == ListViewItemPresenter.SelectionIndicatorBrushProperty ||
			property == ListViewItemPresenter.SelectionIndicatorPointerOverBrushProperty ||
			property == ListViewItemPresenter.SelectionIndicatorPressedBrushProperty)
		{
			// WinUI quirk: SelectionIndicatorDisabledBrush has no case, so changing it does not refresh the indicator.
			if (m_selectionIndicatorRectangle is not null)
			{
				SetSelectionIndicatorBackground();
				InvalidateRender();
			}
		}
		else if (property == ListViewItemPresenter.SelectionIndicatorCornerRadiusProperty)
		{
			if (m_selectionIndicatorRectangle is not null)
			{
				SetSelectionIndicatorCornerRadius();
				InvalidateRender();
			}
		}
		else if (property == CornerRadiusProperty)
		{
			if (IsRoundedListViewBaseItemChromeEnabled())
			{
				if (m_backplateRectangle is not null)
				{
					SetBackplateCornerRadius();
					InvalidateRender();
				}

				if (m_outerBorder is not null)
				{
					SetOuterBorderCornerRadius();
					InvalidateRender();
				}

				if (m_innerSelectionBorder is not null)
				{
					SetInnerSelectionBorderCornerRadius();
					InvalidateRender();
				}
			}
		}
	}

	private void DrawRevealBackground(
		ChromeContentRenderer pContentRenderer,
		Rect bounds)
	{
		var placeholderBounds = bounds;

		// exclude the reveal border area
		CSizeUtil.Deflate(ref placeholderBounds, GetRevealBorderThickness());

		AddRectangle(
			pContentRenderer,
			placeholderBounds,
			GetRevealBackgroundBrushNoRef()!,
			this
		);
	}

	// all XCBBs are backed by a comp brush
	// For performance reason, if it's reveal and in fallback mode, comp brush is set to null
	// LVIP would detect this and not render it.
	internal static bool IsNullCompositionBrush(Brush? brush)
	{
		var nullCompositionBrush = false;
		// Uno-specific: WinUI matches the core XamlCompositionBrushBase type, which every app-defined subclass shares.
		// RadialGradientBrush renders without setting CompositionBrush, unlike WinUI's which sets it in OnConnected.
		if (brush is XamlCompositionBrushBase xamlCompositionBrush && brush is not RadialGradientBrush)
		{
			nullCompositionBrush = xamlCompositionBrush.GetValue(XamlCompositionBrushBase.CompositionBrushProperty) is null;
		}
		return nullCompositionBrush;
	}

	internal static bool IsOpaqueBrush(Brush brush)
	{
		if (brush is SolidColorBrush solidColorBrush)
		{
			return solidColorBrush.Color.A == 0xFF;
		}
		else if (brush is GradientBrush gradientBrush)
		{
			// MUX Reference gradient.cpp (CGradientBrush::IsOpaque)
			var gradientStops = gradientBrush.GradientStops;
			var stopHasAlpha = false;
			for (var i = 0; !stopHasAlpha && i < gradientStops.Count; i++)
			{
				if (gradientStops[i].Color.A != 0xFF)
				{
					stopHasAlpha = true;
				}
			}

			return gradientStops.Count > 0 && !stopHasAlpha;
		}
		return false;
	}

	// Invoked when a TAEF test calls TestServices::Utilities::DeleteResourceDictionaryCaches().
	internal static void ClearIsRoundedListViewBaseItemChromeEnabledCache()
		=> s_isRoundedListViewBaseItemChromeEnabled = null;

	internal static bool IsRoundedListViewBaseItemChromeEnabledStatic()
	{
		if (IsRoundedListViewBaseItemChromeForced())
		{
			return true;
		}

		return DependencyProperty.GetBooleanThemeResourceValue("ListViewBaseItemRoundedChromeEnabled");
	}

	internal static bool IsRoundedListViewBaseItemChromeForced()
	{
		if (ListViewBaseItemChromeRuntimeFeatures.DenyRoundedListViewBaseItemChrome)
		{
			return false;
		}

		return ListViewBaseItemChromeRuntimeFeatures.ForceRoundedListViewBaseItemChrome;
	}

	// Dead WinUI code: IsSelectionIndicatorVisualForced (DBG only, fallback colors).
}
