// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewCellAutomationPeer.cpp, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;

using static Microsoft.UI.Xaml.Controls.Tabular.TableViewAutomationHelpers;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewCellAutomationPeer
{
	private static FrameworkElement CellForPublicConstructor(
		FrameworkElement? cell,
		TableViewRow? row,
		TableViewColumn? column,
		int columnIndex)
	{
		if (cell is null || row is null || column is null || columnIndex < 0)
		{
			// hresult_invalid_argument
			throw new ArgumentException();
		}

		// A row not yet owned by a TableView has no column set to check against.
		if (row.GetOwningTableView() is { } table)
		{
			if (column.GetOwningTableView() != table ||
				(uint)columnIndex >= (uint)table.Columns.Count)
			{
				// hresult_invalid_argument
				throw new ArgumentException();
			}
		}

		return cell;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TableViewCellAutomationPeer"/> class.
	/// </summary>
	/// <param name="cell">The realized cell element.</param>
	/// <param name="row">The row that hosts the cell.</param>
	/// <param name="column">The column that owns the cell.</param>
	/// <param name="columnIndex">The visible column index of the cell.</param>
	public TableViewCellAutomationPeer(
		FrameworkElement cell,
		TableViewRow? row,
		TableViewColumn? column,
		int columnIndex)
		: this(new TrustedOwnerTag(), CellForPublicConstructor(cell, row, column, columnIndex), row, column, columnIndex)
	{
	}

	internal TableViewCellAutomationPeer(
		TrustedOwnerTag trustedOwnerTag,
		FrameworkElement cell,
		TableViewRow? row,
		TableViewColumn? column,
		int columnIndex)
		: base(cell)
	{
		m_columnIndex = columnIndex;

		// The cell owner supplies bounds; weak refs avoid extending row/column lifetimes.
		if (row is not null)
		{
			m_row = new WeakReference<TableViewRow>(row);
		}
		if (column is not null)
		{
			m_column = new WeakReference<TableViewColumn>(column);
		}
		if (row is not null)
		{
			if (row.GetOwningTableView() is { } tableView)
			{
				TrackRowItem(row, tableView);
			}
		}
		// Primes m_lastOwningTable / m_lastKnownRowIndex for IsVirtualized(). It has to happen while
		// the row is still in the tree: once the row is recycled the index is unresolvable, and a
		// client may not query VirtualizedItem until after that point.
		_ = GetRowIndex();
	}

	// TODO Uno: ~TableViewCellAutomationPeer() calls ResetAutomationContentViewCache(). There is no
	// deterministic destruction here and a finalizer must not touch DependencyObjects off the UI thread.
	// The registered property callbacks only hold the peer weakly, so they turn into no-ops once it is
	// collected. Orphaned callbacks unregister themselves on their next firing; one that never fires
	// again stays registered until its element dies.

	protected override object? GetPatternCore(PatternInterface patternInterface)
	{
		// GridItem + TableItem are structural and always meaningful for realized cells.
		if (patternInterface == PatternInterface.GridItem ||
			patternInterface == PatternInterface.TableItem)
		{
			return this;
		}

		// Offered only where SetValue can honour it: the cell must be editable and its column must
		// produce a TextBox editor. Advertising it elsewhere tells assistive technology it can set a
		// value, then fails after opening an edit.
		if (patternInterface == PatternInterface.Value && SupportsValuePattern())
		{
			if (m_row.Get() is { } row && GetTrackedTableForRow(row) is not null)
			{
				return this;
			}
		}

		if (patternInterface == PatternInterface.VirtualizedItem && IsVirtualized())
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore()
	{
		return "TableViewCell";
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		// DataItem lets Narrator read the composed cell name instead of a generic container.
		return AutomationControlType.DataItem;
	}

	protected override string GetLocalizedControlTypeCore()
	{
		// A host app may not merge the control's PRI; degrade to the framework default rather than
		// throwing into UIA.
		var localized = TryGetLocalizedString(ResourceAccessor.SR_TableViewCellLocalizedControlType);
		if (!string.IsNullOrEmpty(localized))
		{
			return localized;
		}

		return base.GetLocalizedControlTypeCore();
	}

	// UIA focusability is not Tab reachability: row-level cells still need SetFocus and
	// IsKeyboardFocusable to succeed when enabled, visible and actionable.
	protected override bool IsKeyboardFocusableCore()
	{
		var cell = Owner as FrameworkElement;
		if (cell is null || cell.Visibility != Visibility.Visible || IsVirtualized())
		{
			return false;
		}

		// The cell wrapper is a Grid, so IsEnabled lives on the owning row, not on the cell.
		var row = m_row.Get();
		if (row is not null && GetTrackedTableForRow(row) is null)
		{
			return false;
		}
		return row is null || (row.IsEnabled && row.IsCellActionableInternal(cell));
	}

	protected override void SetFocusCore()
	{
		var row = m_row.Get();
		if (row is null || GetTrackedTableForRow(row) is null)
		{
			ThrowElementNotAvailable();
		}

		var cell = row is not null ? GetRealizedCellFromRow(row) : null;

		if (row is not null && cell is not null)
		{
			// Only an actionable cell takes focus; never leave the row armed with no focusable level.
			var rowImpl = row;
			int column = rowImpl.GetVisibleCellIndexInternal(cell);
			if (column < 0 || !rowImpl.IsCellActionableInternal(cell))
			{
				// UIA_E_INVALIDOPERATION
				throw new InvalidOperationException();
			}
			// The exact cell or nothing: no fallback to the row, and a refused cell restores the level.
			bool wasCellLevel = rowImpl.IsCellLevelInternal();
			rowImpl.SetCellLevelInternal(true);
			if (!cell.Focus(FocusState.Programmatic) || cell.FocusState == FocusState.Unfocused)
			{
				rowImpl.SetCellLevelInternal(wasCellLevel);
				// UIA_E_INVALIDOPERATION
				throw new InvalidOperationException();
			}
			return;
		}

		base.SetFocusCore();
	}

	protected override void ShowContextMenuCore()
	{
		// Same tracking rule as SetFocusCore: a recycled cell must not raise a request for another item.
		var row = m_row.Get();
		var cell = row is not null && GetTrackedTableForRow(row) is not null ? GetRealizedCellFromRow(row) : null;
		if (cell is null)
		{
			ThrowElementNotAvailable();
		}

		if (cell == Owner)
		{
			base.ShowContextMenuCore();
		}
		else if (FrameworkElementAutomationPeer.CreatePeerForElement(cell) is { } peer)
		{
			peer.ShowContextMenu();
		}
	}

	protected override string GetNameCore()
	{
		var row = m_row.Get();
		var tableView = row is not null ? GetTrackedTableForRow(row) : null;
		if (row is not null && tableView is null)
		{
			ThrowElementNotAvailable();
		}

		UpdateNameItem(row is not null && tableView is not null
			? row.DataContext : null);
		if (m_editName is not null)
		{
			return m_editName;
		}
		if (GetCachedHasInteractiveCellContent(Owner as FrameworkElement))
		{
			// The control names itself; naming the cell after it too would read that name twice.
			var interactiveName = GetColumnHeaderText(tableView);
			m_lastName = interactiveName;
			return interactiveName;
		}
		if (row is not null)
		{
			if (row.GetDisplayElementForAutomation(Owner) is { } display)
			{
				var displayName = ReadDisplayName(tableView, display as FrameworkElement);
				m_lastName = displayName;
				return displayName;
			}
		}
		var name = ReadDisplayName(tableView);
		m_lastName = name;
		return name;
	}

	protected override IList<AutomationPeer> GetChildrenCore()
	{
		if (Owner is FrameworkElement cell)
		{
			PrepareAutomationContentView(cell);
		}

		return base.GetChildrenCore();
	}

	internal string ReadNameForEdit()
	{
		var row = m_row.Get();
		var tableView = row is not null ? GetTrackedTableForRow(row) : null;
		if (row is not null && tableView is null)
		{
			ThrowElementNotAvailable();
		}

		UpdateNameItem(row is not null && tableView is not null
			? row.DataContext : null);
		var name = ReadDisplayName(tableView);
		m_lastName = name;
		return name;
	}

	internal void UpdateNameItem(object? item)
	{
		if (!TableView.SameInspectableIdentity(m_nameItem, item))
		{
			ResetEditName();
			m_nameItem = item;
			m_item.Track(item);
			m_lastKnownRowIndex = -1;
			m_trackedItemOccurrence = -1;
			ResetAutomationContentViewCache();
		}
	}

	internal void BeginEditName()
	{
		++m_nameGeneration;
		m_nameLayoutUpdatedRevoker.Disposable = null;
		m_editName = m_lastName;
	}

	internal void ResetEditName()
	{
		++m_nameGeneration;
		m_nameLayoutUpdatedRevoker.Disposable = null;
		m_editName = null;
		m_lastName = null;
		m_nameItem = null;
	}

	internal void EndEditName()
	{
		m_nameLayoutUpdatedRevoker.Disposable = null;
		var generation = m_nameGeneration;
		var weakThis = new WeakReference<TableViewCellAutomationPeer>(this);
		var cell = Owner as FrameworkElement;
		if (cell is null)
		{
			m_editName = null;
			return;
		}

		// Released by whichever of the three paths below resolves first. Owned by the backstop's
		// lambda, so the subscription cannot outlive one frame.
		// TODO Uno: the C++ shared_ptr revokes when the last owner drops it; the failure path below
		// disposes it explicitly instead.
		SerialDisposable unloadRevoker = new();

		// TODO Uno: wil::scope_exit cleanupOnFailure.
		bool cleanupOnFailure = true;
		try
		{
			// LayoutUpdated precedes the framework's automatic-property pass. Queue
			// release from that event, leaving the held old Name intact for the pass.
			EventHandler<object> onLayoutUpdated = (_, _) =>
			{
				if (weakThis.TryGetTarget(out var peer) && peer.m_nameGeneration == generation)
				{
					peer.m_nameLayoutUpdatedRevoker.Disposable = null;
					try
					{
						peer.QueueFinalName(generation);
					}
					catch
					{
						peer.m_editName = null;
						TVDiag.LogRetailF("[TableView] Optional post-layout name publication could not be queued.");
					}
				}
			};
			cell.LayoutUpdated += onLayoutUpdated;
			m_nameLayoutUpdatedRevoker.Disposable = Disposable.Create(() => cell.LayoutUpdated -= onLayoutUpdated);

			// An unloaded, collapsed, or hidden-column cell never sees another LayoutUpdated, so the pinned
			// pre-edit name would otherwise be returned by GetNameCore forever.
			RoutedEventHandler onUnloaded = (_, _) =>
			{
				if (weakThis.TryGetTarget(out var peer) && peer.m_nameGeneration == generation)
				{
					peer.m_nameLayoutUpdatedRevoker.Disposable = null;
					peer.m_editName = null;
					try
					{
						peer.InvalidatePeer();
					}
					catch
					{
						TVDiag.LogRetailF("[TableView] Optional cell-name invalidation on unload failed.");
					}
				}
			};
			cell.Unloaded += onUnloaded;
			unloadRevoker.Disposable = Disposable.Create(() => cell.Unloaded -= onUnloaded);

			cell.InvalidateMeasure();

			// Backstop: LayoutUpdated is not guaranteed to run for this cell at all. An armed layout
			// revoker at end of frame means it did not, so release rather than stay pinned.
			var queue = DispatcherQueue;
			if (queue is null || !queue.TryEnqueue(DispatcherQueuePriority.Low,
				() =>
				{
					unloadRevoker.Dispose();
					if (weakThis.TryGetTarget(out var peer) &&
						peer.m_nameGeneration == generation && peer.m_nameLayoutUpdatedRevoker.Disposable is not null)
					{
						peer.m_nameLayoutUpdatedRevoker.Disposable = null;
						peer.m_editName = null;
						try
						{
							peer.InvalidatePeer();
						}
						catch
						{
							TVDiag.LogRetailF("[TableView] Optional cell-name backstop invalidation failed.");
						}
					}
				}))
			{
				// Without the backstop the one-frame bound cannot be honoured, so do not pin at all.
				TVDiag.LogRetailF("[TableView] Optional cell-name release backstop could not be queued.");
				return;
			}

			cleanupOnFailure = false;
		}
		finally
		{
			if (cleanupOnFailure)
			{
				m_nameLayoutUpdatedRevoker.Disposable = null;
				m_editName = null;
				unloadRevoker.Dispose();
			}
		}
	}

	private void QueueFinalName(ulong generation)
	{
		var weakThis = new WeakReference<TableViewCellAutomationPeer>(this);
		var queue = DispatcherQueue;
		if (queue is not null && queue.TryEnqueue(DispatcherQueuePriority.Low, () =>
		{
			if (weakThis.TryGetTarget(out var peer) && peer.m_nameGeneration == generation)
			{
				try
				{
					var row = peer.m_row.Get();
					var cell = peer.Owner;
					if (row is null || row.GetOwningTableView() is null ||
						!TableView.SameInspectableIdentity(row.DataContext, peer.m_nameItem) ||
						!ReferenceEquals(VisualTreeHelper.GetParent(cell) as Panel, row.GetCellsHostPanelInternal()))
					{
						peer.ResetEditName();
						return;
					}
					peer.m_editName = null;
					peer.InvalidatePeer();
				}
				catch
				{
					TVDiag.LogRetailF("[TableView] Optional final cell-name invalidation failed.");
				}
			}
		}))
		{
			return;
		}
		m_editName = null;
		TVDiag.LogRetailF("[TableView] Optional final cell-name invalidation could not be queued.");
	}

	// True when the cell shows nothing: only layout containers and empty text.
	private static bool ShowsNoCellContent(DependencyObject element, uint depth)
	{
		var fe = element as FrameworkElement;
		if (fe is null || fe.Visibility != Visibility.Visible)
		{
			return true;
		}
		if (fe is TextBlock textBlock)
		{
			return string.IsNullOrEmpty(textBlock.Text);
		}
		if (fe is TextBox textBox)
		{
			return string.IsNullOrEmpty(textBox.Text);
		}
		if (depth == 0 || !(fe is Panel || fe is Border || fe is ContentPresenter))
		{
			return false;
		}
		var count = VisualTreeHelper.GetChildrenCount(fe);
		for (int i = 0; i < count; ++i)
		{
			if (!ShowsNoCellContent(VisualTreeHelper.GetChild(fe, i), depth - 1))
			{
				return false;
			}
		}
		return true;
	}

	private string ReadDisplayName(TableView? table, FrameworkElement? display = null)
	{
		uint remaining = 32;
		var valueText = display is not null
			? GetCellContentName(display, true, 8, ref remaining)
			: GetCellDisplayText(Owner as FrameworkElement);

		// Borrowing the header would read an empty cell like one whose value is the header text.
		if (string.IsNullOrEmpty(valueText))
		{
			// An unnamed visual (a ProgressBar, say) is not blank; name it by its column instead.
			var content = display ?? GetCellContentElement(Owner as FrameworkElement);
			if (content is not null && !ShowsNoCellContent(content, 8))
			{
				return GetColumnHeaderText(table);
			}
			valueText = LocalizedOrFallbackForTableViewAutomation(ResourceAccessor.SR_TableViewCellBlankName, "blank");
		}

		// A reachable header item already gives the column context.
		if (IsColumnHeaderReachable(table))
		{
			return valueText;
		}

		var headerText = GetColumnHeaderText(table);
		if (string.IsNullOrEmpty(headerText))
		{
			return valueText;
		}

		return FormatLocalizedOrFallback(ResourceAccessor.SR_TableViewCellNameFormat, "%1!s!, %2!s!", headerText, valueText, ", ");
	}

	private string GetColumnHeaderText(TableView? table)
	{
		return ResolveColumnAccessibleName(table, m_column.Get());
	}

	private bool IsColumnHeaderReachable(TableView? table)
	{
		var column = m_column.Get();
		return table is not null && IsVisibleColumn(column) &&
			column!.GetOwningTableView() == table &&
			table.AreColumnHeadersShownInternal();
	}

	internal string? TakeLastNameInternal()
	{
		// An edit pins the pre-edit name; EndEditName publishes the final one.
		if (m_editName is not null)
		{
			return null;
		}

		var lastName = m_lastName;
		m_lastName = null;
		return lastName;
	}

	private string GetCellValueText()
	{
		var row = m_row.Get();
		if (row is not null && GetTrackedTableForRow(row) is null)
		{
			ThrowElementNotAvailable();
		}

		var content = GetCellContentElement(Owner as FrameworkElement);
		// ValuePattern describes editable text, not the accessibility label naming that text.
		if (content is TextBlock text)
		{
			return text.Text;
		}
		if (content is TextBox editor)
		{
			return editor.Text;
		}
		return GetCellDisplayText(Owner as FrameworkElement);
	}

	private void PrepareAutomationContentView(FrameworkElement cell)
	{
		var content = GetCellAutomationContent(cell);
		if (content is null)
		{
			ResetAutomationContentViewCache();
			return;
		}

		if (GetCachedHasInteractiveCellContent(cell))
		{
			SetAccessibilityViewIfNeeded(content, AccessibilityView.Content);
			SetInteractiveCellContentAccessibilityViewContent(content);
		}
		else
		{
			SetCellContentAccessibilityViewRaw(content);
		}
	}

	private bool GetCachedHasInteractiveCellContent(FrameworkElement? cell)
	{
		var content = GetCellAutomationContent(cell);
		if (content is null)
		{
			ResetAutomationContentViewCache();
			return false;
		}

		var item = GetCurrentAutomationContentItem();
		if (IsAutomationContentCacheValid(content, item))
		{
			return m_hasInteractiveAutomationContent!.Value;
		}

		ResetAutomationContentViewCache();
		m_automationContent = new WeakReference<FrameworkElement>(content);
		m_automationContentItem.Track(item);

		uint remaining = 64;
		m_hasInteractiveAutomationContent =
			HasInteractiveCellContentAndRegisterCallbacks(content, 8, ref remaining);
		return m_hasInteractiveAutomationContent.Value;
	}

	private object? GetCurrentAutomationContentItem()
	{
		var row = m_row.Get();
		if (row is null)
		{
			return null;
		}

		if (row.GetOwningTableView() is { } tableView)
		{
			return tableView.UnwrapEditingDataItem(row.DataContext);
		}

		return row.DataContext;
	}

	private bool IsAutomationContentCacheValid(
		FrameworkElement content,
		object? item)
	{
		var cachedContent = m_automationContent.Get();
		return m_hasInteractiveAutomationContent.HasValue &&
			cachedContent is not null &&
			TableView.SameInspectableIdentity(cachedContent, content) &&
			CachedAutomationContentItemMatches(item);
	}

	private bool CachedAutomationContentItemMatches(object? item)
	{
		return item is not null ? m_automationContentItem.SameIdentityAs(item) : !m_automationContentItem.IsTracking();
	}

	// TODO Uno: C++ takes uint32_t* remainingBudget = nullptr; the null case is this overload.
#pragma warning disable IDE0051 // C++ default remainingBudget = nullptr is never taken upstream; kept for 1:1 parity
	private bool HasInteractiveCellContentAndRegisterCallbacks(
		UIElement? element,
		uint depthBudget = 8)
	{
		uint localBudget = 64;
		return HasInteractiveCellContentAndRegisterCallbacks(element, depthBudget, ref localBudget);
	}
#pragma warning restore IDE0051

	private bool HasInteractiveCellContentAndRegisterCallbacks(
		UIElement? element,
		uint depthBudget,
		ref uint remainingBudget)
	{
		if (element is null || depthBudget == 0 || remainingBudget == 0)
		{
			return false;
		}

		RegisterAutomationContentPropertyCallbacks(element);
		if (element.Visibility != Visibility.Visible)
		{
			return false;
		}

		--remainingBudget;
		if (IsFocusableCellContent(element))
		{
			return true;
		}

		const int maxChildrenPerLevel = 32;
		var childCount = VisualTreeHelper.GetChildrenCount(element);
		for (int i = 0; i < childCount && i < maxChildrenPerLevel && remainingBudget > 0; ++i)
		{
			if (VisualTreeHelper.GetChild(element, i) is UIElement child)
			{
				if (HasInteractiveCellContentAndRegisterCallbacks(child, depthBudget - 1, ref remainingBudget))
				{
					return true;
				}
			}
		}

		return false;
	}

	private void RegisterAutomationContentPropertyCallbacks(UIElement element)
	{
		var weakThis = new WeakReference<TableViewCellAutomationPeer>(this);

		void Observe(DependencyObject target, DependencyProperty property)
		{
			// TODO Uno: with no destructor, a callback orphaned by a collected peer unregisters itself.
			long token = 0;
			DependencyPropertyChangedCallback callback =
				(sender, _) =>
				{
					if (weakThis.TryGetTarget(out var peer))
					{
						peer.InvalidateAutomationContentViewCache();
					}
					else
					{
						sender.UnregisterPropertyChangedCallback(property, token);
					}
				};

			token = target.RegisterPropertyChangedCallback(property, callback);
			m_automationContentPropertyChangedRevokers.Add(new AutomationContentPropertyChangedRevoker(
				target,
				property,
				token));
		}

		Observe(element, UIElement.VisibilityProperty);

		if (element is Control control)
		{
			Observe(control, Control.IsEnabledProperty);
			Observe(control, UIElement.IsTabStopProperty);
		}

		// The three properties above describe elements that already exist. They say nothing
		// about the subtree gaining or losing elements, which a TableViewTemplateColumn does
		// whenever an app-supplied CellTemplate swaps what sits inside an otherwise stable
		// presenter. Without this, a stale "not interactive" verdict would survive the arrival
		// of a focusable control and SetCellContentAccessibilityViewRaw would then hide it.
		// XAML raises no subtree-changed event, so observe the properties that drive the swap.
		if (element is ContentPresenter presenter)
		{
			Observe(presenter, ContentPresenter.ContentProperty);
			Observe(presenter, ContentPresenter.ContentTemplateProperty);
		}

		if (element is ContentControl contentControl)
		{
			Observe(contentControl, ContentControl.ContentProperty);
			Observe(contentControl, ContentControl.ContentTemplateProperty);
		}

		if (element is ItemsControl itemsControl)
		{
			Observe(itemsControl, ItemsControl.ItemsSourceProperty);
		}
	}

	private void InvalidateAutomationContentViewCache()
	{
		m_hasInteractiveAutomationContent = null;
	}

	private void ResetAutomationContentViewCache()
	{
		m_hasInteractiveAutomationContent = null;
		m_automationContent = null;
		m_automationContentItem.Track(null);
		// TODO Uno: vector::clear() runs each revoker's destructor; revoke explicitly here.
		foreach (var revoker in m_automationContentPropertyChangedRevokers)
		{
			revoker.Revoke();
		}
		m_automationContentPropertyChangedRevokers.Clear();
	}

	protected override string GetHelpTextCore()
	{
		var row = m_row.Get();
		if (row is not null && GetTrackedTableForRow(row) is null)
		{
			ThrowElementNotAvailable();
		}

		var helpText = base.GetHelpTextCore();
		if (string.IsNullOrEmpty(helpText))
		{
			return helpText;
		}

		var record = TableViewDetails.GetRecord(Owner as FrameworkElement);

		// Resolved here, not at attach, where the cell's binding may not have produced a value yet.
		// Gated on the record so text the app set is never dropped.
		if (record is not null && !string.IsNullOrEmpty(record.PublishedHelpText) &&
			helpText == record.PublishedHelpText &&
			helpText == GetCellValueText())
		{
			return string.Empty;
		}

		return helpText;
	}

	private int GetRowIndex()
	{
		if (m_row.Get() is { } row)
		{
			return GetRowIndex(row, GetTrackedTableForRow(row));
		}

		return -1;
	}

	private int GetRowIndex(TableViewRow row, TableView? tableView)
	{
		if (tableView is not null)
		{
			m_lastOwningTable = new WeakReference<TableView>(tableView);

			// Use the row peer's coordinate basis and avoid a visual-tree walk per realized cell.
			if (tableView.GetRowsRepeaterInternal() is { } tableRepeater)
			{
				var trackedRowIndex = tableRepeater.GetElementIndex(row);
				if (trackedRowIndex >= 0)
				{
					m_lastKnownRowIndex = trackedRowIndex;
					return trackedRowIndex;
				}
			}
		}

		// TableView exposes no public row-index API, so fall back to the hosting ItemsRepeater for
		// a row whose owner is not resolvable yet.
		var rowItem = row.DataContext;
		if (!m_item.SameIdentityAs(rowItem))
		{
			return -1;
		}

		DependencyObject? parent = VisualTreeHelper.GetParent(row);
		while (parent is not null)
		{
			if (parent is ItemsRepeater repeater)
			{
				var rowIndex = repeater.GetElementIndex(row);
				if (rowIndex >= 0)
				{
					m_lastKnownRowIndex = rowIndex;
					return rowIndex;
				}
			}
			parent = VisualTreeHelper.GetParent(parent);
		}

		return -1;
	}

	private bool IsVirtualized()
	{
		if (GetRowIndex() >= 0)
		{
			return false;
		}

		var tableView = m_lastOwningTable.Get();
		if (tableView is null || m_lastKnownRowIndex < 0)
		{
			return false;
		}

		var tableImpl = tableView;
		var rowIndex = GetTrackedItemIndex(tableView);
		if (rowIndex < 0 || rowIndex >= tableImpl.GetRowCountInternal())
		{
			return false;
		}

		var repeater = tableImpl.GetRowsRepeaterInternal();
		bool isVirtualized = repeater is not null && repeater.TryGetElement(rowIndex) is null;
		if (isVirtualized)
		{
			m_lastKnownRowIndex = rowIndex;
		}
		return isVirtualized;
	}

	void IVirtualizedItemProvider.Realize()
	{
		if (!IsVirtualized())
		{
			return;
		}

		// IVirtualizedItemProvider::Realize is synchronous: on return the client immediately re-queries
		// this provider and expects the element realized, with VirtualizedItem no longer supported.
		// Deferring to the dispatcher hands the client a still-virtualized provider.
		RealizeCore();
	}

	private void RealizeCore()
	{
		var tableView = m_lastOwningTable.Get();
		var rowIndex = tableView is not null ? GetTrackedItemIndex(tableView) : -1;
		if (tableView is null || rowIndex < 0)
		{
			ThrowElementNotAvailable();
		}

		var tableImpl = tableView;
		if (rowIndex >= tableImpl.GetRowCountInternal())
		{
			ThrowElementNotAvailable();
		}

		var repeater = tableImpl.GetRowsRepeaterInternal();
		if (repeater is null)
		{
			ThrowElementNotAvailable();
		}

		UIElement? element = null;
		try
		{
			element = repeater.TryGetElement(rowIndex);
			if (element is null)
			{
				element = repeater.GetOrCreateElement(rowIndex);
			}
		}
		catch
		{
			ThrowElementNotAvailable();
		}

		if (element is TableViewRow row)
		{
			// The peer is bound to its own cell element; if the repeater realized the item into another
			// row, this provider cannot describe it, so fail rather than report a realized element.
			if (!IsTrackedRow(row, tableView) || row != m_row.Get() || !IsOwnerCurrentCellOfRow(row))
			{
				ThrowElementNotAvailable();
			}

			if (GetRealizedCellFromRow(row) is { } cell)
			{
				if (cell is FrameworkElement cellElement)
				{
					cellElement.StartBringIntoView();
					return;
				}
			}
		}

		if (element is FrameworkElement frameworkElement)
		{
			frameworkElement.StartBringIntoView();
		}
	}

	private TableView? GetTrackedTableForRow(TableViewRow? row)
	{
		if (row is null)
		{
			return null;
		}

		var tableView = row.GetOwningTableView();
		if (tableView is not null && IsTrackedRow(row, tableView))
		{
			m_lastOwningTable = new WeakReference<TableView>(tableView);
			return tableView;
		}

		return null;
	}

	private object? GetTrackedItem()
	{
		return m_item.Resolve();
	}

	private int GetTrackedItemIndex(TableView? tableView)
	{
		if (tableView is null || !m_item.IsTracking())
		{
			return -1;
		}

		var tableImpl = tableView;
		var repeater = tableImpl.GetRowsRepeaterInternal();
		var view = repeater?.ItemsSourceView;
		if (view is null)
		{
			return -1;
		}

		int count = view.Count;
		if (m_lastKnownRowIndex >= 0 && m_lastKnownRowIndex < count)
		{
			var lastKnownCandidate = tableImpl.UnwrapEditingDataItem(view.GetAt(m_lastKnownRowIndex));
			if (m_item.SameIdentityAs(lastKnownCandidate))
			{
				return m_lastKnownRowIndex;
			}
		}

		int occurrence = 0;
		for (int index = 0; index < count; ++index)
		{
			var candidate = tableImpl.UnwrapEditingDataItem(view.GetAt(index));
			if (m_item.SameIdentityAs(candidate))
			{
				if (m_trackedItemOccurrence < 0 || occurrence == m_trackedItemOccurrence)
				{
					return index;
				}
				++occurrence;
			}
		}

		return -1;
	}

	private int GetItemOccurrenceAtIndex(
		TableView? tableView,
		object? item,
		int targetIndex)
	{
		if (tableView is null || item is null || targetIndex < 0)
		{
			return -1;
		}

		var tableImpl = tableView;
		var repeater = tableImpl.GetRowsRepeaterInternal();
		var view = repeater?.ItemsSourceView;
		if (view is null || targetIndex >= view.Count)
		{
			return -1;
		}

		int occurrence = 0;
		for (int index = 0; index <= targetIndex; ++index)
		{
			var candidate = tableImpl.UnwrapEditingDataItem(view.GetAt(index));
			if (candidate is not null && TableView.SameInspectableIdentity(candidate, item))
			{
				if (index == targetIndex)
				{
					return occurrence;
				}
				++occurrence;
			}
		}

		return -1;
	}

	private bool IsTrackedRow(TableViewRow? row, TableView? tableView)
	{
		if (row is null || tableView is null)
		{
			return false;
		}

		var rowItem = tableView.UnwrapEditingDataItem(row.DataContext);
		if (rowItem is null)
		{
			return false;
		}

		if (!m_item.IsTracking())
		{
			TrackRowItem(row, tableView);
			return true;
		}

		if (!m_item.SameIdentityAs(rowItem))
		{
			return false;
		}

		if (tableView.GetRowsRepeaterInternal() is { } repeater)
		{
			var rowIndex = repeater.GetElementIndex(row);
			if (rowIndex >= 0)
			{
				var occurrence = GetItemOccurrenceAtIndex(tableView, rowItem, rowIndex);
				if (m_trackedItemOccurrence >= 0 && occurrence != m_trackedItemOccurrence)
				{
					return false;
				}

				TrackRowItem(row, tableView);
			}
		}

		return true;
	}

	private void TrackRowItem(TableViewRow? row, TableView? tableView)
	{
		if (row is not null && tableView is not null)
		{
			if (tableView.UnwrapEditingDataItem(row.DataContext) is { } item)
			{
				var previousOccurrence = m_trackedItemOccurrence;
				var sameItem = m_item.SameIdentityAs(item);
				var trackedItemOccurrence = -1;
				if (tableView.GetRowsRepeaterInternal() is { } repeater)
				{
					var rowIndex = repeater.GetElementIndex(row);
					if (rowIndex >= 0)
					{
						m_lastKnownRowIndex = rowIndex;
						trackedItemOccurrence = GetItemOccurrenceAtIndex(tableView, item, rowIndex);
					}
				}
				if (!sameItem || previousOccurrence != trackedItemOccurrence)
				{
					ResetAutomationContentViewCache();
				}
				m_item.Track(item);
				m_trackedItemOccurrence = trackedItemOccurrence;
			}
		}
	}

	private UIElement? GetRealizedCellFromRow(TableViewRow? row)
	{
		if (row is null)
		{
			return null;
		}

		var rowImpl = row;

		var cellsHost = rowImpl.GetCellsHostPanelInternal();
		if (cellsHost is null)
		{
			return null;
		}

		if (m_column.Get() is { } column)
		{
			foreach (var child in cellsHost.Children)
			{
				var cell = child as UIElement;
				if (cell is not null && rowImpl.GetCellOwningColumn(cell) == column)
				{
					return cell;
				}
			}
		}

		return rowImpl.GetVisibleCellInternal(m_columnIndex);
	}

	/// <summary>
	/// Gets the ordinal number of the row that contains the cell.
	/// </summary>
	public int Row
	{
		get
		{
			var row = m_row.Get();
			var tableView = row is not null ? GetTrackedTableForRow(row) : null;
			if (tableView is null || !IsOwnerCurrentCellOfRow(row!))
			{
				ThrowElementNotAvailable();
			}

			var rowIndex = GetRowIndex(row!, tableView);
			if (rowIndex >= 0)
			{
				return rowIndex;
			}

			// Between recycle and re-prepare a live row has no repeater index; only an item that has left
			// the source makes this provider stale.
			var itemIndex = GetTrackedItemIndex(tableView);
			if (itemIndex >= 0)
			{
				return itemIndex;
			}
			ThrowElementNotAvailable();
			return -1;
		}
	}

	internal int GetRowIndexInternal()
	{
		return GetRowIndex();
	}

	/// <summary>
	/// Gets the ordinal number of the visible column that contains the cell.
	/// </summary>
	public int Column
	{
		get
		{
			var row = m_row.Get();
			if (row is null || GetTrackedTableForRow(row) is null || !IsOwnerCurrentCellOfRow(row))
			{
				ThrowElementNotAvailable();
			}

			// Computed live, mirroring Row(): a cached index goes stale as soon as a column is hidden or
			// shown underneath a client holding this provider.
			var rowImpl = row;
			if (rowImpl.GetCellsHostPanelInternal() is { } cellsHost)
			{
				var cell = Owner;
				var cellChildren = cellsHost.Children;
				var count = cellChildren.Count;
				int visibleColumnIndex = 0;

				// Same walk and predicate as TableViewAutomationPeer::VisibleColumnToChildIndex and
				// the row peer's GetChildrenCore, so all three agree on the coordinate.
				for (int i = 0; i < count; ++i)
				{
					var child = cellChildren[i] as UIElement;
					if (child is null || !IsVisibleColumn(rowImpl.GetCellOwningColumn(child)))
					{
						continue;
					}

					if (child == cell)
					{
						return visibleColumnIndex;
					}
					++visibleColumnIndex;
				}

				// Still hosted, but its column is collapsed: the cell has no visible-column coordinate.
				ThrowElementNotAvailable();
			}

			if (m_columnIndex < 0)
			{
				ThrowElementNotAvailable();
			}
			return m_columnIndex;
		}
	}

	// A same-row rebuild (column reorder or removal, CellTemplate change) replaces the cells while the
	// row and its item stay live, so a tracked row alone does not prove this cell is current.
	private bool IsOwnerCurrentCellOfRow(TableViewRow row)
	{
		var rowImpl = row;
		var cellsHost = rowImpl.GetCellsHostPanelInternal();
		if (cellsHost is null)
		{
			return true;
		}

		var cell = Owner as UIElement;
		return cell is not null &&
			(VisualTreeHelper.GetParent(cell) as Panel) == cellsHost &&
			rowImpl.GetCellOwningColumn(cell) == m_column.Get();
	}

	/// <summary>
	/// Gets the number of rows spanned by the cell.
	/// </summary>
	public int RowSpan => 1;

	/// <summary>
	/// Gets the number of columns spanned by the cell.
	/// </summary>
	public int ColumnSpan => 1;

	/// <summary>
	/// Gets the UI Automation provider of the owning <see cref="TableView"/>.
	/// </summary>
	public IRawElementProviderSimple? ContainingGrid
	{
		get
		{
			// The containing grid is the owning TableView's automation peer.
			if (m_row.Get() is { } row)
			{
				if (GetTrackedTableForRow(row) is { } owner)
				{
					if (FrameworkElementAutomationPeer.CreatePeerForElement(owner) is { } peer)
					{
						return ProviderFromPeer(peer);
					}
				}
			}

			return null;
		}
	}

	/// <summary>
	/// Retrieves the row header items associated with the cell.
	/// </summary>
	/// <returns>An empty array, as TableView has no row headers.</returns>
	public IRawElementProviderSimple[] GetRowHeaderItems()
	{
		// TableView has no row-header concept.
		return Array.Empty<IRawElementProviderSimple>();
	}

	/// <summary>
	/// Retrieves the column header item associated with the cell.
	/// </summary>
	/// <returns>An array holding the column header provider, or an empty array.</returns>
	public IRawElementProviderSimple[] GetColumnHeaderItems()
	{
		// Return the corresponding column header provider using the same peer construction path
		// as the table-level header enumeration.
		List<IRawElementProviderSimple> headers = new();

		if (m_column.Get() is { } column)
		{
			if (m_row.Get() is { } row)
			{
				// Hidden headers are named into the cell instead, so reporting them would repeat the header.
				if (GetTrackedTableForRow(row) is { } owner && IsColumnHeaderReachable(owner))
				{
					// Resolve the visual's peer even when the app supplies a custom table peer.
					var headerPeer = GetRealizedColumnHeaderPeer(owner, column);

					// A provider array must not contain nulls - UIA marshals every element. An empty
					// array correctly reports "this cell has no reachable column header".
					if (headerPeer is not null)
					{
						if (ProviderFromPeer(headerPeer) is { } provider)
						{
							headers.Add(provider);
						}
					}
				}
			}
		}

		return headers.ToArray();
	}

	// ----- IValueProvider -----

	/// <summary>
	/// Gets the displayed text of the cell.
	/// </summary>
	public string Value => GetCellValueText();

	/// <summary>
	/// Gets a value that indicates whether the cell value cannot be set.
	/// </summary>
	public bool IsReadOnly => !SupportsValuePattern();

	/// <summary>
	/// Sets the cell value by driving the TableView edit lifecycle.
	/// </summary>
	/// <param name="value">The text to write to the cell.</param>
	public void SetValue(string value)
	{
		const int E_FAIL = unchecked((int)0x80004005);

		if (IsReadOnly)
		{
			// throw winrt::hresult_error(E_NOTIMPL, L"This cell is read-only.");
			throw new NotImplementedException("This cell is read-only.");
		}

		var row = m_row.Get();
		var column = m_column.Get();
		if (row is null || column is null || GetTrackedTableForRow(row) is null)
		{
			// throw winrt::hresult_error(E_FAIL, L"The cell is no longer realized.");
			throw new Exception("The cell is no longer realized.") { HResult = E_FAIL };
		}

		var owner = column.GetOwningTableView();
		if (owner is null)
		{
			// throw winrt::hresult_error(E_FAIL, L"The cell has no owning TableView.");
			throw new Exception("The cell has no owning TableView.") { HResult = E_FAIL };
		}

		var item = GetTrackedItem();
		if (item is null)
		{
			ThrowElementNotAvailable();
		}
		var ownerImpl = owner;

		// Drive the real edit lifecycle rather than writing the source directly, so a BeginningEdit
		// handler can still veto and CellEditEnding/validation still run - a programmatic set must not
		// be able to do what a user cannot.
		if (!ownerImpl.BeginEdit(item, column))
		{
			// throw winrt::hresult_error(E_FAIL, L"The cell could not be opened for editing.");
			throw new Exception("The cell could not be opened for editing.") { HResult = E_FAIL };
		}

		bool wrote = false;
		if (ownerImpl.CurrentEditingElement() is { } editingElement)
		{
			if (editingElement is TextBox textBox)
			{
				textBox.Text = value;
				wrote = true;
			}
		}

		if (!wrote)
		{
			// Nothing we can type into - do not leave the editor open.
			ownerImpl.CancelEdit();
			// throw winrt::hresult_error(E_NOTIMPL, L"This cell's editor does not support setting a text value.");
			throw new NotImplementedException("This cell's editor does not support setting a text value.");
		}

		if (!ownerImpl.CommitEdit())
		{
			// Vetoed, rejected by validation, or waiting on a deferral; the edit stays open and the
			// caller must not be told the value was applied.
			// throw winrt::hresult_error(E_FAIL, L"The value was not accepted.");
			throw new Exception("The value was not accepted.") { HResult = E_FAIL };
		}
	}

	private bool SupportsValuePattern()
	{
		var column = m_column.Get();
		if (column is null || column.IsReadOnly)
		{
			return false;
		}

		var owner = column.GetOwningTableView();
		if (owner is null || owner.IsReadOnly)
		{
			return false;
		}

		// SetValue writes text, so the column must produce a TextBox. A text column no longer implies
		// one: CellEditingTemplate lives on the base column now, so an app can replace any column's
		// editor with an arbitrary template. Advertising the pattern then tells assistive technology it
		// can set a value, and the attempt fails only after an edit has been opened on screen.
		var textColumn = column as TableViewTextColumn;
		return textColumn is not null && column.CellEditingTemplate is null;
	}
}
