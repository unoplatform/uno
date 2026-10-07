// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewCellAutomationPeer.h, tag winui3/main, commit dc28206ea35

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation.Provider;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// UIA peer for a realized TableView cell; supplies the cell name and grid/table item coordinates.
// Weak row/column refs avoid extending recycled rows or removed columns.
// IVirtualizedItemProvider is the extra ReferenceTracker interface (not on the IDL surface), hence the explicit implementation.
partial class TableViewCellAutomationPeer : IVirtualizedItemProvider
{
	// TableViewCellAutomationPeer(
	//     winrt::FrameworkElement const& cell,
	//     winrt::TableViewRow const& row,
	//     winrt::TableViewColumn const& column,
	//     int32_t columnIndex);
	// ~TableViewCellAutomationPeer();

	// IAutomationPeerOverrides
	// winrt::IInspectable GetPatternCore(winrt::PatternInterface const& patternInterface);
	// hstring GetClassNameCore();
	// hstring GetNameCore();
	// winrt::IVector<winrt::AutomationPeer> GetChildrenCore();
	// hstring GetHelpTextCore();
	// winrt::AutomationControlType GetAutomationControlTypeCore();
	// hstring GetLocalizedControlTypeCore();

	// Overrides base IsTabStop-derived focusability for the two-level body navigation model.
	// bool IsKeyboardFocusableCore();
	// void SetFocusCore();

	// IGridItemProvider — per-cell coordinates in the owning TableView.
	// int32_t Row();
	// int32_t Column();
	// int32_t RowSpan();
	// int32_t ColumnSpan();
	// winrt::IRawElementProviderSimple ContainingGrid();

	// ITableItemProvider — returns this cell's column header; rows have no headers.
	// winrt::com_array<winrt::IRawElementProviderSimple> GetRowHeaderItems();
	// winrt::com_array<winrt::IRawElementProviderSimple> GetColumnHeaderItems();

	// IValueProvider — lets assistive technology read the cell text and set it without a pointer.
	// SetValue drives the same public edit lifecycle a user does (BeginEdit / write / CommitEdit),
	// so BeginningEdit / CellEditEnding handlers and validation all still run.
	// winrt::hstring Value();
	// bool IsReadOnly();
	// void SetValue(winrt::hstring const& value);

	// IVirtualizedItemProvider — available only after the owning row has been recycled out.
	// void Realize();

	// winrt::hstring ReadNameForEdit();
	// void BeginEditName();
	// void EndEditName();
	// void ResetEditName();
	// void UpdateNameItem(winrt::IInspectable const& item);

	// private:
	// True when this cell is editable AND its column produces a TextBox editor, which is the only
	// editor SetValue can drive today.
	// bool SupportsValuePattern();

	// Resolves the row index from the owning ItemsRepeater.
	// int32_t GetRowIndex();
	// bool IsVirtualized();
	// void RealizeCore();
	// winrt::UIElement GetRealizedCellFromRow(winrt::TableViewRow const& row);
	// winrt::TableView GetTrackedTableForRow(winrt::TableViewRow const& row);
	// winrt::IInspectable GetTrackedItem() const;
	// int32_t GetTrackedItemIndex(winrt::TableView const& tableView);
	// int32_t GetItemOccurrenceAtIndex(winrt::TableView const& tableView, winrt::IInspectable const& item, int32_t targetIndex);
	// bool IsTrackedRow(winrt::TableViewRow const& row, winrt::TableView const& tableView);
	// void TrackRowItem(winrt::TableViewRow const& row, winrt::TableView const& tableView);
	// [[noreturn]] static void ThrowElementNotAvailable();

	// Resolves the column's stringified Header.
	// winrt::hstring GetColumnHeaderText();

	// Resolves displayed text from the TextBlock or content peer name.
	// winrt::hstring GetCellValueText();
	// winrt::hstring ReadDisplayName(winrt::FrameworkElement const& display = nullptr);
	// void PrepareAutomationContentView(winrt::FrameworkElement const& cell);
	// bool GetCachedHasInteractiveCellContent(winrt::FrameworkElement const& cell);
	// winrt::IInspectable GetCurrentAutomationContentItem();
	// bool IsAutomationContentCacheValid(
	//     winrt::FrameworkElement const& content,
	//     winrt::IInspectable const& item) const;
	// bool CachedAutomationContentItemMatches(winrt::IInspectable const& item) const;
	// bool HasInteractiveCellContentAndRegisterCallbacks(
	//     winrt::UIElement const& element,
	//     uint32_t depthBudget = 8,
	//     uint32_t* remainingBudget = nullptr);
	// void RegisterAutomationContentPropertyCallbacks(winrt::UIElement const& element);
	// void InvalidateAutomationContentViewCache();
	// void ResetAutomationContentViewCache() noexcept;
	// void QueueFinalName(uint64_t generation);

	// TODO Uno: the C++ type is a move-only RAII token whose destructor revokes. C# has no deterministic
	// destruction, so owners call Revoke() explicitly before dropping it.
	private sealed class AutomationContentPropertyChangedRevoker
	{
		public AutomationContentPropertyChangedRevoker(
			DependencyObject @object,
			DependencyProperty property,
			long token)
		{
			m_object = new WeakReference<DependencyObject>(@object);
			m_property = property;
			m_token = token;
		}

		public void Revoke()
		{
			if (m_object is not null && m_object.TryGetTarget(out var @object) && m_property is not null)
			{
				try
				{
					@object.UnregisterPropertyChangedCallback(m_property, m_token);
				}
				catch
				{
				}
			}

			m_object = null;
			m_property = null;
			m_token = 0;
		}

		private WeakReference<DependencyObject>? m_object;
		private DependencyProperty? m_property;
		private long m_token;
	}

	private WeakReference<TableViewRow>? m_row = null;
	private WeakReference<TableViewColumn>? m_column = null;
	private WeakReference<TableView>? m_lastOwningTable = null;
	private TableViewTrackedItemIdentity m_item = new();
	private TableViewTrackedItemIdentity m_automationContentItem = new();
	// Construction-time fallback only; Column() recomputes from the live cell host.
	private int m_columnIndex = -1;
	private int m_lastKnownRowIndex = -1;
	private int m_trackedItemOccurrence = -1;
	private object? m_nameItem;
	private string? m_lastName;
	// Holds the pre-edit Name only; read paths must not write it or live cell names freeze.
	private string? m_editName;
	private ulong m_nameGeneration = 0;
	private readonly SerialDisposable m_nameLayoutUpdatedRevoker = new();
	private WeakReference<FrameworkElement>? m_automationContent = null;
	private bool? m_hasInteractiveAutomationContent;
	private readonly List<AutomationContentPropertyChangedRevoker> m_automationContentPropertyChangedRevokers = new();
}
