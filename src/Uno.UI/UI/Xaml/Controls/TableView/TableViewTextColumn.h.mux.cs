// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewTextColumn.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewTextColumn
{
	// TODO Uno: Original C++: ForwardRefToBaseReferenceTracker(TableViewColumn) - reference-tracker plumbing, not needed in C#.

	// TableViewTextColumn();

	// Setter lets XAML pass the Binding object through without evaluating it.
	// winrt::Microsoft::UI::Xaml::Data::Binding Binding();
	// void Binding(const winrt::Microsoft::UI::Xaml::Data::Binding& value);

	// Override
	// winrt::hstring GetSortMemberPathCore() override;
	// winrt::FrameworkElement GenerateElementCore(const winrt::IInspectable& dataItem) override;
	// winrt::FrameworkElement GenerateEditingElementCore(const winrt::IInspectable& dataItem) override;
	// winrt::IInspectable PrepareCellForEditCore(const winrt::FrameworkElement& editingElement, const winrt::RoutedEventArgs& editingEventArgs) override;
	// bool CommitCellEditCore(const winrt::FrameworkElement& editingElement) override;
	// void CancelCellEditCore(const winrt::FrameworkElement& editingElement, const winrt::IInspectable& uneditedValue) override;

	// Internal, not projected: the data field this column edits, derived from Binding rather than
	// authored separately. The public answer to "which field is this column about?" is the base
	// column's SortMemberPath, so this must never become a second public concept.
	// winrt::hstring GetEditingPropertyPath() const;

	// private:
	private Data.Binding? m_binding;
}
