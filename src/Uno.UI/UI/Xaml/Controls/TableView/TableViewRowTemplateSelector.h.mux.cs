// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewRowTemplateSelector.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Chooses between a TableViewRow and a TableViewGroupHeader container template. Assigned to
// PART_RowsRepeater.ItemTemplate, so ItemsRepeater wraps it in ItemTemplateWrapper and the
// framework supplies the per-template recycle pools and owner-aware unparenting.
partial class TableViewRowTemplateSelector
{
	// TableViewRowTemplateSelector() = default;

	// ItemTemplateWrapper calls the single-arg form; the two-arg overload delegates to it so
	// the two cannot drift.
	// winrt::DataTemplate SelectTemplateCore(winrt::IInspectable const& item);
	// winrt::DataTemplate SelectTemplateCore(winrt::IInspectable const& item, winrt::DependencyObject const& container);

	// void SetOwningTableViewInternal(winrt::TableView const& owner);

	// Must run from ~TableView while it still holds the selector. Once anything has been
	// recycled the pools hang off the cached templates and close the cycle
	// repeater -> wrapper -> selector -> template -> pool -> repeater through plain C++
	// references the reference tracker cannot walk, so nothing is collected and the selector's
	// own destructor never runs. Dropping the pools breaks the one edge we own.
	// void Detach();

	// private:
	// winrt::DataTemplate ResolveTemplateFromMarkup(std::wstring_view markup);
	// void EnsureTemplates();

	private WeakReference<TableView>? m_owningTableView = null;

	// Cached: ItemTemplateWrapper keys the recycle pool by DataTemplate instance, so the same
	// instance must come back every call. Resolved lazily -- no resources exist at construction.
	private DataTemplate? m_rowTemplate = null;
	private DataTemplate? m_groupHeaderTemplate = null;
	private bool m_templatesResolved = false;

	// Markup rather than a resource key: these would live in the control's generic dictionary,
	// whose root LookupElementResource does not walk, so both keys resolve to null and the null
	// template fails realization. ViewManager and ItemsView use XamlReader::Load for the same
	// reason. HorizontalAlignment=Left is required: the band sizes itself from MeasureOverride,
	// and Stretch centres a narrower desired width, leaving a gap at both ends.
	private const string s_rowContainerMarkup =
		"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
		" xmlns:tv='using:Microsoft.UI.Xaml.Controls.Tabular'>" +
		"<tv:TableViewRow/></DataTemplate>";

	private const string s_groupHeaderContainerMarkup =
		"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
		" xmlns:tv='using:Microsoft.UI.Xaml.Controls.Tabular'>" +
		"<tv:TableViewGroupHeader HorizontalAlignment='Left'/></DataTemplate>";
}
