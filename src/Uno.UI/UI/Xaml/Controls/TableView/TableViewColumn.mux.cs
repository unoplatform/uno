// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewColumn.cpp, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.Boxes;
using Uno.UI.Helpers.WinUI;

using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewColumn
{
	// namespace {
	private static uint s_nextAutomationIdentity = 1;

	private static bool TryFocusEditor(FrameworkElement editor)
	{
		if (editor.Focus(FocusState.Programmatic))
		{
			return true;
		}
		if (FocusManager.FindFirstFocusableElement(editor) is { } target)
		{
			if (target is Control control)
			{
				return control.Focus(FocusState.Programmatic);
			}
			if (target is UIElement element)
			{
				return element.Focus(FocusState.Programmatic);
			}
		}
		return false;
	}

	// Realizing and focusing a CellEditingTemplate's content runs the app's code, so a throw there leaves tagged as the
	// app's; the rest of the editing hooks is TableView's own code.
	private static T RunEditorContent<T>(bool appContent, Func<T> call)
	{
		try
		{
			return call();
		}
		catch (Exception ex)
		{
			if (appContent)
			{
				TableViewTip.RethrowAppFault(ex, TableViewTip.AppFaultKind.Factory);
			}
			throw;
		}
	}

	private static void RunEditorContent(bool appContent, Action call)
	{
		try
		{
			call();
		}
		catch (Exception ex)
		{
			if (appContent)
			{
				TableViewTip.RethrowAppFault(ex, TableViewTip.AppFaultKind.Factory);
			}
			throw;
		}
	}
	// } // namespace

	// Required for derivation: the overridable cell factories below are only reachable if a
	// consumer can actually construct a derived column.
	/// <summary>
	/// Initializes a new instance of the <see cref="TableViewColumn"/> class.
	/// </summary>
	public TableViewColumn()
	{
		// Defaults let initial ActualWidth equal Width without clamping.
	}

	/// <summary>
	/// Generates the display-only visual root for a cell.
	/// </summary>
	/// <param name="dataItem">The row data item.</param>
	/// <returns>The display visual for the cell, or null when the column has no display surface.</returns>
	public FrameworkElement? GenerateElement(object? dataItem)
	{
		// Forward to the overridable display-cell factory.
		return GenerateElementCore(dataItem);
	}

	/// <summary>
	/// Overridable cell factory. Custom columns return the display visual for a cell.
	/// The returned element must bind reactively to its inherited DataContext (the row data item) and
	/// must not set a local DataContext: rows are recycled and their DataContext is updated in place,
	/// so cells refresh through inheritance/bindings. dataItem is provided for initial setup only;
	/// baking it in as static content (or a local DataContext) will show stale data after recycle.
	/// </summary>
	/// <param name="dataItem">The row data item, for initial setup only.</param>
	/// <returns>The display visual for the cell, or null when the column has no display surface.</returns>
	protected virtual FrameworkElement? GenerateElementCore(object? dataItem)
	{
		// Base columns have no display surface.
		return null;
	}

	internal FrameworkElement? GenerateEditingElement(object? dataItem)
	{
		// Forward to the overridable editing-cell factory.
		return GenerateEditingElementCore(dataItem);
	}

	internal virtual FrameworkElement? GenerateEditingElementCore(object? dataItem)
	{
		// A CellEditingTemplate is the column-agnostic way to supply an editor, so the base honours it
		// for every column type. Without one a base column declines the edit: returning null is how a
		// column says "this cell cannot be edited", rather than opening an empty editor over it.
		var editingTemplate = CellEditingTemplate;
		if (editingTemplate is null)
		{
			return null;
		}

		ContentPresenter presenter = new();
		presenter.HorizontalAlignment = HorizontalAlignment.Stretch;
		presenter.VerticalAlignment = VerticalAlignment.Stretch;
		presenter.ContentTemplate = editingTemplate;

		// Unlike the display cell - whose Content is bound by TableViewRow::RebuildCells so it survives
		// recycle - the editing presenter lives only for the duration of one edit on one item, so its
		// Content is bound here against its own inherited DataContext.
		Binding contentBinding = new();
		contentBinding.Path = new PropertyPath("DataContext");
		contentBinding.RelativeSource = new RelativeSource();
		contentBinding.RelativeSource.Mode = RelativeSourceMode.Self;
		BindingOperations.SetBinding(presenter, ContentPresenter.ContentProperty, contentBinding);

		return presenter;
	}

	internal object? PrepareCellForEdit(FrameworkElement? editingElement, RoutedEventArgs? editingEventArgs) =>
		PrepareCellForEditCore(editingElement, editingEventArgs);

	internal virtual object? PrepareCellForEditCore(FrameworkElement? editingElement, RoutedEventArgs? editingEventArgs)
	{
		if (editingElement is null)
		{
			return null;
		}

		// A CellEditingTemplate's content is the app's (see GenerateEditingElementCore); TableView's own editor is not.
		bool appContent = CellEditingTemplate is not null;

		// Focus so typing goes straight into the editor; otherwise the row keeps focus and the user has
		// to click the editor they just opened. The editing root is not necessarily focusable - a
		// template column produces a ContentPresenter - so fall back to its first focusable descendant.
		var root = editingElement.XamlRoot;
		var initialFocus = root is not null ? FocusManager.GetFocusedElement(root) : null;
		if (!RunEditorContent(appContent, () => TryFocusEditor(editingElement)))
		{
			if (root is not null && FocusManager.GetFocusedElement(root) != initialFocus)
			{
				return null;
			}
			var owner = GetOwningTableView();
			var editingParent = VisualTreeHelper.GetParent(editingElement);
			TableViewRow? row = null;
			for (var parent = editingParent; parent is not null;
				parent = VisualTreeHelper.GetParent(parent))
			{
				if (parent is TableViewRow candidate)
				{
					row = candidate;
					break;
				}
			}
			var item = row?.DataContext;

			// The editing ContentPresenter may not have instantiated its DataTemplate.
			// Realize it before returning from F2 so the first keystroke has a target.
			RunEditorContent(appContent, () => editingElement.UpdateLayout());

			// Layout can run application code and recycle the row or end the edit.
			if (owner is not null && (GetOwningTableView() != owner ||
				owner.CurrentEditingElement() != editingElement ||
				editingParent is null || VisualTreeHelper.GetParent(editingElement) != editingParent ||
				row is null || row.GetOwningTableView() != owner ||
				!TableView.SameInspectableIdentity(row.DataContext, item)))
			{
				return null;
			}
			if (root is not null && FocusManager.GetFocusedElement(root) != initialFocus)
			{
				// A Loaded/focus handler chose another target; preserve that choice.
				return null;
			}
			if (!RunEditorContent(appContent, () => TryFocusEditor(editingElement)))
			{
				TVDiag.LogRetailF("[TableView] The realized cell editor has no available keyboard focus target.");
			}
		}

		// The base has no single value to snapshot - cancel is served by re-pulling from the source
		// (see CancelCellEditCore), which needs nothing carried across.
		return null;
	}

	internal bool CommitCellEdit(FrameworkElement? editingElement) => CommitCellEditCore(editingElement);

	// The editing bindings use UpdateSourceTrigger::Explicit, so this is what actually moves the typed
	// value onto the data item.
	//
	// Resolved HERE rather than when the edit opened: a CellEditingTemplate's ContentPresenter has not
	// stamped its template at begin time, so a walk then finds nothing. By commit time the editor is
	// realized. WPF sidesteps the same ordering problem with an UpdateLayout() call in BeginEdit.
	//
	// Returns false when nothing could be written, so the control keeps the edit open instead of
	// reporting a commit that never reached the item.
	internal virtual bool CommitCellEditCore(FrameworkElement? editingElement)
	{
		var expressions = TableViewColumn.CollectEditingBindingExpressions(editingElement);
		if (expressions.Count == 0)
		{
			return false;
		}

		var wroteEverything = true;
		foreach (var expression in expressions)
		{
			try
			{
				expression.UpdateSource();
			}
			catch (Exception)
			{
				// A setter that throws is the app rejecting the value, not a control failure.
				wroteEverything = false;
			}
		}

		return wroteEverything;
	}

	internal void CancelCellEdit(FrameworkElement? editingElement, object? uneditedValue) =>
		CancelCellEditCore(editingElement, uneditedValue);

	// Cancel needs to do nothing in the general case, and deliberately so.
	//
	// The editing binding is UpdateSourceTrigger::Explicit, so a cancelled edit never reached the data
	// item: the source still holds the pre-edit value. The editor is then discarded and the display
	// element - bound to that same untouched source - is put back. WinUI's BindingExpression has no
	// UpdateTarget(), but none is needed, because the target being refreshed is thrown away.
	//
	// A column whose editor writes outside its binding must override this.
	internal virtual void CancelCellEditCore(FrameworkElement? editingElement, object? uneditedValue)
	{
	}

	// Binding expressions on the editor subtree, for the properties an editor realistically writes.
	// Used by the base commit so a CellEditingTemplate works without the column knowing what the app
	// put in it. A column that knows its own editor should override and answer exactly.
	internal static List<BindingExpression> CollectEditingBindingExpressions(FrameworkElement? element)
	{
		List<BindingExpression> expressions = new();
		if (element is null)
		{
			return expressions;
		}

		List<DependencyProperty> properties = new();
		if (element is TextBox)
		{
			properties.Add(TextBox.TextProperty);
		}
		if (element is AutoSuggestBox)
		{
			properties.Add(AutoSuggestBox.TextProperty);
		}
		if (element is ToggleSwitch)
		{
			properties.Add(ToggleSwitch.IsOnProperty);
		}
		if (element is ToggleButton)
		{
			properties.Add(ToggleButton.IsCheckedProperty);
		}
		if (element is Selector)
		{
			properties.Add(Selector.SelectedItemProperty);
			properties.Add(Selector.SelectedIndexProperty);
			properties.Add(Selector.SelectedValueProperty);
		}
		if (element is Slider)
		{
			properties.Add(RangeBase.ValueProperty);
		}
		if (element is NumberBox)
		{
			properties.Add(NumberBox.ValueProperty);
		}

		foreach (var property in properties)
		{
			if (element.GetBindingExpression(property) is { } expression)
			{
				expressions.Add(expression);
			}
		}

		var childCount = VisualTreeHelper.GetChildrenCount(element);
		for (var i = 0; i < childCount; ++i)
		{
			if (VisualTreeHelper.GetChild(element, i) is FrameworkElement child)
			{
				var childExpressions = TableViewColumn.CollectEditingBindingExpressions(child);
				expressions.AddRange(childExpressions);
			}
		}

		return expressions;
	}

	// Typed accessor for the owning TableView.
	internal TableView? GetOwningTableView()
	{
		// Keep the owner weak; callers acquire a strong ref only for synchronous work.
		return m_owningTableView is not null && m_owningTableView.TryGetTarget(out var owner) ? owner : null;
	}

	internal uint AutomationIdentity()
	{
		if (m_automationIdentity == 0)
		{
			// fetch_add returns the pre-increment value.
			m_automationIdentity = Interlocked.Increment(ref s_nextAutomationIdentity) - 1;
		}
		return m_automationIdentity;
	}

	private protected void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var property = args.Property;

		if (property == WidthProperty ||
			property == MinWidthProperty ||
			property == MaxWidthProperty)
		{
			// Only an app-driven assignment replaces the authored width; a user resize does not.
			if (property == WidthProperty && !m_inUserResize)
			{
				m_authoredWidth = (GridLength)args.NewValue!;
			}

			UpdateActualWidth();
			if (GetOwningTableView() is { } owner)
			{
				if (property != WidthProperty || !m_inUserResize)
				{
					owner.ExpireLockedColumnHolds();
				}
				owner.OnColumnWidthChanged(this);
			}
		}
		else if (property == IsReadOnlyProperty)
		{
			// Closing an edit is the control's job - it owns the edit state - but the column is where
			// the property change surfaces. Without this the editor stays open on a cell that has just
			// declared itself read-only.
			if (GetOwningTableView() is { } owner)
			{
				owner.OnColumnIsReadOnlyChanged(this);
			}
		}
		else if (property == CellEditingTemplateProperty)
		{
			// Realized editors were generated from the previous template. An editor does not change
			// what the cell displays, so the Auto width keeps its history.
			NotifyCellContentChanged(false /* resetAutoWidth */);
		}
		else if (property == HeaderTemplateProperty ||
				 property == HeaderTemplateSelectorProperty ||
				 property == HeaderProperty)
		{
			// Re-render headers and recompute Auto widths after header content changes.
			if (GetOwningTableView() is { } owner)
			{
				owner.OnColumnHeaderChanged(this);
			}
		}
		else if (property == HeaderToolTipProperty)
		{
			// Not the header branch above: that recomputes Auto width, which a tooltip cannot change.
			if (GetOwningTableView() is { } owner)
			{
				owner.OnColumnHeaderToolTipChanged(this);
			}
		}
		else if (property == HeaderStyleProperty)
		{
			if (GetOwningTableView() is { } owner)
			{
				owner.QueueRebuildHeaders();
			}
		}
		else if (property == FrozenEdgeProperty)
		{
			if (GetOwningTableView() is { } owner)
			{
				owner.OnColumnFrozenEdgeChanged(this);
			}
		}
		else if (property == CanResizeProperty)
		{
			// Adds or removes this column's gripper, so the header band has to be rebuilt.
			if (GetOwningTableView() is { } owner)
			{
				owner.ExpireLockedColumnHolds();
				owner.QueueRebuildHeaders();
			}
		}
		else if (property == VisibilityProperty)
		{
			if (GetOwningTableView() is { } owner)
			{
				owner.ExpireLockedColumnHolds();
				owner.OnColumnVisibilityChanged(this);
			}
		}
		else if (property == CanSortProperty)
		{
			// The sort affordance is stamped at header-build time, so opting a column in or out at
			// runtime has to rebuild the headers for the chevron to appear or disappear.
			if (GetOwningTableView() is { } owner)
			{
				owner.OnColumnCanSortChanged(this);
			}
		}
	}

	// Not a dependency property: the codegen does not project delegate-typed DPs, and a comparer
	// has nothing to bind, animate or style anyway.
	/// <summary>
	/// Gets or sets an optional comparer supplied by the column for custom sort ordering.
	/// Takes precedence over <see cref="SortMemberPath"/>.
	/// </summary>
	public ITableViewSortComparer? CustomSortComparer
	{
		get => m_customSortComparer;
		set
		{
			if (m_customSortComparer == value)
			{
				return;
			}

			m_customSortComparer = value;
			if (GetOwningTableView() is { } tableView)
			{
				tableView.RefreshHeaderTabStopsInternal();
			}

			// Swapping the comparer invalidates the order the column is currently sorted in, so re-apply
			// the active direction rather than leaving the rows in the old comparer's order.
			if (GetOwningTableView() is { } owner && SortDirection != SortDirection.None)
			{
				var direction = SortDirection;
				var ownerImpl = owner;
				// Force a re-sort: SortByColumn no-ops when the column already carries this direction.
				ownerImpl.SortByColumn(this, SortDirection.None);
				ownerImpl.SortByColumn(this, direction);
			}
		}
	}

	// TODO Uno: `overridable` in the IDL and called by the owning TableView (TableView_Sort.cpp),
	// so it is protected internal rather than protected.
	/// <summary>
	/// Returns the effective sort key path. The base column returns SortMemberPath verbatim;
	/// TableViewTextColumn overrides to fall back to Binding.Path.Path when SortMemberPath is empty.
	/// </summary>
	/// <returns>The effective sort key path.</returns>
	protected internal virtual string GetSortMemberPathCore() => SortMemberPath;

	internal void SetSortStateInternal(SortDirection direction)
	{
		if (SortDirection == direction)
		{
			return;
		}

		// Read-only DP: same SetValue-via-key convention as ActualWidth.
		SetValue(SortDirectionProperty, direction);

		if (GetOwningTableView() is { } owner)
		{
			owner.RefreshSortIndicators();
		}
	}

	// Lets a derived column refresh its realized cells when one of ITS OWN properties changes.
	//
	// The base deliberately does not enumerate derived types' dependency properties: a third-party
	// column would then have no way to invalidate its cells without editing this file. This mirrors
	// WPF, where DataGridColumn owns the notification and each derived column routes its own property
	// callbacks into it.
	internal void NotifyCellContentChanged(bool resetAutoWidth = true)
	{
		if (GetOwningTableView() is { } owner)
		{
			owner.OnColumnCellTemplateChanged(this, resetAutoWidth);
		}
	}

	// Setter lets XAML pass the Binding object through without evaluating it.
	/// <summary>
	/// Gets or sets the opt-in per-cell tooltip content, bound against the row's data item; null or
	/// empty means no tooltip. A CLR property, not a DP, so XAML passes the Binding through unevaluated.
	/// </summary>
	public Binding? CellToolTipBinding
	{
		get => m_cellToolTipBinding;
		set
		{
			m_cellToolTipBinding = value;

			// Realized cells carry the previous binding. A tooltip does not change what the cell displays,
			// so the Auto width keeps its history.
			NotifyCellContentChanged(false /* resetAutoWidth */);
		}
	}

	internal bool SetOwningTableViewInternal(TableView? owner)
	{
		// Keep the owner weak to avoid TableView -> Columns -> Column -> TableView cycles.
		if (owner is not null)
		{
			// Single-owner: detach (owner cleared to null) from the first TableView before attaching to another.
			if (GetOwningTableView() is { } existing && existing != owner)
			{
				// Runs inside a Columns VectorChanged callback; an escaping throw would failfast.
				// Assert in debug, keep the existing owner in retail.
				MUX_ASSERT(false, "TableViewColumn re-owned without removing it from the previous TableView.Columns first");
				return false;
			}
			m_owningTableView = new WeakReference<TableView>(owner);
			// These have no generated change callback: a menu makes cells actionable, a sort key the header.
			if (m_cellContextFlyoutToken.Disposable is null)
			{
				WeakReference<TableViewColumn> weakThis = new(this);
				long cellContextFlyoutToken = RegisterPropertyChangedCallback(CellContextFlyoutProperty,
					(DependencyObject _, DependencyProperty _) =>
					{
						if (weakThis.TryGetTarget(out var strongThis) && strongThis.GetOwningTableView() is { } owningTableView)
						{
							owningTableView.RefreshCellFocusLevelsInternal();
						}
					});
				m_cellContextFlyoutToken.Disposable = Disposable.Create(() => UnregisterPropertyChangedCallback(CellContextFlyoutProperty, cellContextFlyoutToken));
				foreach (DependencyProperty property in new[] { SortMemberPathProperty, HeaderContextFlyoutProperty })
				{
					RegisterPropertyChangedCallback(property,
						(DependencyObject _, DependencyProperty _) =>
						{
							if (weakThis.TryGetTarget(out var strongThis) && strongThis.GetOwningTableView() is { } owningTableView)
							{
								owningTableView.RefreshHeaderTabStopsInternal();
							}
						});
				}
			}
			return true;
		}
		else
		{
			m_owningTableView = null;
			return true;
		}
	}

	internal void SetResolvedActualWidthInternal(double width)
	{
		// TableView resolved this column against the viewport / desired content. Uses the same
		// SetValue-via-key read-only DP convention as UpdateActualWidth; the caller has already
		// clamped to Min/MaxWidth.
		if (Math.Abs(width - ActualWidth) > 0.0001)
		{
			SetValue(ActualWidthProperty, Boxer.Box(width));
		}
	}

	internal void SetDesiredWidthInternal(double rowsDesiredWidth, double headerDesiredWidth)
	{
		m_rowsDesiredWidth = StdMath.Max(0.0, rowsDesiredWidth);
		m_headerDesiredWidth = StdMath.Max(0.0, headerDesiredWidth);
	}

	internal void ResetDesiredWidthInternal()
	{
		m_rowsDesiredWidth = 0.0;
		m_headerDesiredWidth = 0.0;
	}

	private void UpdateActualWidth()
	{
		// Pixel/Auto resolve to a fixed width here; Star gets a provisional default and is later
		// resolved against the viewport by the owning TableView (TableView::ResolveColumnWidths).
		var width = Width;
		var widthPixels =
			width.GridUnitType == GridUnitType.Pixel
				? NonNegativeFiniteOrDefault(width.Value)
				: c_widthDefault.Value;

		// Reject malformed widths and bounds the same way layout does, so provisional ActualWidth and
		// resolved layout widths cannot drift.
		var lo = NonNegativeFiniteOrZero(MinWidth);
		var hi = MaxWidthForColumn(this, lo);
		var clamped = StdMath.Clamp(widthPixels, lo, hi);

		// ActualWidth uses the SetValue-via-key read-only DP convention.
		if (Math.Abs(clamped - ActualWidth) > 0.0001)
		{
			SetValue(ActualWidthProperty, Boxer.Box(clamped));
		}
	}
}
