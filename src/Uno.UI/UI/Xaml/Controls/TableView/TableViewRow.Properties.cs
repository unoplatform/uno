// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\Generated\TableViewRow.properties.cpp, tag winui3/main, commit 79209e041

#nullable enable

using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewRow
{
	/// <summary>
	/// Gets a value that indicates whether the row is the selected row.
	/// </summary>
	public bool IsSelected
	{
		get => (bool)GetValue(IsSelectedProperty);
		internal set => SetValue(IsSelectedProperty, value);
	}

	/// <summary>
	/// Identifies the IsSelected dependency property.
	/// </summary>
	public static DependencyProperty IsSelectedProperty { get; } =
		DependencyProperty.Register(
			nameof(IsSelected),
			typeof(bool),
			typeof(TableViewRow),
			new FrameworkPropertyMetadata(BoolBoxes.False));

	/// <summary>
	/// Gets the depth of the row in a hierarchical source. The value is 0 for flat and grouped sources.
	/// </summary>
	public int Level
	{
		get => (int)GetValue(LevelProperty);
		internal set => SetValue(LevelProperty, value);
	}

	/// <summary>
	/// Identifies the Level dependency property.
	/// </summary>
	public static DependencyProperty LevelProperty { get; } =
		DependencyProperty.Register(
			nameof(Level),
			typeof(int),
			typeof(TableViewRow),
			new FrameworkPropertyMetadata(0));

	/// <summary>
	/// Gets a value that indicates whether the row can be expanded.
	/// </summary>
	public bool IsExpandable
	{
		get => (bool)GetValue(IsExpandableProperty);
		internal set => SetValue(IsExpandableProperty, value);
	}

	/// <summary>
	/// Identifies the IsExpandable dependency property.
	/// </summary>
	public static DependencyProperty IsExpandableProperty { get; } =
		DependencyProperty.Register(
			nameof(IsExpandable),
			typeof(bool),
			typeof(TableViewRow),
			new FrameworkPropertyMetadata(BoolBoxes.False));

	/// <summary>
	/// Gets a value that indicates whether the row is expanded.
	/// </summary>
	public bool IsExpanded
	{
		get => (bool)GetValue(IsExpandedProperty);
		internal set => SetValue(IsExpandedProperty, value);
	}

	/// <summary>
	/// Identifies the IsExpanded dependency property.
	/// </summary>
	public static DependencyProperty IsExpandedProperty { get; } =
		DependencyProperty.Register(
			nameof(IsExpanded),
			typeof(bool),
			typeof(TableViewRow),
			new FrameworkPropertyMetadata(BoolBoxes.False));
}
