// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewBeginningEditEventArgs.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

/// <summary>
/// Provides data for the <see cref="TableView.BeginningEdit"/> event.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public sealed partial class TableViewBeginningEditEventArgs
{
	internal TableViewBeginningEditEventArgs(
		object? item,
		TableViewColumn? column)
	{
		m_item = item;
		m_column = column;
	}

	/// <summary>
	/// Gets the data item of the row whose cell is about to be edited.
	/// </summary>
	public object? Item => m_item;

	/// <summary>
	/// Gets the column of the cell that is about to be edited.
	/// </summary>
	public TableViewColumn? Column => m_column;

	/// <summary>
	/// Gets or sets a value that indicates whether the edit should be prevented.
	/// </summary>
	public bool Cancel
	{
		get => m_cancel;
		set => m_cancel = value;
	}

	private readonly object? m_item;
	private readonly TableViewColumn? m_column;
	private bool m_cancel;
}
