// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewSortingEventArgs.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

/// <summary>
/// Provides data for the <see cref="TableView.Sorting"/> event.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public sealed partial class TableViewSortingEventArgs
{
	internal TableViewSortingEventArgs(
		TableViewColumn? column,
		SortDirection direction)
	{
		m_column = column;
		m_direction = direction;
	}

	// The trigger column. Null is the clear-all sentinel used by ClearSort.
	/// <summary>
	/// Gets the column that triggered the sort, or null when all sorting is being cleared.
	/// </summary>
	public TableViewColumn? Column => m_column;

	// The direction the control is about to apply.
	/// <summary>
	/// Gets the direction the control is about to apply.
	/// </summary>
	public SortDirection Direction => m_direction;

	// Set true to stop the control applying the sort, leaving the app to own the ordering. The
	// control's sort state, header glyph and Sorted event are all suppressed, so an app that
	// orders the rows itself and wants the glyph to follow sets the column's state explicitly.
	/// <summary>
	/// Gets or sets a value that indicates whether the control should not apply the sort.
	/// </summary>
	public bool Cancel
	{
		get => m_cancel;
		set => m_cancel = value;
	}

	private readonly TableViewColumn? m_column;
	private readonly SortDirection m_direction = SortDirection.None;
	private bool m_cancel;
}
