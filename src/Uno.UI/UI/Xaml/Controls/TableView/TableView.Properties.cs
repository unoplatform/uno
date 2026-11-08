// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\Generated\TableView.properties.cpp, tag winui3/main, commit 79209e041

#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Helpers.Boxes;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableView
{
	/// <summary>
	/// Gets or sets the optional alternating row background used for banding.
	/// </summary>
	public Brush? AlternatingRowBackground
	{
		get => (Brush?)GetValue(AlternatingRowBackgroundProperty);
		set => SetValue(AlternatingRowBackgroundProperty, value);
	}

	/// <summary>
	/// Identifies the AlternatingRowBackground dependency property.
	/// </summary>
	public static DependencyProperty AlternatingRowBackgroundProperty { get; } =
		DependencyProperty.Register(
			nameof(AlternatingRowBackground),
			typeof(Brush),
			typeof(TableView),
			new FrameworkPropertyMetadata(default(Brush), OnAlternatingRowBackgroundPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates whether the user can resize columns.
	/// </summary>
	public bool CanUserResizeColumns
	{
		get => (bool)GetValue(CanUserResizeColumnsProperty);
		set => SetValue(CanUserResizeColumnsProperty, value);
	}

	/// <summary>
	/// Identifies the CanUserResizeColumns dependency property.
	/// </summary>
	public static DependencyProperty CanUserResizeColumnsProperty { get; } =
		DependencyProperty.Register(
			nameof(CanUserResizeColumns),
			typeof(bool),
			typeof(TableView),
			new FrameworkPropertyMetadata(BoolBoxes.True, OnCanUserResizeColumnsPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates whether the user can sort columns by clicking their headers.
	/// </summary>
	public bool CanUserSortColumns
	{
		get => (bool)GetValue(CanUserSortColumnsProperty);
		set => SetValue(CanUserSortColumnsProperty, value);
	}

	/// <summary>
	/// Identifies the CanUserSortColumns dependency property.
	/// </summary>
	public static DependencyProperty CanUserSortColumnsProperty { get; } =
		DependencyProperty.Register(
			nameof(CanUserSortColumns),
			typeof(bool),
			typeof(TableView),
			new FrameworkPropertyMetadata(BoolBoxes.True, OnCanUserSortColumnsPropertyChanged));

	/// <summary>
	/// Gets the developer-defined column collection. This is the content property.
	/// </summary>
	public IList<TableViewColumn> Columns
	{
		get => (IList<TableViewColumn>)GetValue(ColumnsProperty);
		internal set => SetValue(ColumnsProperty, value);
	}

	/// <summary>
	/// Identifies the Columns dependency property.
	/// </summary>
	public static DependencyProperty ColumnsProperty { get; } =
		DependencyProperty.Register(
			nameof(Columns),
			typeof(IList<TableViewColumn>),
			typeof(TableView),
			new FrameworkPropertyMetadata(default(IList<TableViewColumn>), OnColumnsPropertyChanged));

	/// <summary>
	/// Gets or sets the row and cell spacing preset.
	/// </summary>
	public TableViewDensity Density
	{
		get => (TableViewDensity)GetValue(DensityProperty);
		set => SetValue(DensityProperty, value);
	}

	/// <summary>
	/// Identifies the Density dependency property.
	/// </summary>
	public static DependencyProperty DensityProperty { get; } =
		DependencyProperty.Register(
			nameof(Density),
			typeof(TableViewDensity),
			typeof(TableView),
			new FrameworkPropertyMetadata(TableViewDensity.Standard, OnDensityPropertyChanged));

	/// <summary>
	/// Gets or sets the template displayed when there are no rows.
	/// </summary>
	public DataTemplate? EmptyTemplate
	{
		get => (DataTemplate?)GetValue(EmptyTemplateProperty);
		set => SetValue(EmptyTemplateProperty, value);
	}

	/// <summary>
	/// Identifies the EmptyTemplate dependency property.
	/// </summary>
	public static DependencyProperty EmptyTemplateProperty { get; } =
		DependencyProperty.Register(
			nameof(EmptyTemplate),
			typeof(DataTemplate),
			typeof(TableView),
			new FrameworkPropertyMetadata(default(DataTemplate), OnEmptyTemplatePropertyChanged));

	/// <summary>
	/// Gets or sets which gridlines are displayed.
	/// </summary>
	public TableViewGridLinesVisibility GridLinesVisibility
	{
		get => (TableViewGridLinesVisibility)GetValue(GridLinesVisibilityProperty);
		set => SetValue(GridLinesVisibilityProperty, value);
	}

	/// <summary>
	/// Identifies the GridLinesVisibility dependency property.
	/// </summary>
	public static DependencyProperty GridLinesVisibilityProperty { get; } =
		DependencyProperty.Register(
			nameof(GridLinesVisibility),
			typeof(TableViewGridLinesVisibility),
			typeof(TableView),
			new FrameworkPropertyMetadata(TableViewGridLinesVisibility.All, OnGridLinesVisibilityPropertyChanged));

	/// <summary>
	/// Gets or sets the template used for the content of group headers.
	/// </summary>
	public DataTemplate? GroupHeaderTemplate
	{
		get => (DataTemplate?)GetValue(GroupHeaderTemplateProperty);
		set => SetValue(GroupHeaderTemplateProperty, value);
	}

	/// <summary>
	/// Identifies the GroupHeaderTemplate dependency property.
	/// </summary>
	public static DependencyProperty GroupHeaderTemplateProperty { get; } =
		DependencyProperty.Register(
			nameof(GroupHeaderTemplate),
			typeof(DataTemplate),
			typeof(TableView),
			new FrameworkPropertyMetadata(default(DataTemplate), OnGroupHeaderTemplatePropertyChanged));

	/// <summary>
	/// Gets or sets the style applied to every column header. A column's HeaderStyle takes precedence.
	/// </summary>
	public Style? ColumnHeaderStyle
	{
		get => (Style?)GetValue(ColumnHeaderStyleProperty);
		set => SetValue(ColumnHeaderStyleProperty, value);
	}

	/// <summary>
	/// Identifies the ColumnHeaderStyle dependency property.
	/// </summary>
	public static DependencyProperty ColumnHeaderStyleProperty { get; } =
		DependencyProperty.Register(
			nameof(ColumnHeaderStyle),
			typeof(Style),
			typeof(TableView),
			new FrameworkPropertyMetadata(default(Style), OnColumnHeaderStylePropertyChanged));

	/// <summary>
	/// Gets or sets which headers are displayed.
	/// </summary>
	public TableViewHeadersVisibility HeadersVisibility
	{
		get => (TableViewHeadersVisibility)GetValue(HeadersVisibilityProperty);
		set => SetValue(HeadersVisibilityProperty, value);
	}

	/// <summary>
	/// Identifies the HeadersVisibility dependency property.
	/// </summary>
	public static DependencyProperty HeadersVisibilityProperty { get; } =
		DependencyProperty.Register(
			nameof(HeadersVisibility),
			typeof(TableViewHeadersVisibility),
			typeof(TableView),
			new FrameworkPropertyMetadata(TableViewHeadersVisibility.Column, OnHeadersVisibilityPropertyChanged));

	/// <summary>
	/// Gets or sets a value that gates editing for the whole control.
	/// </summary>
	public bool IsReadOnly
	{
		get => (bool)GetValue(IsReadOnlyProperty);
		set => SetValue(IsReadOnlyProperty, value);
	}

	/// <summary>
	/// Identifies the IsReadOnly dependency property.
	/// </summary>
	public static DependencyProperty IsReadOnlyProperty { get; } =
		DependencyProperty.Register(
			nameof(IsReadOnly),
			typeof(bool),
			typeof(TableView),
			new FrameworkPropertyMetadata(BoolBoxes.True, OnIsReadOnlyPropertyChanged));

	/// <summary>
	/// Gets or sets the source collection for table rows.
	/// </summary>
	public object? ItemsSource
	{
		get => GetValue(ItemsSourceProperty);
		set => SetValue(ItemsSourceProperty, value);
	}

	/// <summary>
	/// Identifies the ItemsSource dependency property.
	/// </summary>
	public static DependencyProperty ItemsSourceProperty { get; } =
		DependencyProperty.Register(
			nameof(ItemsSource),
			typeof(object),
			typeof(TableView),
			new FrameworkPropertyMetadata(default(object), OnItemsSourcePropertyChanged));

	/// <summary>
	/// Gets or sets the background brush for rows.
	/// </summary>
	public Brush? RowBackground
	{
		get => (Brush?)GetValue(RowBackgroundProperty);
		set => SetValue(RowBackgroundProperty, value);
	}

	/// <summary>
	/// Identifies the RowBackground dependency property.
	/// </summary>
	public static DependencyProperty RowBackgroundProperty { get; } =
		DependencyProperty.Register(
			nameof(RowBackground),
			typeof(Brush),
			typeof(TableView),
			new FrameworkPropertyMetadata(default(Brush), OnRowBackgroundPropertyChanged));

	/// <summary>
	/// Gets the selected item's index in the displayed row projection, or -1 when nothing is selected.
	/// </summary>
	/// <remarks>
	/// Under grouping, group headers occupy projection indexes but cannot be selected.
	/// </remarks>
	public int SelectedIndex
	{
		get => (int)GetValue(SelectedIndexProperty);
		internal set => SetValue(SelectedIndexProperty, Boxer.Box(value));
	}

	/// <summary>
	/// Identifies the SelectedIndex dependency property.
	/// </summary>
	public static DependencyProperty SelectedIndexProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectedIndex),
			typeof(int),
			typeof(TableView),
			new FrameworkPropertyMetadata(IntBoxes.NegativeOne));

	/// <summary>
	/// Gets the selected data item, or null.
	/// </summary>
	public object? SelectedItem
	{
		get => GetValue(SelectedItemProperty);
		internal set => SetValue(SelectedItemProperty, value);
	}

	/// <summary>
	/// Identifies the SelectedItem dependency property.
	/// </summary>
	public static DependencyProperty SelectedItemProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectedItem),
			typeof(object),
			typeof(TableView),
			new FrameworkPropertyMetadata(default(object)));

	/// <summary>
	/// Gets or sets the row selection behavior.
	/// </summary>
	public TableViewSelectionMode SelectionMode
	{
		get => (TableViewSelectionMode)GetValue(SelectionModeProperty);
		set => SetValue(SelectionModeProperty, value);
	}

	/// <summary>
	/// Identifies the SelectionMode dependency property.
	/// </summary>
	public static DependencyProperty SelectionModeProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectionMode),
			typeof(TableViewSelectionMode),
			typeof(TableView),
			new FrameworkPropertyMetadata(TableViewSelectionMode.Single, OnSelectionModePropertyChanged));

	/// <summary>
	/// Occurs before a cell edit opens. Set Cancel to true to prevent it.
	/// </summary>
	public event TypedEventHandler<TableView, TableViewBeginningEditEventArgs>? BeginningEdit;

	/// <summary>
	/// Occurs before a cell edit closes. Vetoable via Cancel; the handler must decide before returning.
	/// </summary>
	public event TypedEventHandler<TableView, TableViewCellEditEndingEventArgs>? CellEditEnding;

	/// <summary>
	/// Occurs after the selection has settled.
	/// </summary>
	public event TypedEventHandler<TableView, SelectionChangedEventArgs>? SelectionChanged;

	/// <summary>
	/// Occurs after a sort-state change has been applied.
	/// </summary>
	public event TypedEventHandler<TableView, TableViewSortedEventArgs>? Sorted;

	/// <summary>
	/// Occurs before a sort-state change is applied.
	/// </summary>
	public event TypedEventHandler<TableView, TableViewSortingEventArgs>? Sorting;

	private static void OnAlternatingRowBackgroundPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TableView)sender;
		owner.OnAlternatingRowBackgroundPropertyChanged(args);
	}

	private static void OnCanUserResizeColumnsPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TableView)sender;
		owner.OnCanUserResizeColumnsPropertyChanged(args);
	}

	private static void OnCanUserSortColumnsPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TableView)sender;
		owner.OnCanUserSortColumnsPropertyChanged(args);
	}

	private static void OnColumnsPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TableView)sender;
		owner.OnColumnsPropertyChanged(args);
	}

	private static void OnDensityPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TableView)sender;
		owner.OnDensityPropertyChanged(args);
	}

	private static void OnEmptyTemplatePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TableView)sender;
		owner.OnEmptyTemplatePropertyChanged(args);
	}

	private static void OnGroupHeaderTemplatePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TableView)sender;
		owner.OnGroupHeaderTemplatePropertyChanged(args);
	}

	private static void OnColumnHeaderStylePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TableView)sender;
		owner.OnColumnHeaderStylePropertyChanged(args);
	}

	private static void OnGridLinesVisibilityPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TableView)sender;
		owner.OnGridLinesVisibilityPropertyChanged(args);
	}

	private static void OnHeadersVisibilityPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TableView)sender;
		owner.OnHeadersVisibilityPropertyChanged(args);
	}

	private static void OnIsReadOnlyPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TableView)sender;
		owner.OnIsReadOnlyPropertyChanged(args);
	}

	private static void OnItemsSourcePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TableView)sender;
		owner.OnItemsSourcePropertyChanged(args);
	}

	private static void OnRowBackgroundPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TableView)sender;
		owner.OnRowBackgroundPropertyChanged(args);
	}

	private static void OnSelectionModePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TableView)sender;
		owner.OnSelectionModePropertyChanged(args);
	}
}
