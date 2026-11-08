// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView_ContextMenu.cpp, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using static Microsoft.UI.Xaml.Controls.Tabular.TableViewAutomationHelpers;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableView
{
	internal bool IsContextMenuColumn(TableViewColumn? column)
	{
		if (column is null || column.Visibility != Visibility.Visible)
		{
			return false;
		}

		var owner = column.GetOwningTableView();
		if (owner is null || owner != this)
		{
			return false;
		}

		var columns = owner.Columns;
		return columns is not null && columns.IndexOf(column) >= 0;
	}

	private bool IsContextMenuTargetCurrent(
		TableViewDetails.ContextMenuTarget target)
	{
		if (target.Anchor is null || target.ScopeRoot is null || !target.Anchor.IsLoaded)
		{
			return false;
		}

		DependencyObject? node = target.ScopeRoot;
		TableView? owner = null;
		while (node is not null && owner is null)
		{
			owner = node as TableView;
			if (owner is null)
			{
				node = VisualTreeHelper.GetParent(node);
			}
		}

		if (owner is null || owner != this)
		{
			return false;
		}

		if (target.Column is not null && !IsContextMenuColumn(target.Column))
		{
			return false;
		}

		if (target.Kind == TableViewDetails.ContextMenuTargetKind.Body)
		{
			if (target.Row is null || target.Row.GetOwningTableView() != owner)
			{
				return false;
			}

			return target.Row.IsContextMenuTargetCurrent(target);
		}

		if (target.Kind != TableViewDetails.ContextMenuTargetKind.Header)
		{
			return false;
		}

		var showsColumnHeaders =
			((uint)owner.HeadersVisibility & (uint)TableViewHeadersVisibility.Column) != 0;
		if (target.Row is not null || target.Item is not null || target.Column is null ||
			!ReferenceEquals(target.ScopeRoot, m_headerHost) ||
			!showsColumnHeaders)
		{
			return false;
		}

		return TableViewDetails.IsLiveCellAnchor(target.Anchor, target.ScopeRoot, target.Column);
	}

	private TableViewDetails.ContextMenuTarget? ResolveHeaderContextMenuTarget(
		ContextRequestedEventArgs args)
	{
		var host = m_headerHost;
		var source = args.OriginalSource as DependencyObject;
		if (host is null || !TableViewDetails.IsOwnRequestSource(source, host))
		{
			return null;
		}

		var node = source;
		while (node is not null && node != host)
		{
			var parent = VisualTreeHelper.GetParent(node);
			if (parent == host)
			{
				var cell = node as FrameworkElement;
				var column = cell is not null ? TableViewCellsPanel.ColumnForCell(cell) : null;
				if (column is null)
				{
					return null;
				}

				TableViewDetails.ContextMenuTarget target = new();
				target.Kind = TableViewDetails.ContextMenuTargetKind.Header;
				target.Column = column;
				target.Anchor = cell;
				target.ScopeRoot = host;
				if (!IsContextMenuTargetCurrent(target))
				{
					return null;
				}

				return target;
			}

			node = parent;
		}

		return null;
	}

	internal void SuppressContextRequestWhileEditing(ContextRequestedEventArgs args)
	{
		if (!args.Handled && AreContextMenusSuppressed())
		{
			args.Handled = true;
		}
	}

	private void OnHeaderContextRequested(ContextRequestedEventArgs args)
	{
		if (args.Handled)
		{
			return;
		}

		var target = ResolveHeaderContextMenuTarget(args);
		if (target is null)
		{
			return;
		}

		if (ProcessContextMenuRequest(target.Value, args))
		{
			args.Handled = true;
		}
	}

	// namespace {
	// The row's own XAML menu: the first app ContextFlyout from just above the cell (or the cells
	// host, for a row target) up to and including the row. Covers a row style setter and row
	// template elements, anchored on the element that owns it, as the framework would show it.
	private static TableViewDetails.ContextMenuCandidate FindRowContextFlyout(TableViewDetails.ContextMenuTarget target)
	{
		if (target.Row is null)
		{
			return default;
		}

		DependencyObject row = target.Row;
		DependencyObject? node = target.Column is not null && target.Anchor is not null
			? VisualTreeHelper.GetParent(target.Anchor)
			: target.ScopeRoot;
		while (node is not null)
		{
			if (node is FrameworkElement element && HasAppContextFlyout(element))
			{
				return new() { Flyout = element.ContextFlyout, Anchor = element };
			}

			if (node == row)
			{
				break;
			}

			node = VisualTreeHelper.GetParent(node);
		}

		return default;
	}

	// First app-supplied ContextFlyout inside a cell or header wrapper (CellTemplate, HeaderTemplate,
	// generated element). Text controls' built-in flyouts are ignored.
	private static TableViewDetails.ContextMenuCandidate FindContentContextFlyout(FrameworkElement? wrapper)
	{
		if (wrapper is null)
		{
			return default;
		}

		// Depth-first in visual order so the outermost element wins. A nested items host's own
		// ContextFlyout counts, its items' do not; a nested TableView is skipped entirely.
		List<DependencyObject> stack = new();
		void PushChildren(DependencyObject parent)
		{
			var count = VisualTreeHelper.GetChildrenCount(parent);
			for (var i = count - 1; i >= 0; --i)
			{
				stack.Add(VisualTreeHelper.GetChild(parent, i));
			}
		}

		PushChildren(wrapper);
		while (stack.Count > 0)
		{
			var node = stack[stack.Count - 1];
			stack.RemoveAt(stack.Count - 1);

			if (node is not UIElement element || element.Visibility != Visibility.Visible ||
				node is TableView)
			{
				continue;
			}

			if (HasAppContextFlyout(element))
			{
				if (element is FrameworkElement anchor)
				{
					return new() { Flyout = element.ContextFlyout, Anchor = anchor };
				}
			}

			if (node is ItemsControl || node is ItemsRepeater)
			{
				continue;
			}

			PushChildren(node);
		}

		return default;
	}
	// } // namespace

	private TableViewDetails.ContextMenuCandidate ResolveContextFlyout(
		TableViewDetails.ContextMenuTarget target,
		bool includeTableFlyout)
	{
		// A ContextFlyout the app placed inside the cell or header (template or style) wins over the
		// column/row properties, so keyboard and wrapper-padding requests match what a pointer request
		// on the content already shows natively.
		if (target.Column is not null)
		{
			var content = FindContentContextFlyout(target.Anchor);
			if ((bool)content)
			{
				return content;
			}
		}

		if (target.Kind == TableViewDetails.ContextMenuTargetKind.Header)
		{
			if (target.Column!.HeaderContextFlyout is { } flyout)
			{
				return new() { Flyout = flyout, Anchor = target.Anchor };
			}
		}
		else
		{
			if (target.Column is not null)
			{
				if (target.Column.CellContextFlyout is { } flyout)
				{
					return new() { Flyout = flyout, Anchor = target.Anchor };
				}
			}

			var rowMenu = FindRowContextFlyout(target);
			if ((bool)rowMenu)
			{
				return rowMenu;
			}
		}

		// Pointer requests keep native bubbling/placement. Non-pointer requests need the cell bounds,
		// while retaining the table as Target and as the source of the menu's DataContext.
		if (includeTableFlyout)
		{
			return new() { Flyout = ContextFlyout, Anchor = this };
		}

		return default;
	}

	private void ShowContextFlyout(
		TableViewDetails.ContextMenuTarget target,
		TableViewDetails.ContextMenuCandidate candidate,
		ContextRequestedEventArgs? args)
	{
		FlyoutShowOptions options = new();
		options.Placement = FlyoutPlacementMode.Auto;
		// Match ShowAt(element): an options ShowMode of Auto opens as Standard and writes that back
		// to the flyout, dropping a Transient mode for this and every later open.
		options.ShowMode = candidate.Flyout!.ShowMode;

		Point point = default;
		if (args is not null && args.TryGetPosition(candidate.Anchor, out point))
		{
			options.Position = point;
		}
		else if (target.Column is not null && target.Anchor is not null && candidate.Anchor != target.Anchor)
		{
			// A keyboard or automation request has no pointer position. A menu owned by a larger
			// element (the row or table) still opens at the focused cell, not across the whole owner;
			// the owner stays the flyout's Target.
			var cell = target.Anchor;
			var bounds = cell.TransformToVisual(candidate.Anchor).TransformBounds(
				new Rect(0.0, 0.0, (float)cell.ActualWidth, (float)cell.ActualHeight));
			// TODO Uno: FlyoutShowOptions.ExclusionRect is missing.
			// options.ExclusionRect(bounds);
			options.Position = new Point(bounds.X, bounds.Y + bounds.Height);
		}

		candidate.Flyout.ShowAt(candidate.Anchor, options);
	}

	// IsContextMenuTargetCurrent plus, for body targets, an unchanged source and no edit in progress.
	private bool IsContextMenuRequestCurrent(
		TableViewDetails.ContextMenuTarget target,
		object? sourceSnapshot)
	{
		return IsContextMenuTargetCurrent(target) &&
			(target.Kind != TableViewDetails.ContextMenuTargetKind.Body ||
				(ReferenceEquals(ItemsSource, sourceSnapshot) && !AreContextMenusSuppressed()));
	}

	internal bool FocusContextMenuTargetFromPointer(TableViewDetails.ContextMenuTarget target)
	{
		if (m_isProcessingContextMenu || AreContextMenusSuppressed() || !IsContextMenuTargetCurrent(target))
		{
			return false;
		}

		m_isProcessingContextMenu = true;
		try
		{
			var sourceSnapshot = ItemsSource;

			return FocusContextMenuTarget(target, sourceSnapshot) && IsContextMenuRequestCurrent(target, sourceSnapshot);
		}
		finally
		{
			m_isProcessingContextMenu = false;
		}
	}

	private bool FocusContextMenuTarget(
		TableViewDetails.ContextMenuTarget target,
		object? sourceSnapshot)
	{
		DependencyObject? FocusedElement()
		{
			var root = XamlRoot;
			return root is not null
				? FocusManager.GetFocusedElement(root) as DependencyObject
				: null;
		}

		static bool IsFocusWithin(DependencyObject? focused, DependencyObject element)
		{
			return focused is not null &&
				(focused == element || SharedHelpers.IsAncestor(focused, element, false /* checkVisibility */));
		}

		// Focus the exact target (cell, row or header) so focus restore and Narrator use it. Body
		// targets go through the two-level focus helpers so the cursor level follows.
		if (target.Kind == TableViewDetails.ContextMenuTargetKind.Header)
		{
			if (!IsFocusWithin(FocusedElement(), target.Anchor!))
			{
				// Header cells are tab stops; a request on the header's content keeps focus where it is.
				target.Anchor!.Focus(FocusState.Programmatic);
			}
			return true;
		}

		if (target.Column is not null)
		{
			SetCurrentCell(target.Item, target.Column);
		}

		if (!IsContextMenuRequestCurrent(target, sourceSnapshot))
		{
			return false;
		}

		DependencyObject rowObject = target.Row!;
		var focused = FocusedElement();

		if (target.Column is null)
		{
			if (focused != rowObject)
			{
				FocusRowContainerInternal(target.Row!, FocusState.Programmatic);
			}
			return true;
		}

		// Entering the row from outside lands on the row first. App code reacting to that focus
		// change (removing the item, say) recycles the row back to row level, which would leave a
		// direct cell focus aimed at a cell that is no longer focusable.
		if (!IsFocusWithin(focused, rowObject))
		{
			FocusRowContainerInternal(target.Row!, FocusState.Programmatic);
			if (!IsContextMenuRequestCurrent(target, sourceSnapshot))
			{
				return false;
			}
			focused = FocusedElement();
		}

		if (!IsFocusWithin(focused, target.Anchor!))
		{
			var rowImpl = target.Row!;
			var column = rowImpl.GetVisibleCellIndexInternal(target.Anchor);
			if (column >= 0)
			{
				// The exact cell; an inactive one leaves focus on its row.
				rowImpl.FocusVisibleCellInternal(column, FocusState.Programmatic);
			}
		}

		return true;
	}

	// True when handled (menu shown, or suppressed: editing, nested or stale target); false lets the
	// request bubble to the native ContextFlyout chain.
	internal bool ProcessContextMenuRequest(
		TableViewDetails.ContextMenuTarget target,
		ContextRequestedEventArgs? args)
	{
		// args is null for a row automation peer request, which has no routed event.
		if (args is not null && args.Handled)
		{
			return false;
		}

		// Defensive: the framework fail-fasts a synchronous nested ShowContextMenu before it reaches
		// here, so this only guards against a future path that re-enters while focus or menu app code runs.
		if (m_isProcessingContextMenu)
		{
			return true;
		}

		// No TableView menu shows while a cell is editing: the request is handled with nothing shown,
		// so the row and table menus cannot open through the native chain either. An editor with its
		// own menu (a TextBox) has already shown it and handled the request.
		if (AreContextMenusSuppressed())
		{
			return true;
		}

		if (!IsContextMenuTargetCurrent(target))
		{
			return true;
		}

		// Moving focus runs app code. If that makes the target stale, the request stays handled only
		// when the TableView would have shown a menu, so it cannot fall through to the native chain
		// at a different element.
		bool includeTableFlyout = args is null || !args.TryGetPosition(target.Anchor, out _);
		bool staleResult = (bool)ResolveContextFlyout(target, includeTableFlyout);

		m_isProcessingContextMenu = true;
		try
		{
			var sourceSnapshot = ItemsSource;

			if (!FocusContextMenuTarget(target, sourceSnapshot) || !IsContextMenuRequestCurrent(target, sourceSnapshot))
			{
				return staleResult;
			}

			// Resolved again once focus has settled: app code run by the focus move may have replaced a
			// column's menu or the cell's content, and the menu shown is the one in place now.
			var candidate = ResolveContextFlyout(target, includeTableFlyout);
			if (!(bool)candidate)
			{
				return false;
			}

			ShowContextFlyout(target, candidate, args);
			return true;
		}
		finally
		{
			m_isProcessingContextMenu = false;
		}
	}
}

internal static partial class TableViewDetails
{
	internal static partial bool IsLiveCellAnchor(
		FrameworkElement? anchor,
		Panel? scopeRoot,
		TableViewColumn? column)
	{
		return anchor is not null && scopeRoot is not null && column is not null &&
			anchor.Visibility == Visibility.Visible &&
			VisualTreeHelper.GetParent(anchor) == scopeRoot &&
			TableViewCellsPanel.CellForColumn(scopeRoot, column) == anchor;
	}

	internal static partial bool IsOwnRequestSource(
		DependencyObject? source,
		DependencyObject? scope)
	{
		for (var node = source; node is not null; node = VisualTreeHelper.GetParent(node))
		{
			if (node == scope)
			{
				return true;
			}

			if (node is TableView || node is TableViewRow)
			{
				return false;
			}
		}

		return false;
	}

	internal static partial void SuppressContextFlyoutWhileEditing(
		FlyoutBase flyout,
		TableView? owner)
	{
		if (owner is not null && owner.AreContextMenusSuppressed())
		{
			// This is still Opening: a Closing veto must not expose a menu over an uncommitted edit.
			TypedEventHandler<FlyoutBase, FlyoutBaseClosingEventArgs> onClosing = (_, args) => args.Cancel = false;
			flyout.Closing += onClosing;
			using var closingRevoker = Disposable.Create(() => flyout.Closing -= onClosing);
			flyout.Hide();
		}
	}
}
