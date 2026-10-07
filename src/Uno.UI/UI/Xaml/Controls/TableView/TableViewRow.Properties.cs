// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\Generated\TableViewRow.properties.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewRow
{
	// Read-only DP written only by the owning TableView (Register + a setter that is not
	// projected, NOT RegisterReadOnly). Drives the Selected* states.
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
}
