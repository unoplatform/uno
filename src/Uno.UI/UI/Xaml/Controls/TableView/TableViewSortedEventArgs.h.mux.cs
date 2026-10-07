// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewSortedEventArgs.h, tag winui3/main, commit dc28206ea35

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

/// <summary>
/// Provides data for the <see cref="TableView.Sorted"/> event.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public sealed partial class TableViewSortedEventArgs
{
	internal TableViewSortedEventArgs(
		TableViewColumn? column,
		SortDirection direction)
	{
		m_column = column;
		m_direction = direction;
	}

	// The trigger column for this sort-state change. Null is the clear-all sentinel.
	/// <summary>
	/// Gets the column whose sort state changed, or null when all sorting was cleared.
	/// </summary>
	public TableViewColumn? Column => m_column;

	// The direction now applied to the column.
	/// <summary>
	/// Gets the direction now applied to the column.
	/// </summary>
	public SortDirection Direction => m_direction;

	private readonly TableViewColumn? m_column;
	private readonly SortDirection m_direction = SortDirection.None;
}
