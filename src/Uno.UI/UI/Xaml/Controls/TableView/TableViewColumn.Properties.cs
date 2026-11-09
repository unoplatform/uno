// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\Generated\TableViewColumn.properties.cpp, tag winui3/main, commit 79209e041

#nullable enable

using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewColumn
{
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

	/// <summary>
	/// Gets or sets the style applied to this column's header. It takes precedence over <see cref="TableView.ColumnHeaderStyle"/>.
	/// </summary>
	public Style? HeaderStyle
	{
		get => (Style?)GetValue(HeaderStyleProperty);
		set => SetValue(HeaderStyleProperty, value);
	}

	/// <summary>
	/// Identifies the HeaderStyle dependency property.
	/// </summary>
	public static DependencyProperty HeaderStyleProperty { get; } =
		DependencyProperty.Register(
			nameof(HeaderStyle),
			typeof(Style),
			typeof(TableViewColumn),
			new FrameworkPropertyMetadata(default(Style), OnPropertyChanged));

	/// <summary>
	/// Gets or sets the flyout shown when a context menu is requested on one of this column's cells.
	/// </summary>
	public FlyoutBase? CellContextFlyout
	{
		get => (FlyoutBase?)GetValue(CellContextFlyoutProperty);
		set => SetValue(CellContextFlyoutProperty, value);
	}

	/// <summary>
	/// Identifies the CellContextFlyout dependency property.
	/// </summary>
	public static DependencyProperty CellContextFlyoutProperty { get; } =
		DependencyProperty.Register(
			nameof(CellContextFlyout),
			typeof(FlyoutBase),
			typeof(TableViewColumn),
			new FrameworkPropertyMetadata(default(FlyoutBase)));

	/// <summary>
	/// Gets or sets the flyout shown when a context menu is requested on this column's header.
	/// </summary>
	public FlyoutBase? HeaderContextFlyout
	{
		get => (FlyoutBase?)GetValue(HeaderContextFlyoutProperty);
		set => SetValue(HeaderContextFlyoutProperty, value);
	}

	/// <summary>
	/// Identifies the HeaderContextFlyout dependency property.
	/// </summary>
	public static DependencyProperty HeaderContextFlyoutProperty { get; } =
		DependencyProperty.Register(
			nameof(HeaderContextFlyout),
			typeof(FlyoutBase),
			typeof(TableViewColumn),
			new FrameworkPropertyMetadata(default(FlyoutBase)));

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
