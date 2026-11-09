// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewRow.cpp, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml.Hosting;
using Windows.System;
using Windows.UI.Core;
using static Microsoft.UI.Xaml.Controls._Tracing;
using static Microsoft.UI.Xaml.Controls.Tabular.TableViewAutomationHelpers;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewRow
{
	private const string s_CellsHostPartName = "PART_CellsHost";
	private const string s_GridLineBorderPartName = "PART_GridLineBorder";
	private const string s_RowExpanderGutterPartName = "PART_RowExpanderGutter";

	// Used only when there is no owner to ask; mirrors the TableViewRowExpanderSize resource.
	private const double c_defaultRowExpanderSize = 24.0;

	// Mirrors the TableViewRowIndentSize resource. Used only when there is no owner to ask.
	private const double c_defaultRowIndentSize = 16.0;

	private static readonly Thickness s_verticalThickness = new(0, 0, 1, 0);
	private static readonly Thickness s_zeroThickness = new(0, 0, 0, 0);

	private static bool WantsHorizontalLines(TableViewGridLinesVisibility visibility)
	{
		return visibility == TableViewGridLinesVisibility.Horizontal ||
			visibility == TableViewGridLinesVisibility.All;
	}

	private static bool WantsVerticalLines(TableViewGridLinesVisibility visibility)
	{
		return visibility == TableViewGridLinesVisibility.Vertical ||
			visibility == TableViewGridLinesVisibility.All;
	}

	// Failure path only: the element a column factory returned already sits in another parent. The logical
	// parent is checked first: an element held by a container outside the live tree has no visual parent yet.
	private static bool HasForeignParent(FrameworkElement? element, DependencyObject host)
	{
		try
		{
			if (element is null)
			{
				return false;
			}
			var parent = element.Parent;
			if (parent is null)
			{
				parent = VisualTreeHelper.GetParent(element);
			}
			return parent is not null && !ReferenceEquals(parent, host);
		}
		catch (Exception)
		{
			return false;
		}
	}

	// These run from OnDataContextChanged inside the rows list measure. A redundant write still
	// invalidates measure, which repeated per row per pass trips the layout-cycle failfast, so every
	// layout-affecting write on that path must be a no-op when the value is already in place.
	private static void ClearLocalValueIfSet(DependencyObject target, DependencyProperty property)
	{
		if (target.ReadLocalValue(property) != DependencyProperty.UnsetValue)
		{
			target.ClearValue(property);
		}
	}

	private static void SetThicknessIfChanged(
		DependencyObject target,
		DependencyProperty property,
		Thickness value)
	{
		var current = target.ReadLocalValue(property);
		if (current == DependencyProperty.UnsetValue ||
			(current is Thickness currentThickness ? currentThickness : new Thickness(-1, -1, -1, -1)) != value)
		{
			target.SetValue(property, value);
		}
	}

	private static bool IsKeyDown(VirtualKey key)
	{
		return (InputKeyboardSource.GetKeyStateForCurrentThread(key) &
			CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
	}

	// Collects tab stops BELOW a cell wrapper (the wrapper is owned by SetCellsTabStopInternal).
	// Keeps descending past a found tab stop: a focusable container can hold more.
	private const int c_maxCellContentTabStopDepth = 16;

	private static void AppendContentTabStops(
		DependencyObject parent, List<UIElement> tabStops, int depthBudget)
	{
		if (depthBudget <= 0)
		{
			return;
		}

		int count = VisualTreeHelper.GetChildrenCount(parent);
		for (int i = 0; i < count && i < c_maxCellContentChildrenPerLevel; ++i)
		{
			var child = VisualTreeHelper.GetChild(parent, i);
			if (child is null)
			{
				continue;
			}

			if (child is UIElement element && element.IsTabStop)
			{
				tabStops.Add(element);
			}

			AppendContentTabStops(child, tabStops, depthBudget - 1);
		}
	}

	public TableViewRow()
	{
		this.SetTabularDefaultStyleKey();

		var weakRow = new WeakReference<TableViewRow>(this);

		RegisterPropertyChangedCallback(UIElement.ContextFlyoutProperty,
			(_, _) =>
			{
				if (weakRow.TryGetTarget(out var strongRow))
				{
					strongRow.m_contextFlyoutOpeningRevoker.Disposable = TableViewDetails.HookContextFlyoutSuppression(strongRow);
				}
			});

		// handledEventsToo: a row that participates in selection marks the press handled, which also
		// suppresses XAML's gesture recognizer - this registration is what keeps begin-edit reachable.
		m_editingPointerPressedHandler = new PointerEventHandler(
			(object sender, PointerRoutedEventArgs args) =>
			{
				if (weakRow.TryGetTarget(out var strongRow))
				{
					strongRow.OnPointerPressedForEditing(sender, args);
				}
			});
		AddHandler(UIElement.PointerPressedEvent, m_editingPointerPressedHandler, true /* handledEventsToo */);

		// auto_revoke owns this self-event subscription until row destruction.
		TypedEventHandler<FrameworkElement, DataContextChangedEventArgs> dataContextChangedHandler =
			(FrameworkElement sender, DataContextChangedEventArgs args) =>
			{
				if (weakRow.TryGetTarget(out var strongRow))
				{
					strongRow.OnDataContextChanged(sender, args);
				}
			};
		DataContextChanged += dataContextChangedHandler;
		m_dataContextChangedRevoker.Disposable = Disposable.Create(() => DataContextChanged -= dataContextChangedHandler);

		// Keep CommonStates VSM in sync with IsEnabled so the Disabled
		// state activates when consumers toggle row IsEnabled at runtime.
		// auto_revoke owns this self-event subscription until row destruction.
		DependencyPropertyChangedEventHandler isEnabledChangedHandler =
			(object sender, DependencyPropertyChangedEventArgs args) =>
			{
				if (weakRow.TryGetTarget(out var strongRow))
				{
					strongRow.OnIsEnabledChanged(sender, args);
				}
			};
		IsEnabledChanged += isEnabledChangedHandler;
		m_isEnabledChangedRevoker.Disposable = Disposable.Create(() => IsEnabledChanged -= isEnabledChangedHandler);

		RoutedEventHandler gotFocusHandler =
			(object sender, RoutedEventArgs args) =>
			{
				if (weakRow.TryGetTarget(out var strongRow))
				{
					strongRow.OnRowGotFocus(args.OriginalSource as DependencyObject);
				}
			};
		GotFocus += gotFocusHandler;
		m_gotFocusRevoker.Disposable = Disposable.Create(() => GotFocus -= gotFocusHandler);

		// auto_revoke owns this self-event subscription until row destruction.
		TypedEventHandler<UIElement, ContextRequestedEventArgs> contextRequestedHandler =
			(UIElement sender, ContextRequestedEventArgs args) =>
			{
				if (weakRow.TryGetTarget(out var strongRow))
				{
					strongRow.OnRowContextRequested(args);
				}
			};
		ContextRequested += contextRequestedHandler;
		m_contextRequestedRevoker.Disposable = Disposable.Create(() => ContextRequested -= contextRequestedHandler);

		// Don't strand cell content out of the tab order if unloaded before the restore turn runs.
		RoutedEventHandler unloadedHandler =
			(object sender, RoutedEventArgs args) =>
			{
				if (weakRow.TryGetTarget(out var strongRow))
				{
					strongRow.RestoreCellContentTabStopsInternal();
				}
			};
		Unloaded += unloadedHandler;
		m_unloadedRevoker.Disposable = Disposable.Create(() => Unloaded -= unloadedHandler);
	}

	private void OnIsEnabledChanged(
		object sender,
		DependencyPropertyChangedEventArgs args)
	{
		UpdateVisualState(true /* useTransitions */);
	}

	protected override void OnApplyTemplate()
	{
		m_cellsHostContextRequestedRevoker.Disposable = null;
		m_templateRootContextRequestedRevoker.Disposable = null;
		base.OnApplyTemplate();

		ResetCellAutomationNames();
		var host = GetTemplateChild(s_CellsHostPartName) as Panel;
		m_cellsHost = host;
		m_gridLineBorder = GetTemplateChild(s_GridLineBorderPartName) as Border;
		m_rowExpanderGutter = GetTemplateChild(s_RowExpanderGutterPartName) as FrameworkElement;

		// Revoke unconditionally so a re-template that drops the gutter leaves no stale subscription.
		m_gutterPointerPressedRevoker.Disposable = null;
		m_gutterPointerReleasedRevoker.Disposable = null;
		m_gutterPointerExitedRevoker.Disposable = null;
		m_gutterPointerCaptureLostRevoker.Disposable = null;
		m_isGutterPressed = false;

		// Subscribe per template apply, not per prepare (pooled container would fan one tap into N
		// toggles). The press is handled so the row's release-time selection latch never arms; the
		// toggle itself fires on release-inside, matching TableViewGroupHeader.
		if (m_rowExpanderGutter is { } gutter)
		{
			PointerEventHandler pressedHandler = OnExpanderGutterPointerPressed;
			gutter.PointerPressed += pressedHandler;
			m_gutterPointerPressedRevoker.Disposable = Disposable.Create(() => gutter.PointerPressed -= pressedHandler);

			PointerEventHandler releasedHandler = OnExpanderGutterPointerReleased;
			gutter.PointerReleased += releasedHandler;
			m_gutterPointerReleasedRevoker.Disposable = Disposable.Create(() => gutter.PointerReleased -= releasedHandler);

			PointerEventHandler exitedHandler = OnExpanderGutterPointerCanceled;
			gutter.PointerExited += exitedHandler;
			m_gutterPointerExitedRevoker.Disposable = Disposable.Create(() => gutter.PointerExited -= exitedHandler);

			PointerEventHandler captureLostHandler = OnExpanderGutterPointerCanceled;
			gutter.PointerCaptureLost += captureLostHandler;
			m_gutterPointerCaptureLostRevoker.Disposable = Disposable.Create(() => gutter.PointerCaptureLost -= captureLostHandler);
		}

		if (host is not null)
		{
			// Match PART_HeaderHost: scope TabFocusNavigation at the host, not the shared
			// TableViewCellsPanel primitive.
			host.TabFocusNavigation = KeyboardNavigationMode.Once;
		}

		if (host is TableViewCellsPanel cellsPanel)
		{
			cellsPanel.SetOwningRowInternal(this);
		}

		// Every body request is handled below the row, so the framework never shows a row style or
		// template ContextFlyout on its own and skips the focus and current-cell handling. Cell
		// requests are caught at the cells host; padding and chevron requests at the template root.
		if (host is not null)
		{
			var weakThis = new WeakReference<TableViewRow>(this);
			TypedEventHandler<UIElement, ContextRequestedEventArgs> hostHandler =
				(UIElement _, ContextRequestedEventArgs args) =>
				{
					if (weakThis.TryGetTarget(out var self))
					{
						self.OnCellsHostContextRequested(args);
					}
				};
			host.ContextRequested += hostHandler;
			m_cellsHostContextRequestedRevoker.Disposable = Disposable.Create(() => host.ContextRequested -= hostHandler);
		}

		{
			var weakThis = new WeakReference<TableViewRow>(this);
			m_templateRootContextRequestedRevoker.Disposable = TableViewDetails.HookTemplateRootContextRequested(this,
				(UIElement _, ContextRequestedEventArgs args) =>
				{
					if (weakThis.TryGetTarget(out var self))
					{
						self.OnTemplateRootContextRequested(args);
					}
				});
		}

		RebuildCells();

		ApplyHierarchyAffordance();

		UpdateVisualState(false /* useTransitions */);
	}

	internal void SetTerminalGridLineSuppression(TerminalGridLineSuppressionState state, bool refresh = true)
	{
		m_suppressTrailingGridLine = state.suppressTrailing;
		m_suppressBottomGridLine = state.suppressBottom;
		if (refresh)
		{
			RefreshGridLines();
		}
	}

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new TableViewRowAutomationPeer(this);
	}

	// Typed accessor for the owning TableView.
	internal TableView? GetOwningTableView()
	{
		// Keep the owner weak; callers acquire a strong ref only for synchronous work.
		return m_owningTableView is not null && m_owningTableView.TryGetTarget(out var owner) ? owner : null;
	}

	// Row-side half of TableView::IsContextMenuTargetCurrent, the only caller: it has already checked
	// the target kind, row, anchor load state, column, and owning table.
	internal bool IsContextMenuTargetCurrent(
		TableViewDetails.ContextMenuTarget target)
	{
		if (target.Row is null || !target.Row.IsLoaded || target.ScopeRoot is null || !ReferenceEquals(target.ScopeRoot, m_cellsHost))
		{
			return false;
		}

		var owner = GetOwningTableView();
		if (owner is null)
		{
			return false;
		}

		var ownerImpl = owner;
		if (!TableView.SameInspectableIdentity(
				ownerImpl.UnwrapEditingDataItem(target.Row.DataContext),
				target.Item))
		{
			return false;
		}

		var repeater = ownerImpl.GetRowsRepeaterInternal();
		if (repeater is null)
		{
			return false;
		}

		var index = repeater.GetElementIndex(target.Row);
		var view = repeater.ItemsSourceView;
		if (view is null || index < 0 || index >= view.Count ||
			!TableView.SameInspectableIdentity(
				ownerImpl.UnwrapEditingDataItem(view.GetAt(index)),
				target.Item))
		{
			return false;
		}

		if (target.Column is null)
		{
			return ReferenceEquals(target.Anchor, target.Row);
		}

		return TableViewDetails.IsLiveCellAnchor(target.Anchor, target.ScopeRoot, target.Column);
	}

	internal TableViewColumn? GetCellOwningColumn(UIElement? cellElement)
	{
		if (cellElement is FrameworkElement cellFE)
		{
			if (cellFE.Tag is TableViewColumn column)
			{
				return column;
			}
		}

		return null;
	}

	// ----- Cell-level keyboard focus -----

	internal int GetVisibleCellCountInternal()
	{
		int count = 0;
		if (m_cellsHost is { } host)
		{
			var children = host.Children;
			int size = children.Count;
			for (int i = 0; i < size; ++i)
			{
				var child = children[i];
				// Same predicate as TableViewCellAutomationPeer::Column and the row peer's children, so
				// the keyboard coordinate space and the UIA one cannot drift apart.
				if (child is not null && IsVisibleColumn(GetCellOwningColumn(child)))
				{
					++count;
				}
			}
		}
		return count;
	}

	internal UIElement? GetVisibleCellInternal(int visibleColumnIndex)
	{
		if (visibleColumnIndex < 0)
		{
			return null;
		}

		if (m_cellsHost is { } host)
		{
			var children = host.Children;
			int size = children.Count;
			int visible = 0;
			for (int i = 0; i < size; ++i)
			{
				var child = children[i];
				if (child is null || !IsVisibleColumn(GetCellOwningColumn(child)))
				{
					continue;
				}
				if (visible == visibleColumnIndex)
				{
					return child;
				}
				++visible;
			}
		}
		return null;
	}

	internal int GetVisibleCellIndexInternal(UIElement? cell)
	{
		if (cell is null)
		{
			return -1;
		}

		if (m_cellsHost is { } host)
		{
			var children = host.Children;
			int size = children.Count;
			int visible = 0;
			for (int i = 0; i < size; ++i)
			{
				var child = children[i];
				if (child is null || !IsVisibleColumn(GetCellOwningColumn(child)))
				{
					continue;
				}
				if (ReferenceEquals(child, cell))
				{
					return visible;
				}
				++visible;
			}
		}
		return -1;
	}

	internal UIElement? FindOwnCellInternal(
		DependencyObject? element, bool requireExact)
	{
		var host = m_cellsHost;
		if (element is null || host is null)
		{
			return null;
		}

		DependencyObject? current = element;
		while (current is not null)
		{
			if (ReferenceEquals(current, host))
			{
				return null;
			}

			if (current is UIElement candidate)
			{
				if (ReferenceEquals(VisualTreeHelper.GetParent(candidate), host) &&
					IsVisibleColumn(GetCellOwningColumn(candidate)))
				{
					return (!requireExact || ReferenceEquals(candidate, element))
						? candidate : null;
				}
			}

			if (requireExact)
			{
				return null;
			}

			current = VisualTreeHelper.GetParent(current);
		}

		return null;
	}

	internal FrameworkElement? GetLastVisibleCellInternal()
	{
		if (m_cellsHost is { } host)
		{
			var children = host.Children;
			for (int i = children.Count; i > 0; --i)
			{
				if (children[i - 1] is FrameworkElement cell &&
					cell.Visibility == Visibility.Visible &&
					cell.ActualWidth > 0.0)
				{
					return cell;
				}
			}
		}

		return null;
	}

	internal bool FocusVisibleCellInternal(int visibleColumnIndex, FocusState state)
	{
		if (GetVisibleCellInternal(visibleColumnIndex) is { } cell)
		{
			if (cell is FrameworkElement cellFE)
			{
				cellFE.StartBringIntoView();
			}

			// Drill in BEFORE focusing: at row level the cells are not tab stops, and
			// CUIElement::IsFocusable requires IsTabStop even for a programmatic Focus().
			if (m_isCellLevel)
			{
				cell.IsTabStop = IsCellActionableInternal(cell);
			}
			else
			{
				SetCellLevelInternal(true);
			}

			if (cell.Focus(state))
			{
				return true;
			}

			// The cell refused (collapsed column, disabled subtree, a focus operation already in
			// flight). Undo the drill-in rather than leaving the row in a state where neither level is
			// a tab stop, which would strand the body with no reachable focus target at all.
			SetCellLevelInternal(false);
		}

		return Focus(state);
	}

	internal bool IsCellActionableInternal(UIElement? cell)
	{
		if (cell is not FrameworkElement cellElement || cellElement.Visibility != Visibility.Visible)
		{
			return false;
		}

		var column = GetCellOwningColumn(cell);
		if (!IsVisibleColumn(column))
		{
			return false;
		}

		if (GetOwningTableView() is { } owner && owner.IsColumnCellEditableInternal(column))
		{
			return true;
		}

		if (column!.CellContextFlyout is not null || TableViewDetails.HasEffectiveToolTip(cellElement))
		{
			return true;
		}

		// Content taken out of the tab order for one Tab walk is still interactive.
		bool isSuppressed(UIElement element)
		{
			foreach (var entry in m_suppressedContentTabStops)
			{
				if (entry.element is not null && entry.element.TryGetTarget(out var suppressed) && ReferenceEquals(suppressed, element))
				{
					return true;
				}
			}
			return false;
		}
		int budget = 64;
		return HasActionableContent(cellElement, 9, ref budget,
			m_suppressedContentTabStops.Count == 0 ? null : isSuppressed);
	}

	internal int FindActionableCellInternal(int startVisibleIndex, int step)
	{
		int count = GetVisibleCellCountInternal();
		for (int i = startVisibleIndex; i >= 0 && i < count && step != 0; i += step)
		{
			if (IsCellActionableInternal(GetVisibleCellInternal(i)!))
			{
				return i;
			}
		}
		return -1;
	}

	// ----- Two-level focus: ROW level vs CELL level -----
	//
	// Body focus is either the row or one of its cells, never both. XAML tab search enters children
	// before consulting TabFocusNavigation, so a focusable row with focusable cells creates extra
	// forward/reverse tab stops.
	//
	// Gating IsTabStop at both ends keeps the body one tab stop while still allowing row/cell arrow
	// navigation. Cells default to IsTabStop(true); row policy stamps the current level.
	internal void SetCellLevelInternal(bool isCellLevel)
	{
		m_isCellLevel = isCellLevel;
		ApplyFocusLevelInternal();
	}

	// Re-applies the current level to the live cells. Called after any rebuild, because new cell
	// wrappers arrive with IsTabStop(true) and would otherwise re-open the row-level Tab leak.
	private void ApplyFocusLevelInternal()
	{
		// Make the incoming level focusable before clearing the outgoing one, or the row has no focus
		// target during the handoff.
		if (m_isCellLevel)
		{
			// With no actionable cell left, the row stays the tab stop, or Tab could skip the table.
			bool rowIsTabStop = !SetCellsTabStopInternal(true);
			if (IsTabStop != rowIsTabStop)
			{
				IsTabStop = rowIsTabStop;
			}
		}
		else
		{
			if (!IsTabStop)
			{
				IsTabStop = true;
			}
			SetCellsTabStopInternal(false);
		}
	}

	private bool SetCellsTabStopInternal(bool isTabStop)
	{
		bool anyTabStop = false;
		if (m_cellsHost is { } host)
		{
			var children = host.Children;
			int size = children.Count;
			for (int i = 0; i < size; ++i)
			{
				if (children[i] is { } child)
				{
					// Inactive cells never take focus, by keyboard, pointer or UIA.
					bool cellIsTabStop = isTabStop && IsCellActionableInternal(child);
					if (child.IsTabStop != cellIsTabStop)
					{
						child.IsTabStop = cellIsTabStop;
					}
					anyTabStop = anyTabStop || cellIsTabStop;
				}
			}
		}
		return anyTabStop;
	}

	// Focus inside a cell's content (drilled in via Enter, or an editor) is not grid level: that
	// content owns its own keys, including Tab.
	private bool IsGridLevelFocusInternal()
	{
		var root = XamlRoot;
		if (root is null)
		{
			return false;
		}

		var focused = FocusManager.GetFocusedElement(root) as DependencyObject;
		if (focused is null)
		{
			return false;
		}

		DependencyObject selfObject = this;
		if (ReferenceEquals(focused, selfObject))
		{
			return true;
		}

		return FindOwnCellInternal(focused, true /* requireExact */) is not null;
	}

	// Takes authored cell content out of the tab order for ONE Tab walk. CFocusManager::GetNextTabStop
	// searches the focused element's children before honouring TabFocusNavigation, so cell content
	// would otherwise become extra tab stops. It can't stay suppressed: IsFocusable requires IsTabStop,
	// so pointer/programmatic focus would break too. Restored on the next dispatcher turn (after XAML's
	// default Tab move), and only if the local value is still the false written here.
	private void SuppressCellContentTabStopsForTabWalkInternal()
	{
		// Restore a pending suppression first, or our false would be recorded as the original.
		RestoreCellContentTabStopsInternal();

		List<UIElement> candidates = new();
		if (m_cellsHost is { } host)
		{
			var children = host.Children;
			uint size = (uint)children.Count;
			for (uint i = 0; i < size; ++i)
			{
				if (children[(int)i] is { } cell)
				{
					AppendContentTabStops(cell, candidates, c_maxCellContentTabStopDepth);
				}
			}
		}

		if (candidates.Count == 0)
		{
			return;
		}

		var isTabStopProperty = UIElement.IsTabStopProperty;
		var unsetValue = DependencyProperty.UnsetValue;
		object suppressedValue = false;

		foreach (var element in candidates)
		{
			var local = element.ReadLocalValue(isTabStopProperty);
			var binding = local as BindingExpression;
			bool isRestorable =
				local == unsetValue ||
				local is bool ||
				(binding is not null && binding.ParentBinding is not null);
			if (!isRestorable)
			{
				continue;
			}

			if (binding is not null)
			{
				// Detach first: SetValue over a TwoWay binding would write through to the app's source.
				element.ClearValue(isTabStopProperty);
			}

			element.SetValue(isTabStopProperty, suppressedValue);
			m_suppressedContentTabStops.Add(new SuppressedContentTabStop { element = new WeakReference<UIElement>(element), originalLocalValue = local });
		}

		if (m_suppressedContentTabStops.Count == 0)
		{
			return;
		}

		var weakRow = new WeakReference<TableViewRow>(this);
		var queue = DispatcherQueue;
		if (queue is null || !queue.TryEnqueue(() =>
			{
				if (weakRow.TryGetTarget(out var strongRow))
				{
					try
					{
						strongRow.RestoreCellContentTabStopsInternal();
					}
					catch (Exception)
					{
						// Best-effort restore; never fail-fast the dispatcher.
					}
				}
			}))
		{
			// No dispatcher: never leave content out of the tab order, even at the cost of this press.
			RestoreCellContentTabStopsInternal();
		}
	}

	// Idempotent. Elements that have left this row are still restored (the false is our write); the
	// "still our false" check preserves any newer value.
	private void RestoreCellContentTabStopsInternal()
	{
		if (m_suppressedContentTabStops.Count == 0)
		{
			return;
		}

		var suppressed = m_suppressedContentTabStops.ToArray();
		m_suppressedContentTabStops.Clear();

		var isTabStopProperty = UIElement.IsTabStopProperty;
		var unsetValue = DependencyProperty.UnsetValue;

		foreach (var entry in suppressed)
		{
			if (entry.element is null || !entry.element.TryGetTarget(out var element))
			{
				continue;
			}

			try
			{
				if (element.ReadLocalValue(isTabStopProperty) is not bool current || current)
				{
					continue;
				}

				var original = entry.originalLocalValue;
				if (original == unsetValue)
				{
					element.ClearValue(isTabStopProperty);
				}
				else if (original is BindingExpression binding)
				{
					BindingOperations.SetBinding(
						element, isTabStopProperty, binding.ParentBinding);
				}
				else
				{
					element.SetValue(isTabStopProperty, original);
				}
			}
			catch (Exception)
			{
				// One element failing must not strand the rest out of the tab order.
			}
		}
	}

	// The body is one tab stop; Enter is the route into cell content. The move is left to XAML:
	// FocusManager::TryMoveFocus ignores ItemsRepeater's KeyboardNavigationMode::Once, so Shift+Tab
	// would land in the previous row's content. Multiply expands the subtree (as TreeViewItem);
	// unhandled Left/Right go to TableView::TryHandleRowLevelDrillKey here, ahead of PART_BodyScroller,
	// and Space stays row selection.
	protected override void OnKeyDown(KeyRoutedEventArgs args)
	{
		if (!args.Handled &&
			args.Key == VirtualKey.Tab &&
			// Ctrl+Tab is app-level navigation and Alt is the resize chord; neither is a band move.
			!IsKeyDown(VirtualKey.Control) &&
			!IsKeyDown(VirtualKey.Menu) &&
			IsGridLevelFocusInternal())
		{
			SuppressCellContentTabStopsForTabWalkInternal();
		}

		// Unmodified only: Ctrl+arrow is focus-without-select; Shift/Alt chords belong to the app.
		if (!args.Handled && Level > 0 && IsRowItselfFocused() &&
			!IsKeyDown(VirtualKey.Control) &&
			!IsKeyDown(VirtualKey.Shift) &&
			!IsKeyDown(VirtualKey.Menu))
		{
			var owner = GetOwningTableView();
			if (args.Key == VirtualKey.Multiply && IsExpandable && owner is not null)
			{
				owner.ExpandRowSubtree(this);
				args.Handled = true;
			}
		}

		// Row-level Up/Down too, or PART_BodyScroller would focus a control in this row's own cells.
		if (!args.Handled &&
			(args.Key == VirtualKey.Left || args.Key == VirtualKey.Right ||
				((args.Key == VirtualKey.Up || args.Key == VirtualKey.Down) && IsRowItselfFocused())))
		{
			if (GetOwningTableView() is { } owner)
			{
				owner.HandleBodyKeyFromRowInternal(args);
			}
		}

		base.OnKeyDown(args);
	}

	// Pop-out must make the row focusable before clearing the focused cell from tab order.
	internal void EnableRowFocusInternal()
	{
		IsTabStop = true;
	}

	private void OnRowGotFocus(DependencyObject? focusedElement)
	{
		if (GetOwningTableView() is { } owner)
		{
			owner.OnRowCellFocusChanged(this, focusedElement);
		}
	}

	private void DetachColumnsSubscription()
	{
		if (m_observedColumns is not null && m_observedColumns.TryGetTarget(out _))
		{
			m_columnsVectorChangedRevoker.Disposable = null;
			m_observedColumns = null;
		}
	}

	private void AttachColumnsSubscription(TableView? owner)
	{
		if (owner is null)
		{
			return;
		}

		// Cast the ABI vector to its observable backing type for VectorChanged.
		if (owner.Columns is IObservableVector<TableViewColumn> observable)
		{
			var weakRow = new WeakReference<TableViewRow>(this);
			VectorChangedEventHandler<TableViewColumn> handler =
				(IObservableVector<TableViewColumn> sender, IVectorChangedEventArgs args) =>
				{
					if (weakRow.TryGetTarget(out var strongRow))
					{
						strongRow.OnColumnsVectorChanged(sender, args);
					}
				};
			observable.VectorChanged += handler;
			m_columnsVectorChangedRevoker.Disposable = Disposable.Create(() => observable.VectorChanged -= handler);
			m_observedColumns = new WeakReference<IObservableVector<TableViewColumn>>(observable);
		}
	}

	internal bool SetOwningTableViewInternal(TableView? owner, bool rebuildCells = true)
	{
		var currentOwner = GetOwningTableView();
		m_cellsRebuiltForDataContext = false;
		m_inRecyclePool = !rebuildCells;
		// Compare through the typed accessor so unchanged weak refs hit the no-op path.
		if (currentOwner == owner)
		{
			return false;
		}

		// Single-writer: the recycle cycle clears the owner to null between tables; a direct
		// table-A -> table-B switch (no intervening clear) is a cross-table leak, so reject it.
		if (currentOwner is not null && owner is not null)
		{
			// Runs inside an ItemsRepeater callback; an escaping throw would failfast.
			// Assert in debug, no-op in retail (keep current owner, skip re-attach).
			MUX_ASSERT(false, "TableViewRow re-owned without an intervening clear");
			return false;
		}

		DetachColumnsSubscription();

		if (owner is not null)
		{
			m_owningTableView = new WeakReference<TableView>(owner);
		}
		else
		{
			ResetCellAutomationNames();
			// Drop the row peer's cell-peer cache so the next item's cell peers are not served from it.
			if (FrameworkElementAutomationPeer.FromElement(this) is TableViewRowAutomationPeer rowPeer)
			{
				rowPeer.DropCellPeerCache();
			}
			m_owningTableView = null;
			// Reset transient interaction state so a row recycled while hovered/pressed
			// re-enters the pool in Normal state (ListViewItem parity), not a stale tint.
			m_isPointerOver = false;
			m_isPressed = false;
			m_selectOnPointerRelease = false;

			// Selection belongs to the item, not the container: a pooled row must not carry selected
			// chrome onto its next item. RefreshRowSelectionState restamps it on the way back in.
			IsSelected = false;

			// Begin-edit gesture state must go too. A row recycled away and back to the SAME item
			// inside the double-click interval would otherwise turn the next single click into an
			// edit, and the stale trackers keep the previous item and column alive.
			ResetPressState();

			// Recycled rows return at row level; otherwise a drilled row can reappear unreachable by Tab.
			m_isCellLevel = false;

			RestoreCellContentTabStopsInternal();
			// ElementPrepared may skip the rebuild, which would otherwise restamp row/cell tab stops.
			ApplyFocusLevelInternal();

			UpdateVisualState(false);
		}

		// Observe the new owner's Columns vector (no-op on recycle-out when owner is null).
		AttachColumnsSubscription(owner);

		if (!rebuildCells)
		{
			// Without the rebuild, pooled cell peers would keep their item index and advertise VirtualizedItem.
			UpdateCellPeerItems(DataContext);
			return false;
		}
		return RebuildCells();
	}

	internal bool EnsureOwningTableViewInternal(TableView? owner, bool skipIfCellsCurrent = false)
	{
		if (owner is null)
		{
			return false;
		}

		if (GetOwningTableView() != owner)
		{
			return SetOwningTableViewInternal(owner);
		}

		bool skipRebuild = m_cellsRebuiltForDataContext && skipIfCellsCurrent;
		m_cellsRebuiltForDataContext = false;
		m_inRecyclePool = false;

		var rowPeer = FrameworkElementAutomationPeer.FromElement(this) as TableViewRowAutomationPeer;
		var peerImpl = rowPeer;
		if (peerImpl is not null && !peerImpl.CanReuseForRowItem(this, owner))
		{
			// Cell providers are bound to their element, so replace the cells only if a cell peer exists;
			// otherwise keep them and let them restamp from the inherited DataContext.
			bool chromeRefreshed = false;
			if (HasCellAutomationPeers())
			{
				if (m_cellsHost is { } host)
				{
					ResetCellAutomationNames();
					host.Children.Clear();
				}
				peerImpl.DropCellPeerCache();
				chromeRefreshed = RebuildCells(false /* updateExistingCellPeerItems */);
			}
			else
			{
				peerImpl.DropCellPeerCache();
				chromeRefreshed = skipRebuild ? false : RebuildCells();
			}
			peerImpl.TrackCurrentRowItem(this, owner);
			return chromeRefreshed;
		}

		bool chromeRefreshedCurrent = skipRebuild ? false : RebuildCells();

		if (peerImpl is not null)
		{
			peerImpl.TrackCurrentRowItem(this, owner);
		}

		return chromeRefreshedCurrent;
	}

	// Rewire realized rows when Columns changes but the owner identity does not.
	internal void RefreshColumnsSubscriptionInternal()
	{
		DetachColumnsSubscription();

		var owner = GetOwningTableView();
		if (owner is null)
		{
			return;
		}

		AttachColumnsSubscription(owner);

		if (m_cellsHost is { } host)
		{
			ResetCellAutomationNames();
			host.Children.Clear();
			InvalidateCellsCurrent();
		}

		RebuildCells();
	}

	private void OnDataContextChanged(
		FrameworkElement sender,
		DataContextChangedEventArgs args)
	{
		// On recycle ItemsRepeater updates the row's DataContext; cells pick up the new item reactively
		// via inheritance (they are not restamped). RebuildCells is still called so index-dependent visuals
		// (alternating-row banding, frozen pinning) refresh for the new position, and the following
		// ElementPrepared skips its own rebuild. Guard against re-entry from child DataContext propagation.
		m_cellsRebuiltForDataContext = false;
		if (m_isRebuildingCells)
		{
			return;
		}
		var dataItem = DataContext;
		// The repeater nulls a recycled row's DataContext after ElementClearing; the next prepare rebuilds.
		if (dataItem is null && m_inRecyclePool)
		{
			return;
		}
		m_cellsInvalidated = false;
		bool rebuilt = RebuildCells();
		m_cellsRebuiltForDataContext = rebuilt && !m_cellsInvalidated && dataItem is not null && ReferenceEquals(DataContext, dataItem);
	}

	private void InvalidateCellsCurrent()
	{
		m_cellsRebuiltForDataContext = false;
		m_cellsInvalidated = true;
	}

	private void OnColumnsVectorChanged(
		IObservableVector<TableViewColumn> sender,
		IVectorChangedEventArgs args)
	{
		// Mutating Columns triggers a cell rebuild against the new column set. Coalesce a burst of column
		// changes into a single rebuild on the next tick. No explicit Children().Clear() is needed:
		// RebuildCells' restamp fast-path forces a full rebuild (which clears) whenever the column set
		// actually changed, and keeping the old cells until the tick avoids an empty-cell flash.
		QueueRebuildCells();
	}

	private void QueueRebuildCells()
	{
		// No cell host yet (template not applied); the owner-set / ApplyTemplate path builds cells.
		if (m_cellsHost is null)
		{
			return;
		}

		// A rebuild is already scheduled for this tick -- collapse the burst into one.
		if (m_rebuildCellsQueued)
		{
			return;
		}

		InvalidateCellsCurrent();

		var dispatcher = DispatcherQueue;
		if (dispatcher is null)
		{
			// No dispatcher (teardown) -- rebuild synchronously so cells are not left stale.
			RebuildCells();
			return;
		}

		m_rebuildCellsQueued = true;
		var weakRow = new WeakReference<TableViewRow>(this);
		if (!dispatcher.TryEnqueue(() =>
			{
				if (weakRow.TryGetTarget(out var strongRow))
				{
					strongRow.m_rebuildCellsQueued = false;
					try
					{
						strongRow.RebuildCells();
					}
					catch (Exception)
					{
						// Coalesced cell rebuild is best-effort; never fail-fast the dispatcher.
					}
				}
			}))
		{
			// Enqueue failed -- fall back to a synchronous rebuild so cells are not left stale.
			m_rebuildCellsQueued = false;
			RebuildCells();
		}
	}

	protected override void OnPointerPressed(PointerRoutedEventArgs args)
	{
		if (args.Handled)
		{
			return;
		}

		var pointerPoint = args.GetCurrentPoint(this);
		var props = pointerPoint.Properties;

		// Right-button focus must precede native template menus opening, without arming selection.
		// Touch reports no pressed button, so it is admitted on device type instead, matching the
		// begin-edit gesture. Without this a touch press never arms the release-time selection.
		var deviceType = args.Pointer.PointerDeviceType;
		if (deviceType != PointerDeviceType.Touch &&
			!props.IsLeftButtonPressed)
		{
			var owner = GetOwningTableView();
			var requestSource = args.OriginalSource as DependencyObject;
			if (!props.IsRightButtonPressed || owner is null ||
				!TableViewDetails.IsOwnRequestSource(requestSource, this))
			{
				return;
			}

			// Resolve from the source only, as the ContextRequested that follows does. A pointer
			// hit-test would pick the cell under the expander chevron, which belongs to the row.
			var target = ResolveContextMenuTarget(requestSource);
			if (target is { } resolvedTarget && resolvedTarget.Column is not null)
			{
				owner.FocusContextMenuTargetFromPointer(resolvedTarget);
			}
			return;
		}

		m_isPressed = true;
		UpdateVisualState(true);

		// A cell press enters cell level so keyboard/UIA focus names that cell, not the whole row.
		UIElement? pressedCell = null;
		if (args.OriginalSource is DependencyObject source)
		{
			pressedCell = FindOwnCellInternal(source, false /* requireExact */);
		}
		if (pressedCell is not null)
		{
			// Drill in before focusing; row-level cells are not focusable.
			SetCellLevelInternal(true);
			if (!pressedCell.Focus(FocusState.Pointer))
			{
				SetCellLevelInternal(false);
				Focus(FocusState.Pointer);
			}
		}
		else
		{
			// Empty strip clicks land on the row, the body's row-level focus target.
			SetCellLevelInternal(false);
			Focus(FocusState.Pointer);
		}

		m_selectOnPointerRelease = true;
		m_selectPointerId = args.Pointer.PointerId;
	}

	protected override void OnPointerEntered(PointerRoutedEventArgs args)
	{
		m_isPointerOver = true;
		UpdateVisualState(true);
	}

	protected override void OnPointerExited(PointerRoutedEventArgs args)
	{
		m_isPointerOver = false;
		m_isPressed = false;

		// The contact left the row, so a deferred selection is no longer a tap on it. Only the pointer
		// that armed it may disarm it: with two contacts on the same row, the other one leaving must
		// not cancel this one's pending selection. Matches the check in OnPointerReleased.
		if (args.Pointer.PointerId == m_selectPointerId)
		{
			m_selectOnPointerRelease = false;
		}

		UpdateVisualState(true);
	}

	protected override void OnPointerReleased(PointerRoutedEventArgs args)
	{
		m_isPressed = false;

		// Deferred selection: the pointer came up on this row without a pan or a capture loss taking
		// it away, so it was a tap. Only the pointer that armed it can commit or disarm it.
		bool isArmingPointer = args.Pointer.PointerId == m_selectPointerId;
		bool selectNow = m_selectOnPointerRelease && isArmingPointer;
		if (isArmingPointer)
		{
			m_selectOnPointerRelease = false;
		}

		if (selectNow)
		{
			if (GetOwningTableView() is { } owner)
			{
				owner.OnRowPointerSelect(this);
			}
		}

		UpdateVisualState(true);
	}

	protected override void OnPointerCaptureLost(PointerRoutedEventArgs args)
	{
		m_isPressed = false;

		// A ScrollViewer took the pointer for a pan; the gesture was a scroll, not a tap. Only the
		// arming pointer disarms, so a second contact panning does not cancel the first one's tap.
		if (args.Pointer.PointerId == m_selectPointerId)
		{
			m_selectOnPointerRelease = false;
		}

		UpdateVisualState(true);
	}

	protected override void OnPointerCanceled(PointerRoutedEventArgs args)
	{
		// Palm rejection or a system gesture. Can arrive without a preceding PointerCaptureLost, which
		// would otherwise leave the latch set and let the next unrelated release select this row.
		m_isPressed = false;

		if (args.Pointer.PointerId == m_selectPointerId)
		{
			m_selectOnPointerRelease = false;
		}

		UpdateVisualState(true);
	}

	private new void UpdateVisualState(bool useTransitions)
	{
		// One GoToState into one group: the selected states share CommonStates so nothing depends on
		// call order (TreeViewItem / ItemContainer pattern). Disabled wins, for ListViewItem parity.
		string state;
		if (IsSelected)
		{
			state = !IsEnabled ? "SelectedDisabled"
				: m_isPressed ? "SelectedPressed"
				: m_isPointerOver ? "SelectedPointerOver"
				: "Selected";
		}
		else
		{
			state = !IsEnabled ? "Disabled"
				: m_isPressed ? "Pressed"
				: m_isPointerOver ? "PointerOver"
				: "Normal";
		}

		VisualStateManager.GoToState(this, state, useTransitions);
	}

	internal void SetIsSelectedInternal(bool isSelected)
	{
		if (IsSelected == isSelected)
		{
			return;
		}

		IsSelected = isSelected;
		UpdateVisualState(true /* useTransitions */);
	}

	internal void SetHierarchyStateInternal(int level, bool isExpandable, bool isExpanded)
	{
		// Snapshot for the automation event (a recycled row moving to another node is a real transition).
		ExpandCollapseState oldState = HierarchyExpandCollapseState();

		// Guarded: setters box before SetValue, and re-preparing onto the same node is the common case.
		if (Level != level)
		{
			Level = level;
		}

		if (IsExpandable != isExpandable)
		{
			IsExpandable = isExpandable;
		}

		if (IsExpanded != isExpanded)
		{
			IsExpanded = isExpanded;
		}

		// Unconditional: indent also depends on the indent resource and grouping. Writes inside are
		// equality-guarded, so no layout re-invalidation.
		ApplyHierarchyAffordance();

		RaiseExpandCollapseStateChanged(oldState, HierarchyExpandCollapseState());

		// Only an existing peer can have handed a client cached set metadata.
		if (AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
		{
			if (FrameworkElementAutomationPeer.FromElement(this) is TableViewRowAutomationPeer rowPeer)
			{
				rowPeer.RaiseHierarchyPositionChanged();
			}
		}
	}

	// Computed here, not on the peer, so transitions are measurable without forcing a peer.
	internal ExpandCollapseState HierarchyExpandCollapseState()
	{
		if (Level <= 0 || !IsExpandable)
		{
			return ExpandCollapseState.LeafNode;
		}

		return IsExpanded
			? ExpandCollapseState.Expanded
			: ExpandCollapseState.Collapsed;
	}

	private void RaiseExpandCollapseStateChanged(
		ExpandCollapseState oldState,
		ExpandCollapseState newState)
	{
		if (oldState == newState)
		{
			return;
		}

		// Runs on every prepare; avoid forcing peers when no client listens.
		if (!AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
		{
			return;
		}

		// XAML caches the peer per element, so the fallback yields the same instance a client holds.
		var peer = FrameworkElementAutomationPeer.FromElement(this);
		if (peer is null)
		{
			peer = FrameworkElementAutomationPeer.CreatePeerForElement(this);
		}

		if (peer is TableViewRowAutomationPeer rowPeer)
		{
			rowPeer.RaiseExpandCollapseAutomationEvent(
				oldState, newState);
		}
	}

	// Keys bubbling from cell content (editors, ComboBox) must not toggle the row.
	private bool IsRowItselfFocused()
	{
		var root = XamlRoot;
		if (root is null)
		{
			return false;
		}

		var focused = FocusManager.GetFocusedElement(root) as UIElement;
		return focused is not null && ReferenceEquals(focused, this);
	}

	// Places the chevron and reserves room in the lead cell. Separate so template apply and cell
	// rebuild (fresh gutter/wrappers) can re-apply against unchanged state.
	internal void ApplyHierarchyAffordance()
	{
		int level = Level;
		bool isHierarchical = level > 0;

		VisualStateManager.GoToState(
			this, isHierarchical ? "RowHierarchical" : "RowNotHierarchical", false /* useTransitions */);
		VisualStateManager.GoToState(
			this, IsExpandable ? "RowExpandable" : "RowNotExpandable", false /* useTransitions */);
		VisualStateManager.GoToState(
			this, IsExpanded ? "RowExpanded" : "RowCollapsed", false /* useTransitions */);

		double indent = HierarchyIndent();

		if (m_rowExpanderGutter is { } gutter)
		{
			// Set directly: a visual state undoing the Collapsed base value would only revert to it.
			var visibility = isHierarchical ? Visibility.Visible : Visibility.Collapsed;
			if (gutter.Visibility != visibility)
			{
				gutter.Visibility = visibility;
			}

			Thickness gutterMargin = new(indent, 0, 0, 0);
			if (gutter.Margin.Left != gutterMargin.Left)
			{
				gutter.Margin = gutterMargin;
			}
		}

		// The clip is in gutter space, which the margin just moved.
		UpdateExpanderGutterClip();

		ApplyHierarchyIndentToCells(indent);

		// A Level change without re-prepare skips RefreshFrozenColumnLayout; sync the pinned lead
		// cell's Translation here.
		if (m_cellsHost is { } host)
		{
			var gutterForSync = m_rowExpanderGutter;
			if (gutterForSync is not null && (isHierarchical || gutterForSync.Translation.X != 0.0f))
			{
				SyncExpanderGutterWithLeadCell(host);
			}
		}
	}

	// Level is 1-based (UIA); a root gets no indent beyond the gutter.
	internal double HierarchyIndent()
	{
		int level = Level;
		if (level <= 0)
		{
			return 0.0;
		}

		double indentSize = c_defaultRowIndentSize;
		double baseIndent = 0.0;
		if (GetOwningTableView() is { } owner)
		{
			var ownerImpl = owner;

			indentSize = ownerImpl.GetRowIndentSize();

			// Under a group header, roots are indented by its gutter + spacing (not its Padding);
			// ungrouped roots stay flush.
			if (ownerImpl.IsTableViewSourceGrouped())
			{
				baseIndent = ownerImpl.GetGroupHeaderContentOffset();
			}
		}

		return baseIndent + (level - 1) * indentSize;
	}

	// Reserves chevron space via lead cell WRAPPER padding (column content may be any app element).
	// Separate so column-visibility changes can move it without re-running visual states.
	internal void ApplyHierarchyIndentToCells()
	{
		ApplyHierarchyIndentToCells(HierarchyIndent());
	}

	internal void ApplyHierarchyIndentToCells(double indent)
	{
		var host = m_cellsHost;
		if (host is null)
		{
			return;
		}

		bool isHierarchical = Level > 0;

		// From the resource, not the element: gutter may not be measured yet.
		double expanderSize = c_defaultRowExpanderSize;
		if (isHierarchical)
		{
			if (GetOwningTableView() is { } owner)
			{
				expanderSize = owner.GetRowExpanderSize();
			}
		}

		var children = host.Children;
		bool leadAssigned = false;
		for (int i = 0; i < children.Count; ++i)
		{
			var wrapper = children[i] as Grid;
			if (wrapper is null || TableViewCellsPanel.ColumnForCell(wrapper) is null)
			{
				continue;
			}

			// Lead = first VISIBLE cell; reset all others so hiding/showing columns never strands or
			// duplicates the reservation.
			bool isLead =
				!leadAssigned && wrapper.Visibility == Visibility.Visible;
			leadAssigned = leadAssigned || isLead;

			double reserved = (isLead && isHierarchical) ? indent + expanderSize : 0.0;
			var padding = wrapper.Padding;
			if (padding.Left != reserved)
			{
				padding.Left = reserved;
				wrapper.Padding = padding;
			}
		}
	}

	private void OnExpanderGutterPointerPressed(
		object sender,
		PointerRoutedEventArgs args)
	{
		if (!IsExpandable)
		{
			// Unhandled so a press on a leaf's gutter selects like the cell.
			return;
		}

		// Primary only (incl. touch/pen) so context-menu presses reach the row.
		if (!args.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
		{
			return;
		}

		// Keep the press from arming the row's release-time selection latch.
		args.Handled = true;
		m_isGutterPressed = true;
	}

	private void OnExpanderGutterPointerReleased(
		object sender,
		PointerRoutedEventArgs args)
	{
		// Released over the gutter (no capture is taken), so a press that drags off never toggles.
		bool wasPressed = m_isGutterPressed;
		m_isGutterPressed = false;
		if (!wasPressed || !IsExpandable)
		{
			return;
		}

		args.Handled = true;

		if (GetOwningTableView() is { } owner)
		{
			// Shared with group headers: resolves identity now, defers the reshape.
			owner.ToggleGroupExpansion(this);
		}
	}

	private void OnExpanderGutterPointerCanceled(
		object sender,
		PointerRoutedEventArgs args)
	{
		m_isGutterPressed = false;
	}

	private bool IsWithinExpanderGutter(object? source)
	{
		var gutter = m_rowExpanderGutter;
		if (gutter is null)
		{
			return false;
		}

		DependencyObject self = this;
		for (var current = source as DependencyObject; current is not null && !ReferenceEquals(current, self);
			current = VisualTreeHelper.GetParent(current))
		{
			if (ReferenceEquals(current, gutter))
			{
				return true;
			}
		}

		return false;
	}

	internal void OnCellsArrangedInternal(double leadLeft, double leadWidth, double height)
	{
		if (m_cellsArranged && leadLeft == m_leadCellLeft && leadWidth == m_leadCellWidth && height == m_cellsArrangedHeight)
		{
			return;
		}

		m_cellsArranged = true;
		m_leadCellLeft = leadLeft;
		m_leadCellWidth = leadWidth;
		m_cellsArrangedHeight = height;
		UpdateExpanderGutterClip();
	}

	// Confine the chevron to the lead cell's slot so it never paints over or takes presses from the next
	// column. Gutter and host share origin and Translation, so subtracting the gutter's left margin maps
	// the slot into gutter space (holds under frozen pinning and RTL). Clip also bounds hit-testing.
	private void UpdateExpanderGutterClip()
	{
		var gutter = m_rowExpanderGutter;
		if (gutter is null)
		{
			return;
		}

		// Every column hidden: clip away and disable hit-testing until the next arrange.
		if (Level > 0 && m_cellsArranged && m_leadCellWidth < 0.0)
		{
			var emptyGeometry = gutter.Clip;
			if (emptyGeometry is null)
			{
				emptyGeometry = new RectangleGeometry();
				gutter.Clip = emptyGeometry;
			}
			Rect empty = new(0.0, 0.0, 0.0, 0.0);
			var currentEmpty = emptyGeometry.Rect;
			if (currentEmpty.Width != 0.0 || currentEmpty.Height != 0.0 || currentEmpty.X != 0.0 || currentEmpty.Y != 0.0)
			{
				emptyGeometry.Rect = empty;
			}
			if (gutter.IsHitTestVisible)
			{
				gutter.IsHitTestVisible = false;
			}
			return;
		}

		// Not arranged yet (or flat): leave the gutter unclipped until the first arrange reports a slot.
		if (Level <= 0 || m_leadCellWidth < 0.0 || m_cellsArrangedHeight <= 0.0)
		{
			if (gutter.Clip is not null)
			{
				gutter.Clip = null;
			}
			if (!gutter.IsHitTestVisible)
			{
				gutter.IsHitTestVisible = true;
			}
			return;
		}

		double gutterLeft = gutter.Margin.Left;
		double visibleLeft = Math.Max(0.0, m_leadCellLeft - gutterLeft);
		double visibleWidth = Math.Max(0.0, m_leadCellLeft + m_leadCellWidth - gutterLeft - visibleLeft);

		var geometry = gutter.Clip;
		if (geometry is null)
		{
			geometry = new RectangleGeometry();
			gutter.Clip = geometry;
		}
		Rect rect = new(
			(float)visibleLeft,
			0.0f,
			(float)visibleWidth,
			(float)m_cellsArrangedHeight);
		var current = geometry.Rect;
		if (current.X != rect.X || current.Y != rect.Y || current.Width != rect.Width || current.Height != rect.Height)
		{
			geometry.Rect = rect;
		}

		bool hitTestable = visibleWidth > 0.0;
		if (gutter.IsHitTestVisible != hitTestable)
		{
			gutter.IsHitTestVisible = hitTestable;
		}
	}

	internal void RefreshDensity()
	{
		// Re-read density metrics and restamp built-in cell padding.
		RebuildCells();
	}

	internal void RefreshCells()
	{
		if (m_cellsHost is { } host)
		{
			ResetCellAutomationNames();
			host.Children.Clear();
			InvalidateCellsCurrent();
		}

		RebuildCells();
	}

	internal void RefreshColumnVisibility(TableViewColumn? column, Visibility visibility)
	{
		if (column is null)
		{
			return;
		}

		if (TableViewCellsPanel.CellForColumn(m_cellsHost, column) is { } cell)
		{
			cell.Visibility = visibility;
		}

		// Lead cell may have changed. Indent only: runs from a column property callback, so no
		// visual-state transitions.
		ApplyHierarchyIndentToCells();
	}

	internal double MeasuredWidthForColumn(TableViewColumn? column)
	{
		if (m_cellsHost is { } host)
		{
			if (host is TableViewCellsPanel cellsPanel)
			{
				return cellsPanel.MeasuredWidthForColumn(column);
			}
		}

		return 0.0;
	}

	internal void InvalidateCells()
	{
		if (m_cellsHost is { } host)
		{
			host.InvalidateMeasure();
		}
	}

	private void ResetCellAutomationNames()
	{
		if (m_cellsHost is { } host)
		{
			foreach (var cell in host.Children)
			{
				try
				{
					if (TableViewCell.TryGetExistingPeer(cell) is TableViewCellAutomationPeer peer)
					{
						peer.ResetEditName();
					}
				}
				catch (Exception)
				{
					TVDiag.LogRetailF("[TableView] Optional released-cell name state could not be reset.");
				}
			}
		}
	}

	private void UpdateCellPeerItems(object? dataItem)
	{
		if (m_cellsHost is { } host)
		{
			try
			{
				foreach (var cell in host.Children)
				{
					if (TableViewCell.TryGetExistingPeer(cell) is TableViewCellAutomationPeer peer)
					{
						peer.UpdateNameItem(dataItem);
					}
				}
			}
			catch (Exception)
			{
				TVDiag.LogRetailF("[TableView] Optional recycled-cell name state could not be refreshed.");
			}
		}
	}

	private bool HasCellAutomationPeers()
	{
		if (m_cellsHost is { } host)
		{
			foreach (var cell in host.Children)
			{
				try
				{
					if (TableViewCell.TryGetExistingPeer(cell) is not null)
					{
						return true;
					}
				}
				catch (Exception)
				{
					// Unknown provider state: take the replace path, which is always correct.
					return true;
				}
			}
		}
		return false;
	}

	internal void RaiseCellAutomationNamesChangedInternal(UIElement? skipCell = null)
	{
		if (m_cellsHost is { } host)
		{
			foreach (var cell in host.Children)
			{
				if (skipCell is not null && ReferenceEquals(cell, skipCell))
				{
					continue;
				}

				try
				{
					if (TableViewCell.TryGetExistingPeer(cell) is TableViewCellAutomationPeer peer)
					{
						if (peer.TakeLastNameInternal() is { } oldName)
						{
							var newName = peer.GetName();
							if (newName != oldName)
							{
								peer.RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty,
									oldName, newName);
							}
						}
					}
				}
				catch (Exception)
				{
					TVDiag.LogRetailF("[TableView] Optional cell name change could not be raised.");
				}
			}
		}
	}

	private bool RebuildCells(bool updateExistingCellPeerItems = true)
	{
		RestoreCellContentTabStopsInternal();

		var host = m_cellsHost;
		if (host is null)
		{
			return false;
		}

		// Re-entry guard. See OnDataContextChanged.
		if (m_isRebuildingCells)
		{
			// The outer pass may already be past the point this request needed.
			InvalidateCellsCurrent();
			return false;
		}
		m_isRebuildingCells = true;
		m_cellsRebuiltForDataContext = false;
		// Synchronous RAII guard: captures this only until RebuildCells returns.
		try
		{
			var owner = GetOwningTableView();
			if (owner is null)
			{
				// Keep realized cells on recycle-out so the next ElementPrepared restamps
				// instead of re-generating every cell (the restamp fast-path exists for this).
				return false;
			}

			// Density: apply the owning TableView's row min-height (Fluent Standard = 40,
			// Compact = 30, Comfortable = 48; resolved from the TableViewRowMinHeight* resources).
			var ownerImpl = owner;
			var rowMinHeight = ownerImpl.GetDensityRowMinHeight();
			if (MinHeight != rowMinHeight)
			{
				MinHeight = rowMinHeight;
			}

			var columns = owner.Columns;
			bool isOwnedColumn(TableViewColumn? column)
			{
				return column is not null && column.GetOwningTableView() == owner;
			}
			if (columns is null)
			{
				// Columns went away entirely. Clearing here without closing the edit first would rip a live
				// editor out of the tree while the control still believed it was editing - and it wedges
				// permanently: the next pass sees zero children AND zero columns, takes the restamp
				// fast-path below, and returns before ever reaching the teardown. IsEditing would stay true
				// for the lifetime of the control, with no editor and no way for the app to recover.
				if (m_editingCellWrapper is not null)
				{
					if (GetOwningTableView() is { } editOwner)
					{
						editOwner.TerminateEditWithoutVisualRestore(true /* insideLayoutPass */);
					}
					else
					{
						AbandonCellEdit();
					}

					// Refused to close (an edit still in its Beginning window). Leave the cells alone
					// rather than orphan the editor; the next pass retries once the edit has closed.
					if (m_editingCellWrapper is not null)
					{
						return false;
					}
				}

				ResetCellAutomationNames();
				host.Children.Clear();
				return false;
			}

			var dataContext = DataContext;
			object? dataItem = dataContext;
			var children = host.Children;
			if (updateExistingCellPeerItems)
			{
				UpdateCellPeerItems(dataItem);
			}

			uint nonNullColumnCount = 0;
			foreach (var column in columns)
			{
				if (isOwnedColumn(column))
				{
					++nonNullColumnCount;
				}
			}

			bool canRestampCells = children.Count == nonNullColumnCount;

			// The fast-path reuses cells and deliberately does not close an open edit. That is only sound
			// while the editor is still one of those cells. If it has been orphaned - its wrapper is no
			// longer a child - reusing them would leave the control editing an element outside the tree,
			// so force the rebuild path, which tears the edit down.
			if (canRestampCells)
			{
				if (m_editingCellWrapper is { } editingWrapper)
				{
					bool stillAttached = false;
					for (int i = 0; i < children.Count; ++i)
					{
						if (ReferenceEquals(children[i], editingWrapper))
						{
							stillAttached = true;
							break;
						}
					}

					if (!stillAttached)
					{
						canRestampCells = false;
					}
				}
			}

			int childIndex = 0;
			if (canRestampCells)
			{
				foreach (var column in columns)
				{
					if (!isOwnedColumn(column))
					{
						continue;
					}

					var cellWrapper = children[childIndex] as Grid;
					if (cellWrapper is null || !ReferenceEquals(cellWrapper.Tag as TableViewColumn, column))
					{
						canRestampCells = false;
						break;
					}
					++childIndex;
				}
			}

			if (canRestampCells)
			{
				var cellPadding = ownerImpl.GetDensityCellPadding();
				childIndex = 0;
				foreach (var column in columns)
				{
					if (!isOwnedColumn(column))
					{
						continue;
					}

					var cellWrapper = (Grid)children[childIndex];
					// Do NOT re-push data during ItemsRepeater measure: live DataContext/Content mutation re-entered layout and hit 0xc0000420 on scroll.
					// Cells must update reactively from inherited DataContext; only equal-valued visual restamps are safe here.
					if (cellWrapper.Visibility != column.Visibility)
					{
						cellWrapper.Visibility = column.Visibility;
					}
					if (cellWrapper.MinHeight != rowMinHeight)
					{
						cellWrapper.MinHeight = rowMinHeight;
					}

					if (TableViewCell.Child(cellWrapper) is FrameworkElement cellElement)
					{
						if (cellElement is TextBlock textBlock)
						{
							if (textBlock.Padding != cellPadding)
							{
								textBlock.Padding = cellPadding;
							}
						}
					}

					++childIndex;
				}

				// Pin immediately so recycled frozen cells do not wait for the next scroll.
				if (GetOwningTableView() is { } owningView)
				{
					owningView.PinFrozenColumnsForRow(this);
				}

				ApplyFocusLevelInternal();

				RefreshGridLines();
				RefreshRowBackground();
				ApplyHierarchyAffordance();
				return true;
			}

			// Column adds/removes/reorders or template changes rebuild cells; width-only changes are picked
			// up automatically by the cell panel's arrange (it reads each column's resolved ActualWidth), so
			// no cell rebuild is needed for those.
			//
			// This path destroys every cell, so an open editor cannot survive it. The restamp fast-path above
			// deliberately does NOT tear the edit down: it reuses the cells, so an editor opened on this row
			// stays valid through the routine re-measure that installing it provokes.
			//
			// The teardown must not restore the display child - RebuildCells can run inside the repeater's
			// measure pass. See AbandonCellEdit.
			if (m_editingCellWrapper is not null)
			{
				if (GetOwningTableView() is { } editOwner)
				{
					editOwner.TerminateEditWithoutVisualRestore(true /* insideLayoutPass */);
				}
				else
				{
					AbandonCellEdit();
				}

				// If the edit still refuses to close, clearing the children would rip the live editor out
				// of the tree while the control still believes it is editing. Leave the cells alone; the
				// rebuild happens on the next pass once the edit has actually closed.
				if (m_editingCellWrapper is not null)
				{
					return false;
				}
			}

			ResetCellAutomationNames();
			host.Children.Clear();

			int visibleColumnIndex = 0;
			foreach (var column in columns)
			{
				if (!isOwnedColumn(column))
				{
					continue;
				}

				var cellWrapper = TableViewCell.Create(this, column, visibleColumnIndex);
				if (column.Visibility == Visibility.Visible)
				{
					++visibleColumnIndex;
				}
				cellWrapper.Tag = column;
				cellWrapper.Visibility = column.Visibility;
				cellWrapper.MinHeight = rowMinHeight;
				ownerImpl.AttachFocusCornerRadius(cellWrapper);

				// A null Background does not hit-test; Transparent keeps the full cell pressable so clicks
				// in padding still set current cell and support double-click-to-edit.
				if (m_cellWrapperBackground is null)
				{
					m_cellWrapperBackground = new SolidColorBrush(Colors.Transparent);
				}
				cellWrapper.Background = m_cellWrapperBackground;

				// No local DataContext anywhere on the cell path: cells inherit the row item so recycled
				// rows update reactively. Custom columns must also bind to inherited DataContext, not bake
				// in the initial dataItem.
				if (column.GenerateElement(dataItem) is { } cellElement)
				{
					AttachCellContent(cellWrapper, cellElement);
				}

				// Opt-in per-cell tooltip. Set once; the binding then tracks the row's inherited DataContext.
				if (column.CellToolTipBinding is { } toolTipBinding)
				{
					TableViewDetails.ApplyCellToolTipBinding(cellWrapper, toolTipBinding);
				}

				host.Children.Add(cellWrapper);
			}

			// Pin immediately so rebuilt frozen cells do not wait for the next scroll.
			if (GetOwningTableView() is { } rebuiltOwningView)
			{
				rebuiltOwningView.PinFrozenColumnsForRow(this);
			}

			// New wrappers default to IsTabStop(true); restamp the current level so rebuilds do not reopen
			// the row-to-first-cell Tab leak.
			ApplyFocusLevelInternal();

			RefreshGridLines();
			RefreshRowBackground();
			ApplyHierarchyAffordance();
			return true;
		}
		finally
		{
			m_isRebuildingCells = false;
		}
	}

	// Recycle-out. Always walks: the bound value can revive a tooltip at any time, so a cached
	// per-row flag would go stale and strand app content in the pool.
	internal void ReleaseCellToolTips()
	{
		if (m_cellsHost is { } host)
		{
			ClearOwnedCellToolTips(host);
		}
	}

	// Recycle-in. ReleaseCellToolTips neutralized the tooltips, and a bound value that did not change
	// (an explicit Source, or equal across the DataContext reset) raises no callback to revive them.
	internal void RefreshCellToolTips()
	{
		var host = m_cellsHost;
		if (host is null)
		{
			return;
		}

		var editingWrapper = m_editingCellWrapper;
		var children = host.Children;
		uint count = (uint)children.Count;
		for (uint i = 0; i < count; ++i)
		{
			if (children[(int)i] is Grid cellWrapper && !ReferenceEquals(cellWrapper, editingWrapper))
			{
				// Usually the value changed with the DataContext and the tooltip is already live.
				var record = TableViewDetails.GetRecord(cellWrapper);
				if (record is null || record.ToolTip is null || record.ToolTip.IsEnabled)
				{
					continue;
				}

				try
				{
					TableViewDetails.RefreshOwnedToolTip(cellWrapper);
				}
				catch (Exception ex)
				{
					TVDiag.LogRetailF($"[TableView] Re-applying a cell tooltip failed (HRESULT 0x{ex.HResult:X8}).");
				}
			}
		}

		// A tooltip makes a cell actionable, so the tab stops may have changed.
		ApplyFocusLevelInternal();
	}

	internal void OnCellToolTipChangedInternal(UIElement cell)
	{
		// Restamps every row and hands a focused cell that lost its only action to its row.
		var owner = GetOwningTableView();
		if (m_isCellLevel && owner is not null && cell.IsTabStop != IsCellActionableInternal(cell))
		{
			owner.RefreshCellFocusLevelsInternal();
		}
	}

	private void ClearOwnedCellToolTips(Panel host)
	{
		var children = host.Children;
		int count = children.Count;
		for (int i = 0; i < count; ++i)
		{
			if (children[i] is Grid cellWrapper)
			{
				TableViewDetails.ClearOwnedToolTip(cellWrapper);
			}
		}
	}

	// Installs a generated display element as a cell's content, including the ContentPresenter wiring a
	// template column needs. Shared by the cell rebuild and by the post-commit refresh, because
	// GenerateElement alone is NOT a complete cell - forgetting the second half leaves a template
	// column's Content unbound and the cell blank.
	private void AttachCellContent(Grid? cellWrapper, FrameworkElement? cellElement)
	{
		if (cellWrapper is null || cellElement is null)
		{
			return;
		}

		TableViewCell.Child(cellWrapper, cellElement);

		// Bind Content to the wrapper's inherited DataContext so recycled template cells track the new
		// item; binding to the presenter itself would freeze stale content.
		if (cellElement is ContentPresenter presenter)
		{
			if (presenter.ContentTemplate is not null)
			{
				Binding contentBinding = new();
				contentBinding.Source = cellWrapper;
				contentBinding.Path = new PropertyPath("DataContext");
				BindingOperations.SetBinding(
					presenter,
					ContentPresenter.ContentProperty,
					contentBinding);
			}
		}
	}

	// Grid lines only: the row's horizontal bottom line and the vertical per-cell separators (driven by
	// GridLinesVisibility). Row background / alternating banding lives in RefreshRowBackground.
	internal void RefreshGridLines()
	{
		var owner = GetOwningTableView();
		if (owner is null)
		{
			return;
		}

		var visibility = owner.GridLinesVisibility;
		if (WantsHorizontalLines(visibility))
		{
			ClearLocalValueIfSet(this, Control.BorderThicknessProperty);
		}
		else
		{
			SetThicknessIfChanged(this, Control.BorderThicknessProperty, s_zeroThickness);
		}

		if (m_gridLineBorder is { } gridLineBorder)
		{
			var thickness = BorderThickness;
			if (m_suppressBottomGridLine)
			{
				thickness.Bottom = 0.0;
			}
			if (gridLineBorder.BorderThickness != thickness)
			{
				gridLineBorder.BorderThickness = thickness;
			}
		}

		var host = m_cellsHost;
		if (host is null)
		{
			return;
		}

		bool wantVertical = WantsVerticalLines(visibility);
		Brush? gridLineBrush = null;
		// FlowDirection mirrors the whole subtree, so the logical right edge is trailing in RTL too.
		var verticalThickness = s_verticalThickness;
		if (wantVertical)
		{
			gridLineBrush = owner.GetGridLineBrush();
		}

		var children = host.Children;
		int childCount = children.Count;
		int lastVisibleCell = childCount;
		// The cell wrapper is a composed Grid. Border is sealed and cannot host a custom
		// automation peer, so both loops in RefreshGridLines must cast cell wrappers to Grid.
		for (int i = childCount; i > 0; --i)
		{
			if (children[i - 1] is Grid lastCellWrapper)
			{
				var column = lastCellWrapper.Tag as TableViewColumn;
				if (lastCellWrapper.Visibility == Visibility.Visible &&
					column is not null &&
					column.ActualWidth > 0.0)
				{
					lastVisibleCell = i - 1;
					break;
				}
			}
		}

		for (int i = 0; i < childCount; ++i)
		{
			if (children[i] is Grid cellWrapper)
			{
				if (wantVertical)
				{
					// RTL-aware thickness, not the LTR-only static: the separator must sit on the
					// trailing edge in both flow directions.
					if (cellWrapper.BorderThickness != verticalThickness)
					{
						cellWrapper.BorderThickness = verticalThickness;
					}
					// Keep the separator's layout thickness stable and suppress only its brush when
					// the terminal cell actually meets the outer border.
					var brush =
						m_suppressTrailingGridLine &&
						cellWrapper.Visibility == Visibility.Visible &&
						i == lastVisibleCell
							? null
							: gridLineBrush;
					if (!ReferenceEquals(cellWrapper.BorderBrush, brush))
					{
						cellWrapper.BorderBrush = brush;
					}
				}
				else
				{
					ClearLocalValueIfSet(cellWrapper, Grid.BorderThicknessProperty);
					ClearLocalValueIfSet(cellWrapper, Grid.BorderBrushProperty);
				}
			}
		}
	}

	// Row background / alternating banding only. Index-dependent (parity), so it must refresh when the
	// row's position changes on recycle, and when RowBackground / AlternatingRowBackground change.
	internal void RefreshRowBackground()
	{
		var owner = GetOwningTableView();
		if (owner is null)
		{
			return;
		}

		// Resolve the target first and write only on change, so recycled rows drop stale banding fills
		// without a clear-then-set churn on every DataContext change.
		Brush? background = null;
		if (owner.RowBackground is not null || owner.AlternatingRowBackground is not null)
		{
			var rowIndex = -1;
			if (owner.GetRowsRepeaterInternal() is { } repeater)
			{
				rowIndex = repeater.GetElementIndex(this);
			}
			if (rowIndex >= 0)
			{
				// RowBackground is the base for every row; AlternatingRowBackground overrides
				// odd rows only when set (WPF DataGrid parity). Setting RowBackground alone
				// must fill all rows uniformly, not stripe odd rows transparent.
				background = owner.RowBackground;
				if ((rowIndex % 2) != 0 && owner.AlternatingRowBackground is not null)
				{
					background = owner.AlternatingRowBackground;
				}
			}
		}

		if (background is not null)
		{
			if (!ReferenceEquals(ReadLocalValue(Control.BackgroundProperty), background))
			{
				Background = background;
			}
		}
		else
		{
			ClearLocalValueIfSet(this, Control.BackgroundProperty);
		}
	}

	internal void RefreshFrozenColumnLayout(double horizontalOffset, double leadingFrozenWidth)
	{
		if (m_cellsHost is { } host)
		{
			TableViewCellsPanel.ApplyFrozenColumnLayout(host, horizontalOffset, leadingFrozenWidth);

			// Same gate as ApplyHierarchyAffordance: flat rows skip this on every scroll pass, but a
			// row leaving a tree still resets its gutter Translation.
			var gutter = m_rowExpanderGutter;
			if (gutter is not null && (Level > 0 || gutter.Translation.X != 0.0f))
			{
				SyncExpanderGutterWithLeadCell(host);
			}
		}
	}

	// Gutter sits outside the cells host, so mirror the lead visible wrapper's Translation (whatever
	// frozen pinning decided, including none) to keep the chevron over its cell.
	internal void SyncExpanderGutterWithLeadCell(Panel host)
	{
		var gutter = m_rowExpanderGutter;
		if (gutter is null)
		{
			return;
		}

		float offset = 0.0f;
		if (Level > 0)
		{
			var children = host.Children;
			for (int i = 0; i < children.Count; ++i)
			{
				var wrapper = children[i];
				if (TableViewCellsPanel.ColumnForCell(wrapper) is not null && wrapper.Visibility == Visibility.Visible)
				{
					offset = wrapper.Translation.X;
					break;
				}
			}
		}

		ElementCompositionPreview.SetIsTranslationEnabled(gutter, true);
		var translation = gutter.Translation;
		if (translation.X != offset || translation.Y != 0.0f || translation.Z != 0.0f)
		{
			gutter.Translation = new Vector3(offset, 0.0f, 0.0f);
		}
		if (Canvas.GetZIndex(gutter) != 2)
		{
			Canvas.SetZIndex(gutter, 2);
		}
	}

	// ----- Editing -----
	//
	// The row owns its cells, so the control delegates the display/editor swap here. The swap replaces
	// Child on the existing cell wrapper Border: keeping the wrapper means column width, visibility,
	// frozen pinning and grid lines keep applying while the cell is edited, with no layout re-plumbing.

	internal bool BeginCellEdit(TableViewColumn? column, object? dataItem)
	{
		if (column is null)
		{
			return false;
		}

		// An edit already open on this row is closed first. Committing it is the control's job, not
		// the row's; by this point the control has already ended it, so anything still open here is
		// stale visual state.
		EndCellEdit(TableViewEditAction.Cancel);

		var host = m_cellsHost;
		if (host is null)
		{
			return false;
		}

		Grid? cellWrapper = null;
		foreach (var child in host.Children)
		{
			if (child is Grid cell)
			{
				if (ReferenceEquals(cell.Tag as TableViewColumn, column))
				{
					cellWrapper = cell;
					break;
				}
			}
		}

		if (cellWrapper is null)
		{
			return false;
		}

		// A throw past here must restore the display element and drop the half-captured edit state, or
		// the cell is left blank and the row half-editing.
		var displayElement = TableViewCell.Child(cellWrapper);
		var focusRoot = XamlRoot;
		var restoreFocusState = FocusState.Unfocused;
		{
			var focused = focusRoot is not null ? FocusManager.GetFocusedElement(focusRoot) : null;
			if (TableViewDetails.IsWithinElementPopupAware(focused, cellWrapper, focusRoot))
			{
				var focusedElement = focused as UIElement;
				restoreFocusState = focusedElement is not null && focusedElement.FocusState != FocusState.Unfocused
					? focusedElement.FocusState : FocusState.Programmatic;
			}
		}
		bool unwindReleased = false;
		using var unwindOnThrow = Disposable.Create(() =>
		{
			if (unwindReleased)
			{
				return;
			}

			try
			{
				if (TableViewCell.TryGetExistingPeer(cellWrapper) is TableViewCellAutomationPeer peer)
				{
					peer.ResetEditName();
				}
			}
			catch (Exception)
			{
				TVDiag.LogRetailF("[TableView] Optional abandoned-cell name state could not be reset.");
			}
			try
			{
				if (!ReferenceEquals(TableViewCell.Child(cellWrapper), displayElement))
				{
					TableViewCell.Child(cellWrapper, displayElement);
					TableViewDetails.RefreshOwnedToolTip(cellWrapper);
				}
			}
			catch (Exception)
			{
				TVDiag.LogRetailF("[TableView] A failed BeginCellEdit could not restore the display cell.");
			}
			m_editingAutomationPeer = null;
			m_editingAutomationItem = null;
			m_editingAutomationValue = "";
			m_editingAutomationName = "";
			m_editingRowAutomationName = null;
			m_editingColumn = null;
			m_editingCellWrapper = null;
			m_editingElement = null;
			m_editingDisplayElement = null;

			// Not a layout pass (unlike AbandonCellEdit's callers), so focus the editor took or dropped
			// can go back to the cell.
			try
			{
				if (restoreFocusState != FocusState.Unfocused &&
					!TableViewDetails.IsWithinElementPopupAware(
						focusRoot is not null ? FocusManager.GetFocusedElement(focusRoot) : null, cellWrapper, focusRoot))
				{
					SetCellLevelInternal(true);
					if (!cellWrapper.Focus(restoreFocusState))
					{
						SetCellLevelInternal(false);
						Focus(restoreFocusState);
					}
				}
			}
			catch (Exception)
			{
				TVDiag.LogRetailF("[TableView] A failed BeginCellEdit could not restore focus to the cell.");
			}
		});

		var editingElement = column.GenerateEditingElement(dataItem);
		if (editingElement is null)
		{
			unwindReleased = true;
			return false;
		}

		// No local DataContext on the editing element, for the same reason the display cell sets none:
		// it inherits from the wrapper, which tracks the item across row recycle.
		// Observe only a provider a client already obtained. Creating peers here would turn every
		// ordinary edit into a UIA-tree allocation and could give the event a different identity.
		try
		{
			// Gate the snapshot work, not the edit: this function's return value starts the edit.
			var peer = AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged)
				? TableViewCell.TryGetExistingPeer(cellWrapper) as TableViewCellAutomationPeer
				: null;
			if (peer is not null && peer.GetPattern(PatternInterface.Value) is not null)
			{
				var value = peer.Value;
				var name = peer.ReadNameForEdit();
				var weakPeer = new WeakReference<TableViewCellAutomationPeer>(peer);
				m_editingAutomationItem = dataItem;
				m_editingAutomationValue = value;
				m_editingAutomationName = name;
				m_editingAutomationPeer = weakPeer;
			}
			m_editingRowAutomationName = null;
			if (AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
			{
				if (FrameworkElementAutomationPeer.FromElement(this) is { } rowPeer)
				{
					m_editingRowAutomationName = rowPeer.GetName();
				}
			}
		}
		catch (Exception)
		{
			m_editingAutomationPeer = null;
			m_editingAutomationItem = null;
			m_editingAutomationValue = "";
			m_editingAutomationName = "";
			m_editingRowAutomationName = null;
			TVDiag.LogRetailF("[TableView] Optional pre-edit UIA snapshot could not be captured.");
		}
		try
		{
			if (TableViewCell.TryGetExistingPeer(cellWrapper) is TableViewCellAutomationPeer peer)
			{
				peer.BeginEditName();
			}
		}
		catch (Exception)
		{
			TVDiag.LogRetailF("[TableView] Optional stable cell-name capture failed.");
		}
		m_editingDisplayElement = displayElement;
		m_pendingEditingCell = new WeakReference<UIElement>(cellWrapper);
		try
		{
			TableViewCell.Child(cellWrapper, editingElement);

			m_editingColumn = column;
			m_editingCellWrapper = cellWrapper;
			TableViewDetails.ClearOwnedToolTip(cellWrapper);
			m_editingElement = editingElement;

			unwindReleased = true;
			return true;
		}
		finally
		{
			m_pendingEditingCell = null;
		}
	}

	internal void EndCellEdit(TableViewEditAction action, StrongBox<int?>? displayFailure = null, StrongBox<bool>? applicationFault = null)
	{
		var cellWrapper = m_editingCellWrapper;
		var weakPeer = m_editingAutomationPeer;
		object? originalItem = null;
		string oldValue = "";
		string oldName = "";
		try
		{
			originalItem = m_editingAutomationItem;
			oldValue = m_editingAutomationValue;
			oldName = m_editingAutomationName;
		}
		catch (Exception)
		{
			originalItem = null;
			TVDiag.LogRetailF("[TableView] Optional edit UIA snapshot could not be retrieved.");
		}
		m_editingAutomationPeer = null;
		m_editingAutomationItem = null;
		m_editingAutomationValue = "";
		m_editingAutomationName = "";
		var oldRowName = m_editingRowAutomationName;
		m_editingRowAutomationName = null;
		if (cellWrapper is null)
		{
			m_editingColumn = null;
			m_editingElement = null;
			m_editingDisplayElement = null;
			return;
		}

		// Move focus off the editor before it leaves the tree. Dropping a focused element causes XAML
		// to fall back to whatever it can find, which can scroll the list; the row is the correct
		// landing spot and is where keyboard navigation expects focus to be.
		if (m_editingElement is { } editingElement)
		{
			bool editorHasFocus = false;
			var restoreState = FocusState.Programmatic;
			if (XamlRoot is { } root)
			{
				var focused = FocusManager.GetFocusedElement(root);
				editorHasFocus = TableViewDetails.IsWithinElementPopupAware(focused, editingElement, root);
				if (editorHasFocus)
				{
					// Read from the focused element: inside a dropdown the editor itself reports Unfocused.
					if (focused is UIElement focusedElement && focusedElement.FocusState != FocusState.Unfocused)
					{
						restoreState = focusedElement.FocusState;
					}
				}
			}

			TableViewDetails.CloseOpenDropDowns(editingElement);

			if (editorHasFocus)
			{
				// Restore focus to the cell, not row, and preserve Keyboard focus state so the focus
				// rectangle survives Enter-commit.
				// Closing edit returns to cell level, so re-arm cells before restoring focus.
				SetCellLevelInternal(true);

				if (!cellWrapper.Focus(restoreState))
				{
					SetCellLevelInternal(false);
					Focus(restoreState);
				}
			}
		}

		// Regenerate the display cell on commit rather than re-parenting the one that was parked when
		// the edit opened.
		//
		// The parked element still holds the OneWay binding it was created with, and a OneWay binding
		// only re-reads its source when that source raises PropertyChanged. For an item that does not
		// implement INotifyPropertyChanged the committed value would therefore never appear - the user
		// types, presses Enter, the item is updated, and the cell keeps showing the old text. A freshly
		// generated element evaluates its binding immediately against the current value, so the commit
		// is visible for plain POCOs too. This is what WPF's DataGrid does for the same reason.
		//
		// Only on Commit: a cancel wrote nothing, so the parked element is still correct and
		// regenerating it would be pure churn.
		UIElement? displayElement = m_editingDisplayElement;
		if (action == TableViewEditAction.Commit)
		{
			if (m_editingColumn is { } column)
			{
				bool factoryReturned = false;
				FrameworkElement? refreshed = null;
				try
				{
					refreshed = column.GenerateElement(DataContext);
					factoryReturned = true;
					if (refreshed is not null)
					{
						AttachCellContent(cellWrapper, refreshed);
						displayElement = null;
					}
				}
				catch (Exception ex)
				{
					if (displayFailure is not null)
					{
						displayFailure.Value = ex.HResult;
					}
					// An app-derived column's factory threw, or returned an element that already has a parent: both
					// are the app's. TableView's own columns build a new element.
					if (applicationFault is not null)
					{
						applicationFault.Value = TableViewDetails.IsAppDerivedColumn(column) &&
							(!factoryReturned || HasForeignParent(refreshed, cellWrapper));
					}
					// A column that throws while regenerating must not strand the row in edit mode;
					// fall back to the parked element, which is stale but present.
					displayElement = m_editingDisplayElement;
				}
			}
		}

		if (displayElement is not null)
		{
			TableViewCell.Child(cellWrapper, displayElement);
		}

		m_editingColumn = null;
		m_editingCellWrapper = null;
		m_editingElement = null;
		m_editingDisplayElement = null;

		// The bound value did not change, so only an explicit re-apply restores what the edit retracted.
		TableViewDetails.RefreshOwnedToolTip(cellWrapper);

		try
		{
			if (TableViewCell.TryGetExistingPeer(cellWrapper) is TableViewCellAutomationPeer peer)
			{
				peer.EndEditName();
			}
		}
		catch (Exception)
		{
			TVDiag.LogRetailF("[TableView] Optional final cell-name invalidation could not be prepared.");
		}

		try
		{
			if (action == TableViewEditAction.Commit && originalItem is not null &&
				TableView.SameInspectableIdentity(DataContext, originalItem))
			{
				if (weakPeer is not null && weakPeer.TryGetTarget(out var peer))
				{
					var newValue = peer.Value;
					var newName = peer.ReadNameForEdit();
					if (oldValue != newValue || oldName != newName)
					{
						// Publish after Ending; reject recycled cells and superseded values.
						if (DispatcherQueue is { } queue)
						{
							var weakThis = new WeakReference<TableViewRow>(this);
							var weakCell = new WeakReference<Grid>(cellWrapper);
							if (!queue.TryEnqueue(() =>
							{
								try
								{
									TableViewRow? row = null;
									TableViewCellAutomationPeer? currentPeer = null;
									Grid? cell = null;
									if (!weakThis.TryGetTarget(out row) || !weakPeer.TryGetTarget(out currentPeer) || !weakCell.TryGetTarget(out cell) ||
										!ReferenceEquals(currentPeer.Owner, cell) ||
										!ReferenceEquals(VisualTreeHelper.GetParent(cell) as Panel, row.GetCellsHostPanelInternal()) ||
										!TableView.SameInspectableIdentity(row.DataContext, originalItem))
									{
										return;
									}
									var peerImpl = currentPeer;
									if (peerImpl.GetRowIndexInternal() < 0 || peerImpl.Value != newValue || peerImpl.ReadNameForEdit() != newName)
									{
										return;
									}
									if (oldValue != newValue)
									{
										currentPeer.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty,
											oldValue, newValue);
									}
								}
								catch (Exception)
								{
									TVDiag.LogRetailF("[TableView] A committed cell's UIA notification could not be delivered.");
								}
							}))
							{
								TVDiag.LogRetailF("[TableView] Optional committed-cell UIA notification queue rejected delivery.");
							}
						}
						else
						{
							TVDiag.LogRetailF("[TableView] Optional committed-cell UIA notification has no dispatcher.");
						}
					}
				}
			}
		}
		catch (Exception)
		{
			TVDiag.LogRetailF("[TableView] Optional committed-cell UIA notification could not be prepared.");
		}

		// Other cells and the row Name can derive from the edited value (a template column bound to a
		// computed property). Their peers compute names live, but clients that cache need the change.
		try
		{
			if (action == TableViewEditAction.Commit &&
				AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
			{
				if (DispatcherQueue is { } queue)
				{
					var weakThis = new WeakReference<TableViewRow>(this);
					var weakCell = new WeakReference<UIElement>(cellWrapper);
					var item = DataContext;
					if (!queue.TryEnqueue(global::Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
					{
						try
						{
							if (!weakThis.TryGetTarget(out var row) || !TableView.SameInspectableIdentity(row.DataContext, item))
							{
								return;
							}
							weakCell.TryGetTarget(out var skipCell);
							row.RaiseCellAutomationNamesChangedInternal(skipCell);
							if (oldRowName is not null)
							{
								if (FrameworkElementAutomationPeer.FromElement(row) is { } rowPeer)
								{
									var newRowName = rowPeer.GetName();
									if (newRowName != oldRowName)
									{
										rowPeer.RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty,
											oldRowName, newRowName);
									}
								}
							}
						}
						catch (Exception)
						{
							TVDiag.LogRetailF("[TableView] Optional committed-row name notification could not be delivered.");
						}
					}))
					{
						TVDiag.LogRetailF("[TableView] Optional committed-row name notification queue rejected delivery.");
					}
				}
			}
		}
		catch (Exception)
		{
			TVDiag.LogRetailF("[TableView] Optional committed-row name notification could not be prepared.");
		}
	}

	internal void AbandonCellEdit()
	{
		try
		{
			if (m_editingCellWrapper is { } cell)
			{
				if (TableViewCell.TryGetExistingPeer(cell) is TableViewCellAutomationPeer peer)
				{
					peer.ResetEditName();
				}
			}
		}
		catch (Exception)
		{
			TVDiag.LogRetailF("[TableView] Optional abandoned-cell name state could not be reset.");
		}
		m_editingAutomationPeer = null;
		m_editingAutomationItem = null;
		m_editingAutomationValue = "";
		m_editingAutomationName = "";
		// Restores the display child, but deliberately does NOT touch focus. Callers run inside a layout
		// pass, where moving focus re-enters the framework and trips the re-entrancy guard. Replacing
		// the child does not - and it must happen, or the row keeps showing a TextBox after the edit
		// closed, and over a different item once recycled.
		var cellWrapper = m_editingCellWrapper;
		if (cellWrapper is not null)
		{
			TableViewCell.Child(cellWrapper, m_editingDisplayElement);
		}

		m_editingColumn = null;
		m_editingCellWrapper = null;
		m_editingElement = null;
		m_editingDisplayElement = null;

		// The cell is a display cell again; restore the tooltip the edit retracted.
		TableViewDetails.RefreshOwnedToolTip(cellWrapper);
	}

	private void ResetPressState()
	{
		m_lastPressTimestamp = 0;
		m_lastPressPosition = default;
		m_lastPressColumn = null;
		m_lastPressItem = null;
	}

	private void ProcessContextRequest(
		TableViewDetails.ContextMenuTarget target,
		ContextRequestedEventArgs args)
	{
		var owner = GetOwningTableView();
		if (owner is null)
		{
			return;
		}

		if (owner.ProcessContextMenuRequest(target, args))
		{
			args.Handled = true;
		}
	}

	// Cell requests (pointer, keyboard, cell peer). Requests on the host's empty area carry no column
	// and go on to the template root as row requests.
	private void OnCellsHostContextRequested(ContextRequestedEventArgs args)
	{
		if (args.Handled)
		{
			return;
		}

		if (ResolveContextMenuTarget(args) is { } target && target.Column is not null)
		{
			ProcessContextRequest(target, args);
		}
	}

	// Row requests from anywhere under the template root but outside a cell: padding, the chevron,
	// the cells host's empty area. A ContextFlyout on the template root itself is shown by the
	// framework before this handler runs.
	private void OnTemplateRootContextRequested(ContextRequestedEventArgs args)
	{
		if (args.Handled)
		{
			return;
		}

		// A cell request the cells host left Unhandled (nothing to show) keeps bubbling.
		if (ResolveContextMenuTarget(args) is { } target && target.Column is null)
		{
			ProcessContextRequest(target, args);
		}
	}

	// Row requests raised on the row itself: Shift+F10 / Menu key while the row (not a cell) has
	// focus. They start above the template root, so neither handler above sees them; without this
	// the row-template menu is skipped and the table fallback opens. Requests that bubbled up from
	// inside the row were already offered to the handlers above and keep bubbling.
	private void OnRowContextRequested(ContextRequestedEventArgs args)
	{
		var source = args.OriginalSource as TableViewRow;
		if (args.Handled || source is null || !ReferenceEquals(source, this))
		{
			return;
		}

		if (ResolveContextMenuTarget(args) is { } target && target.Column is null)
		{
			ProcessContextRequest(target, args);
		}
	}

	internal bool ShowContextMenuFromPeer()
	{
		var owner = GetOwningTableView();
		var target = MakeRowContextMenuTarget();
		if (owner is null || target is null)
		{
			return false;
		}

		return owner.ProcessContextMenuRequest(target.Value, null);
	}

	private TableViewDetails.ContextMenuTarget? MakeRowContextMenuTarget()
	{
		var owner = GetOwningTableView();
		var host = m_cellsHost;
		if (owner is null || host is null)
		{
			return null;
		}

		TableViewDetails.ContextMenuTarget target = new();
		target.Row = this;
		target.Item = owner.UnwrapEditingDataItem(DataContext);
		target.Anchor = this;
		target.ScopeRoot = host;
		return target;
	}

	private TableViewDetails.ContextMenuTarget? ResolveContextMenuTarget(
		ContextRequestedEventArgs args)
	{
		return ResolveContextMenuTarget(args.OriginalSource as DependencyObject);
	}

	private TableViewDetails.ContextMenuTarget? ResolveContextMenuTarget(
		DependencyObject? source)
	{
		var maybeTarget = MakeRowContextMenuTarget();
		if (maybeTarget is null)
		{
			return null;
		}
		var target = maybeTarget.Value;

		TableViewRow self = this;
		if (!TableViewDetails.IsOwnRequestSource(source, self))
		{
			return null;
		}

		// The column comes from the cell wrapper the request started in, never from a Tag on the
		// cell's content and never from where the pointer is: the chevron overlays the first cell but
		// belongs to the row.
		var cell = FindOwnCellInternal(source, false /* requireExact */);
		var column = cell is not null ? GetCellOwningColumn(cell) : null;
		if (column is not null && !GetOwningTableView()!.IsContextMenuColumn(column))
		{
			column = null;
		}

		var anchor = column is not null ? TableViewCellsPanel.CellForColumn(target.ScopeRoot, column) : null;
		if (anchor is not null)
		{
			target.Column = column;
			target.Anchor = anchor;
		}

		return target;
	}

	// Pointer entry point for editing, and the only place a pointer establishes the current cell.
	//
	// Lives on the row because the row owns its cells: resolving which cell a press landed on is a
	// question only the row can answer cheaply. The control keeps the edit state machine, so this
	// handler translates a gesture into SetCurrentCell / BeginEdit and nothing more. Mirrors WPF,
	// where DataGridCell handles the gesture and calls DataGrid.BeginEdit.
	//
	// PointerPressed with click counting, not DoubleTapped: marking a press handled suppresses XAML's
	// gesture recognizer entirely, and a row that participates in selection must mark it handled. A
	// DoubleTapped handler would work today and silently break when selection lands.
	internal void OnPointerPressedForEditing(
		object sender,
		PointerRoutedEventArgs args)
	{
		var owner = GetOwningTableView();
		if (owner is null)
		{
			return;
		}

		var ownerImpl = owner;

		// Editing is opt-in and read-only by default, so a read-only table pays nothing beyond this.
		if (ownerImpl.IsReadOnly)
		{
			return;
		}

		// A chevron press is a toggle, not a lead-cell press; otherwise two quick toggles would read as
		// a double-click and open an editor. Leaf gutters (no toggle) fall through.
		if (IsExpandable && IsWithinExpanderGutter(args.OriginalSource))
		{
			return;
		}

		var pointerPoint = args.GetCurrentPoint(this);
		if (pointerPoint is null)
		{
			return;
		}

		// Mouse/pen: primary button only - a right-click opens a context menu and must not begin an
		// edit. Touch reports no pressed button, so it is admitted on device type instead.
		var deviceType = args.Pointer.PointerDeviceType;
		if (deviceType != PointerDeviceType.Touch &&
			!pointerPoint.Properties.IsLeftButtonPressed)
		{
			return;
		}

		var column = ResolvePressedColumn(args.OriginalSource, args.GetCurrentPoint(null).Position);
		if (column is null)
		{
			return;
		}

		var item = ownerImpl.UnwrapEditingDataItem(DataContext);
		if (item is null)
		{
			return;
		}

		var timestamp = pointerPoint.Timestamp;
		var position = pointerPoint.Position;

		double slop = TableView.DoubleClickSlop(this);

		// Device type is part of the repeat identity so a mouse click following a pen tap is not read
		// as one double-click. Pointer ID deliberately is NOT: every touch contact gets a fresh id, so
		// requiring equality would mean the second tap never matches and touch could never begin an
		// edit at all.
		bool isRepeatPress =
			ReferenceEquals(m_lastPressColumn, column) &&
			TableView.SameInspectableIdentity(m_lastPressItem, item) &&
			m_lastPressDeviceType == deviceType &&
			timestamp >= m_lastPressTimestamp &&
			(timestamp - m_lastPressTimestamp) <= TableView.DoubleClickIntervalMicroseconds() &&
			Math.Abs(position.X - m_lastPressPosition.X) <= slop &&
			Math.Abs(position.Y - m_lastPressPosition.Y) <= slop;

		// A press inside the cell already being edited belongs to the editor: it is the user placing
		// the caret, and must not be read as a navigation move or a fresh edit.
		bool pressInsideOpenEdit =
			ownerImpl.IsEditing &&
			ReferenceEquals(m_editingColumn, column) &&
			m_editingElement is not null;

		if (!pressInsideOpenEdit)
		{
			// Move the current cell even when the edit is refused (read-only, or BeginningEdit cancels):
			// the user pointed here, so a later F2 must not edit a different cell.
			//
			// This is also what makes keyboard editing reachable at all - pointer focus lands on the
			// row, not a tagged cell, so without this the current column stays null.
			ownerImpl.SetCurrentCell(item, column);

			if (isRepeatPress)
			{
				ownerImpl.BeginEdit(item, column);

				// Consume the press so a third click starts a fresh first press rather than
				// re-entering begin-edit on a cell that is already open.
				m_lastPressTimestamp = 0;
				m_lastPressColumn = null;
				m_lastPressItem = null;
				return;
			}
		}

		m_lastPressTimestamp = timestamp;
		m_lastPressPosition = position;
		m_lastPressColumn = column;
		m_lastPressItem = item;
		m_lastPressDeviceType = deviceType;
	}

	// Which of this row's cells a press landed on. Walks up from OriginalSource to the cell wrapper
	// Grid, whose Tag carries the owning column (set in RebuildCells). Once the row itself has focus
	// a press can arrive with the row as OriginalSource and no tagged cell on the chain, so fall back
	// to hit-testing this row's subtree.
	private TableViewColumn? ResolvePressedColumn(
		object? originalSource,
		Point hostPoint)
	{
		var source = originalSource as DependencyObject;
		if (!TableViewDetails.IsOwnRequestSource(source, this))
		{
			return null;
		}

		// Only a direct child of our cells host can identify a column. Application content can have
		// its own Tag, including one naming another column in this table.
		if (FindOwnCellInternal(source, false /* requireExact */) is { } ownCell)
		{
			return GetCellOwningColumn(ownCell);
		}

		foreach (var hit in VisualTreeHelper.FindElementsInHostCoordinates(hostPoint, this))
		{
			if (!TableViewDetails.IsOwnRequestSource(hit, this))
			{
				return null;
			}

			if (FindOwnCellInternal(hit, false /* requireExact */) is { } cell)
			{
				return GetCellOwningColumn(cell);
			}
		}

		return null;
	}
}

internal static partial class TableViewDetails
{
	internal static partial void OnCellToolTipValueAppliedInternal(FrameworkElement cell)
	{
		for (DependencyObject? node = VisualTreeHelper.GetParent(cell); node is not null; node = VisualTreeHelper.GetParent(node))
		{
			if (node is TableViewRow row)
			{
				row.OnCellToolTipChangedInternal(cell);
				return;
			}
		}
	}
}
