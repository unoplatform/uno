// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewGroupHeaderToggleRequestedEventArgs.h, tag winui3/main, commit dc28206ea35

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

/// <summary>
/// Provides data for the <see cref="TableViewGroupHeader.ToggleRequested"/> event.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public sealed partial class TableViewGroupHeaderToggleRequestedEventArgs
{
	internal TableViewGroupHeaderToggleRequestedEventArgs(object? groupKey)
	{
		m_groupKey = groupKey;
	}

	// Carried on the args so a handler that re-enters and mutates the header
	// still sees the key that was actually activated.
	/// <summary>
	/// Gets the key of the group whose header was activated.
	/// </summary>
	public object? GroupKey => m_groupKey;

	private readonly object? m_groupKey;
}
