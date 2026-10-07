// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\Generated\TableViewGroupHeader.properties.cpp, tag winui3/main, commit dc28206ea35

#nullable enable

using Uno.UI.Helpers.Boxes;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewGroupHeader
{
	/// <summary>
	/// Gets or sets a value that indicates whether the group can be expanded.
	/// </summary>
	public bool IsExpandable
	{
		get => (bool)GetValue(IsExpandableProperty);
		set => SetValue(IsExpandableProperty, value);
	}

	/// <summary>
	/// Identifies the IsExpandable dependency property.
	/// </summary>
	public static DependencyProperty IsExpandableProperty { get; } =
		DependencyProperty.Register(
			nameof(IsExpandable),
			typeof(bool),
			typeof(TableViewGroupHeader),
			new FrameworkPropertyMetadata(BoolBoxes.False, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates whether the group is expanded.
	/// </summary>
	public bool IsExpanded
	{
		get => (bool)GetValue(IsExpandedProperty);
		set => SetValue(IsExpandedProperty, value);
	}

	/// <summary>
	/// Identifies the IsExpanded dependency property.
	/// </summary>
	public static DependencyProperty IsExpandedProperty { get; } =
		DependencyProperty.Register(
			nameof(IsExpanded),
			typeof(bool),
			typeof(TableViewGroupHeader),
			new FrameworkPropertyMetadata(BoolBoxes.False, OnPropertyChanged));

	// The owning row forwards this to TableView::ToggleGroupExpansion, keeping the header
	// free of TableView plumbing and independently testable.
	/// <summary>
	/// Occurs when the user activates the group header to toggle its expansion.
	/// </summary>
	public event TypedEventHandler<TableViewGroupHeader, TableViewGroupHeaderToggleRequestedEventArgs>? ToggleRequested;

	private static void OnPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TableViewGroupHeader)sender;
		owner.OnPropertyChanged(args);
	}
}
