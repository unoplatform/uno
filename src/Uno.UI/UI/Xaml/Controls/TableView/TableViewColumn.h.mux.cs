// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewColumn.h, tag winui3/main, commit 79209e041

#nullable enable

using System;
using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewColumn
{
	// Default pixel width and fallback for unresolved Auto values (and Star before the owning
	// TableView has a viewport to resolve against); keep in sync with ActualWidth's
	// MUX_DEFAULT_VALUE("120.0") in TableView.idl.
	// TODO Uno: `static constexpr winrt::GridLength` in C++; a property, as GridLength cannot be a C# const.
	internal static GridLength c_widthDefault => new(120.0, GridUnitType.Pixel);

	// TODO Uno: Free functions in C++; call them as TableViewColumn.NonNegativeFiniteOrZero(...).
	internal static double NonNegativeFiniteOrZero(double value) =>
		double.IsFinite(value) && value >= 0.0 ? value : 0.0;

	internal static double NonNegativeFiniteOrDefault(double value) =>
		double.IsFinite(value) && value >= 0.0 ? value : c_widthDefault.Value;

	internal static double MaxWidthForColumn(TableViewColumn column, double minWidth)
	{
		var maxWidth = column.MaxWidth;
		if (double.IsNaN(maxWidth) || double.IsPositiveInfinity(maxWidth))
		{
			return double.PositiveInfinity;
		}

		// A negative MaxWidth is a malformed finite bound. Treat it as zero so it still clamps to
		// MinWidth; only NaN and positive infinity mean there is no upper bound.
		return Math.Max(minWidth, Math.Max(0.0, maxWidth));
	}

	// TableViewColumn();

	// Public ABI
	// winrt::FrameworkElement GenerateElement(const winrt::IInspectable& dataItem);
	// winrt::FrameworkElement GenerateEditingElement(const winrt::IInspectable& dataItem);
	// winrt::IInspectable PrepareCellForEdit(const winrt::FrameworkElement& editingElement, const winrt::RoutedEventArgs& editingEventArgs);
	// bool CommitCellEdit(const winrt::FrameworkElement& editingElement);
	// void CancelCellEdit(const winrt::FrameworkElement& editingElement, const winrt::IInspectable& uneditedValue);

	// Typed accessor for the owning TableView.
	// winrt::TableView GetOwningTableView();

	// Internal — stable, non-address token used by column-header automation peers.
	// uint32_t AutomationIdentity();

	// Overridable
	// virtual winrt::hstring GetSortMemberPathCore();
	// virtual winrt::FrameworkElement GenerateElementCore(const winrt::IInspectable& dataItem);
	// virtual winrt::FrameworkElement GenerateEditingElementCore(const winrt::IInspectable& dataItem);
	// virtual winrt::IInspectable PrepareCellForEditCore(const winrt::FrameworkElement& editingElement, const winrt::RoutedEventArgs& editingEventArgs);
	// virtual bool CommitCellEditCore(const winrt::FrameworkElement& editingElement);
	// virtual void CancelCellEditCore(const winrt::FrameworkElement& editingElement, const winrt::IInspectable& uneditedValue);

	// Binding expressions on an editor subtree, for the properties an editor realistically writes.
	// static std::vector<winrt::BindingExpression> CollectEditingBindingExpressions(const winrt::FrameworkElement& element);

	// Property-changed callback (single dispatch in IDL)
	// void OnPropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args);

	// Not a dependency property: the codegen does not project delegate-typed DPs, and a comparer
	// has nothing to bind, animate or style anyway.
	// TODO Uno: The getter body is inline here in C++ (`return m_customSortComparer;`) but the setter is
	// in TableViewColumn.cpp; a C# property cannot be split, so the whole CustomSortComparer property
	// lives in TableViewColumn.mux.cs over m_customSortComparer below.
	// winrt::ITableViewSortComparer CustomSortComparer() const { return m_customSortComparer; }
	// void CustomSortComparer(winrt::ITableViewSortComparer const& value);

	// Derived columns call this when one of their own properties invalidates realized cell content.
	// void NotifyCellContentChanged(bool resetAutoWidth = true);

	// Keep the owner weak to avoid TableView -> Columns -> Column -> TableView cycles.
	// bool SetOwningTableViewInternal(winrt::TableView const& owner);

	// Setter lets XAML pass the Binding object through without evaluating it.
	// winrt::Microsoft::UI::Xaml::Data::Binding CellToolTipBinding();
	// void CellToolTipBinding(const winrt::Microsoft::UI::Xaml::Data::Binding& value);

	// Internal — layout: the owning TableView (TableView_Layout.cpp) resolves the final width for
	// every sizing mode (Pixel/Auto/Star) and pushes it here; the caller has already clamped to
	// Min/MaxWidth.
	// void SetResolvedActualWidthInternal(double width);

	// Internal - sorting: the owning TableView owns sort policy and pushes the resulting direction
	// into this read-only DP, then republishes it to the realized header chevrons. The push is
	// required rather than a binding: SortIndicatorDirection and SortDirection are distinct WinRT
	// enums, so a {Binding} between them silently does nothing.
	// void SetSortStateInternal(winrt::SortDirection direction);

	// Internal — Auto size-to-content. The owning TableView pulls measured widths from the header
	// and realized row panels; ResolveColumnWidths keeps the grow-only max of the row widths and of
	// the header width separately, so hiding the headers drops only the header term. Both reset on
	// data-set boundaries (ItemsSource / Columns replaced / CellTemplate / Header changes).
	// void SetDesiredWidthInternal(double rowsDesiredWidth, double headerDesiredWidth);
	// void ResetDesiredWidthInternal();
	internal double RowsDesiredWidthInternal() => m_rowsDesiredWidth;
	internal double HeaderDesiredWidthInternal() => m_headerDesiredWidth;
	internal double DesiredWidthInternal() => Math.Max(m_rowsDesiredWidth, m_headerDesiredWidth);

	// Internal - resize: the width the app authored. A user resize rewrites Width as pixels, and
	// the layout needs the original mode to know whether the table is meant to fit its viewport.
	internal GridLength AuthoredWidthInternal() => m_authoredWidth;
	// Width written inside this scope is the control resizing, not the app re-authoring the column.
	internal UserResizeScope BeginUserResizeScope()
	{
		m_inUserResize = true;
		return new(this);
	}

	// gsl::finally semantics: writes false on dispose rather than restoring the previous value.
	internal readonly struct UserResizeScope(TableViewColumn owner) : IDisposable
	{
		public void Dispose() => owner.m_inUserResize = false;
	}

	// private:
	// Write the resolved, clamped width into the read-only ActualWidth DP.
	// void UpdateActualWidth();

	// Monotonic maxima of the pulled realized-row cell and header measured widths for an Auto column
	// (0 until first pull; the header term is 0 while headers are hidden).
	private double m_rowsDesiredWidth = 0.0;
	private double m_headerDesiredWidth = 0.0;
	private GridLength m_authoredWidth = c_widthDefault;
	private bool m_inUserResize = false;

	private Binding? m_cellToolTipBinding;

	private WeakReference<TableView>? m_owningTableView = null;
	private readonly Uno.Disposables.SerialDisposable m_cellContextFlyoutToken = new();
	private ITableViewSortComparer? m_customSortComparer = null;
	private uint m_automationIdentity = 0;
}
