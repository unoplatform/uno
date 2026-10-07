// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\Generated\TableViewColumn.properties.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewColumn
{
	// Read-only DP written by the column's width resolve; the header host and realized cell panels
	// arrange each cell to this width (no per-cell binding).
	/// <summary>
	/// Gets the resolved, MinWidth/MaxWidth-clamped width of the column.
	/// </summary>
	public double ActualWidth
	{
		get => (double)GetValue(ActualWidthProperty);
		internal set => SetValue(ActualWidthProperty, Boxer.Box(value));
	}

	/// <summary>
	/// Identifies the ActualWidth dependency property.
	/// </summary>
	public static DependencyProperty ActualWidthProperty { get; } =
		DependencyProperty.Register(
			nameof(ActualWidth),
			typeof(double),
			typeof(TableViewColumn),
			new FrameworkPropertyMetadata(120.0));

	// Gates the resize affordance for this column only; the owner's CanUserResizeColumns gates all of them.
	/// <summary>
	/// Gets or sets a value that indicates whether the user can resize this column.
	/// </summary>
	public bool CanResize
	{
		get => (bool)GetValue(CanResizeProperty);
		set => SetValue(CanResizeProperty, value);
	}

	/// <summary>
	/// Identifies the CanResize dependency property.
	/// </summary>
	public static DependencyProperty CanResizeProperty { get; } =
		DependencyProperty.Register(
			nameof(CanResize),
			typeof(bool),
			typeof(TableViewColumn),
			new FrameworkPropertyMetadata(BoolBoxes.True, OnPropertyChanged));

	// Per-column opt-out for the click-to-sort UX. Default true. When false the header click
	// handler ignores this column; programmatic SortByColumn still works.
	/// <summary>
	/// Gets or sets a value that indicates whether clicking this column's header sorts it.
	/// </summary>
	public bool CanSort
	{
		get => (bool)GetValue(CanSortProperty);
		set => SetValue(CanSortProperty, value);
	}

	/// <summary>
	/// Identifies the CanSort dependency property.
	/// </summary>
	public static DependencyProperty CanSortProperty { get; } =
		DependencyProperty.Register(
			nameof(CanSort),
			typeof(bool),
			typeof(TableViewColumn),
			new FrameworkPropertyMetadata(BoolBoxes.True, OnPropertyChanged));

	// Editing visual. A column with no CellEditingTemplate and no built-in editor is not editable.
	/// <summary>
	/// Gets or sets the editing visual for the column's cells.
	/// </summary>
	public DataTemplate? CellEditingTemplate
	{
		get => (DataTemplate?)GetValue(CellEditingTemplateProperty);
		set => SetValue(CellEditingTemplateProperty, value);
	}

	/// <summary>
	/// Identifies the CellEditingTemplate dependency property.
	/// </summary>
	public static DependencyProperty CellEditingTemplateProperty { get; } =
		DependencyProperty.Register(
			nameof(CellEditingTemplate),
			typeof(DataTemplate),
			typeof(TableViewColumn),
			new FrameworkPropertyMetadata(default(DataTemplate), OnPropertyChanged));

	// Pins the column during horizontal scroll; Leading is implemented, Trailing reserved.
	/// <summary>
	/// Gets or sets the edge the column is pinned to during horizontal scrolling.
	/// </summary>
	public TableViewFrozenEdge FrozenEdge
	{
		get => (TableViewFrozenEdge)GetValue(FrozenEdgeProperty);
		set => SetValue(FrozenEdgeProperty, value);
	}

	/// <summary>
	/// Identifies the FrozenEdge dependency property.
	/// </summary>
	public static DependencyProperty FrozenEdgeProperty { get; } =
		DependencyProperty.Register(
			nameof(FrozenEdge),
			typeof(TableViewFrozenEdge),
			typeof(TableViewColumn),
			new FrameworkPropertyMetadata(TableViewFrozenEdge.None, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the header content for the column.
	/// </summary>
	public object? Header
	{
		get => GetValue(HeaderProperty);
		set => SetValue(HeaderProperty, value);
	}

	/// <summary>
	/// Identifies the Header dependency property.
	/// </summary>
	public static DependencyProperty HeaderProperty { get; } =
		DependencyProperty.Register(
			nameof(Header),
			typeof(object),
			typeof(TableViewColumn),
			new FrameworkPropertyMetadata(default(object), OnPropertyChanged));

	// Optional header template; HeaderTemplateSelector takes precedence.
	/// <summary>
	/// Gets or sets the template used to display the header.
	/// </summary>
	public DataTemplate? HeaderTemplate
	{
		get => (DataTemplate?)GetValue(HeaderTemplateProperty);
		set => SetValue(HeaderTemplateProperty, value);
	}

	/// <summary>
	/// Identifies the HeaderTemplate dependency property.
	/// </summary>
	public static DependencyProperty HeaderTemplateProperty { get; } =
		DependencyProperty.Register(
			nameof(HeaderTemplate),
			typeof(DataTemplate),
			typeof(TableViewColumn),
			new FrameworkPropertyMetadata(default(DataTemplate), OnPropertyChanged));

	/// <summary>
	/// Gets or sets the template selector used to display the header; takes precedence over HeaderTemplate.
	/// </summary>
	public DataTemplateSelector? HeaderTemplateSelector
	{
		get => (DataTemplateSelector?)GetValue(HeaderTemplateSelectorProperty);
		set => SetValue(HeaderTemplateSelectorProperty, value);
	}

	/// <summary>
	/// Identifies the HeaderTemplateSelector dependency property.
	/// </summary>
	public static DependencyProperty HeaderTemplateSelectorProperty { get; } =
		DependencyProperty.Register(
			nameof(HeaderTemplateSelector),
			typeof(DataTemplateSelector),
			typeof(TableViewColumn),
			new FrameworkPropertyMetadata(default(DataTemplateSelector), OnPropertyChanged));

	// Opt-in header tooltip content; null or empty means no tooltip. Not a Binding property: a
	// header is not bound against a row, so there is nothing to defer.
	/// <summary>
	/// Gets or sets the opt-in tooltip content for this column's header.
	/// </summary>
	public object? HeaderToolTip
	{
		get => GetValue(HeaderToolTipProperty);
		set => SetValue(HeaderToolTipProperty, value);
	}

	/// <summary>
	/// Identifies the HeaderToolTip dependency property.
	/// </summary>
	public static DependencyProperty HeaderToolTipProperty { get; } =
		DependencyProperty.Register(
			nameof(HeaderToolTip),
			typeof(object),
			typeof(TableViewColumn),
			new FrameworkPropertyMetadata(default(object), OnPropertyChanged));

	// Per-column opt-out. A read-only column can still be the current cell, but cannot be edited.
	/// <summary>
	/// Gets or sets a value that indicates whether the column's cells cannot be edited.
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
			typeof(TableViewColumn),
			new FrameworkPropertyMetadata(BoolBoxes.False, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the maximum column width.
	/// </summary>
	public double MaxWidth
	{
		get => (double)GetValue(MaxWidthProperty);
		set => SetValue(MaxWidthProperty, Boxer.Box(value));
	}

	/// <summary>
	/// Identifies the MaxWidth dependency property.
	/// </summary>
	public static DependencyProperty MaxWidthProperty { get; } =
		DependencyProperty.Register(
			nameof(MaxWidth),
			typeof(double),
			typeof(TableViewColumn),
			new FrameworkPropertyMetadata(double.PositiveInfinity, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the minimum column width.
	/// </summary>
	public double MinWidth
	{
		get => (double)GetValue(MinWidthProperty);
		set => SetValue(MinWidthProperty, Boxer.Box(value));
	}

	/// <summary>
	/// Identifies the MinWidth dependency property.
	/// </summary>
	public static DependencyProperty MinWidthProperty { get; } =
		DependencyProperty.Register(
			nameof(MinWidth),
			typeof(double),
			typeof(TableViewColumn),
			new FrameworkPropertyMetadata(20.0, OnPropertyChanged));

	// Direction sequence repeated header clicks walk for THIS column. Per-column because the cycle
	// describes how one column responds to being clicked again, and a table can reasonably mix
	// policies - a metric column that should open on its largest values alongside a name column
	// that should open A-Z. Read at click time, so a change needs no rebuild.
	/// <summary>
	/// Gets or sets the direction sequence repeated header clicks walk for this column.
	/// </summary>
	public TableViewSortCycle SortCycle
	{
		get => (TableViewSortCycle)GetValue(SortCycleProperty);
		set => SetValue(SortCycleProperty, value);
	}

	/// <summary>
	/// Identifies the SortCycle dependency property.
	/// </summary>
	public static DependencyProperty SortCycleProperty { get; } =
		DependencyProperty.Register(
			nameof(SortCycle),
			typeof(TableViewSortCycle),
			typeof(TableViewColumn),
			new FrameworkPropertyMetadata(TableViewSortCycle.AscendingDescending));

	// Read-only DP driven by the owning TableView. None means the column is not part of the
	// active sort.
	/// <summary>
	/// Gets the column's current sort direction. None means the column is not part of the active sort.
	/// </summary>
	public SortDirection SortDirection
	{
		get => (SortDirection)GetValue(SortDirectionProperty);
		internal set => SetValue(SortDirectionProperty, value);
	}

	/// <summary>
	/// Identifies the SortDirection dependency property.
	/// </summary>
	public static DependencyProperty SortDirectionProperty { get; } =
		DependencyProperty.Register(
			nameof(SortDirection),
			typeof(SortDirection),
			typeof(TableViewColumn),
			new FrameworkPropertyMetadata(SortDirection.None));

	// Property name on the row item used to extract the sort key. When empty, TableViewTextColumn
	// falls back to its Binding.Path.Path; the base column returns an empty string and the column
	// is only sortable if it supplies a CustomSortComparer.
	//
	// The path need not be unique. Several columns may name the same property - a common shape when
	// one column formats a value and another ranks it. Each column sorts as its own independent
	// axis, keyed on column identity rather than on this path, so duplicates neither collide nor
	// prevent the table from being built. Sorting one of them leaves the others' SortDirection at
	// None, since sorting is single-column: applying a sort clears any previous one.
	/// <summary>
	/// Gets or sets the property name on the row item used to extract the sort key.
	/// </summary>
	public string SortMemberPath
	{
		get => (string?)GetValue(SortMemberPathProperty) ?? string.Empty;
		set => SetValue(SortMemberPathProperty, value);
	}

	/// <summary>
	/// Identifies the SortMemberPath dependency property.
	/// </summary>
	public static DependencyProperty SortMemberPathProperty { get; } =
		DependencyProperty.Register(
			nameof(SortMemberPath),
			typeof(string),
			typeof(TableViewColumn),
			new FrameworkPropertyMetadata(string.Empty));

	/// <summary>
	/// Gets or sets the column visibility.
	/// </summary>
	public Visibility Visibility
	{
		get => (Visibility)GetValue(VisibilityProperty);
		set => SetValue(VisibilityProperty, value);
	}

	/// <summary>
	/// Identifies the Visibility dependency property.
	/// </summary>
	public static DependencyProperty VisibilityProperty { get; } =
		DependencyProperty.Register(
			nameof(Visibility),
			typeof(Visibility),
			typeof(TableViewColumn),
			new FrameworkPropertyMetadata(Visibility.Visible, OnPropertyChanged));

	// Width carries GridLength intent; ActualWidth is the resolved, MinWidth/MaxWidth-clamped
	// pixels (see TableView_Layout.cpp). Pixel = exact; Auto = widest realized cell (grows within a
	// data set); Star = a proportional share of the body viewport after fixed columns.
	/// <summary>
	/// Gets or sets the column width as a GridLength (Pixel, Auto or Star).
	/// </summary>
	public GridLength Width
	{
		get => (GridLength)GetValue(WidthProperty);
		set => SetValue(WidthProperty, value);
	}

	/// <summary>
	/// Identifies the Width dependency property.
	/// </summary>
	public static DependencyProperty WidthProperty { get; } =
		DependencyProperty.Register(
			nameof(Width),
			typeof(GridLength),
			typeof(TableViewColumn),
			// ValueHelper<winrt::GridLength>::BoxValueIfNecessary(c_widthDefault)
			new FrameworkPropertyMetadata(c_widthDefault, OnPropertyChanged));

	private static void OnPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TableViewColumn)sender;
		owner.OnPropertyChanged(args);
	}
}
