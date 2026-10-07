// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewCellEditEndingEventArgs.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

// ----- Editing event args -----
// Cancel is read synchronously, right after the handler returns, so a handler must decide before
// returning. Asynchronous validation would need a deferral, which is not in this release.

/// <summary>
/// Provides data for the <see cref="TableView.CellEditEnding"/> event.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public sealed partial class TableViewCellEditEndingEventArgs
{
	internal TableViewCellEditEndingEventArgs(
		object? item,
		TableViewColumn? column,
		TableViewEditAction editAction)
	{
		m_item = item;
		m_column = column;
		m_editAction = editAction;
	}

	/// <summary>
	/// Gets the data item of the row whose cell edit is closing.
	/// </summary>
	public object? Item => m_item;

	/// <summary>
	/// Gets the column of the cell whose edit is closing.
	/// </summary>
	public TableViewColumn? Column => m_column;

	/// <summary>
	/// Gets how the edit is being closed.
	/// </summary>
	public TableViewEditAction EditAction => m_editAction;

	// Set to true to keep the edit open. Read synchronously once the event returns: there is no
	// deferral in this release, so a handler must decide before it returns.
	/// <summary>
	/// Gets or sets a value that indicates whether the edit should stay open.
	/// </summary>
	public bool Cancel
	{
		get => m_cancel;
		set => m_cancel = value;
	}

	private readonly object? m_item;
	private readonly TableViewColumn? m_column;
	private readonly TableViewEditAction m_editAction = TableViewEditAction.Commit;
	private bool m_cancel;
}
