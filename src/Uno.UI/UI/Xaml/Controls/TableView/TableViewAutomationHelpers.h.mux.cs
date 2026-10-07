// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewAutomationHelpers.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System.Collections.Generic;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Shared helpers for the TableView automation peers, so the visible-column and column-header-string
// logic lives in one place instead of being copy-pasted across the peer translation units. Assumes
// pch.h (winrt type aliases) is included first, per the TableView header convention.
// TODO Uno: free inline functions in C++; they live in a static class here.
internal static class TableViewAutomationHelpers
{
	internal static bool IsVisibleColumn(TableViewColumn? column) =>
		column is not null && column.Visibility == Visibility.Visible;

	internal static int CountVisibleColumns(IList<TableViewColumn> columns)
	{
		var count = 0;
		foreach (var column in columns)
		{
			if (IsVisibleColumn(column))
			{
				++count;
			}
		}
		return count;
	}

	// Returns the column Header's string form (an IStringable, or a String-typed IPropertyValue), or
	// nullopt when the header is not a string (or the column is null). Returning nullopt rather than an
	// empty string lets callers distinguish "no string header" from "an explicitly empty string header".
	internal static string? TryGetColumnHeaderString(TableViewColumn? column)
	{
		if (column is not null)
		{
			var header = column.Header;
			if (SharedHelpers.IsStringable(header))
			{
				return SharedHelpers.StringableToString(header);
			}
			// TODO Uno: IPropertyValue projection
			if (header is not null)
			{
				if (ValueConversionHelpers.GetPropertyType(header.GetType()) == PropertyType.String)
				{
					return (string)header;
				}
			}
		}
		return null;
	}
}
