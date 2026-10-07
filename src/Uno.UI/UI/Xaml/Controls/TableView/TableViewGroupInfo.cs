// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/main, commit dc28206ea35

#nullable enable

using System.ComponentModel;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Read-only group-header projection: the binding source of a GroupHeaderTemplate.
// Mirrors ICollectionViewGroup (Key <-> Group, ItemCount <-> GroupItems.Count).
// Updated in place and raises PropertyChanged, so recycling does not re-evaluate
// every binding in the header template.
/// <summary>
/// Read-only group-header projection: the binding source of a <see cref="TableView.GroupHeaderTemplate"/>.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public sealed partial class TableViewGroupInfo : INotifyPropertyChanged
{
	// Object Key { get; };            // the GroupBy key
	// Int32 ItemCount { get; };       // members in the group
	// Int32 Level { get; };           // 0 for single-level v1
	// Boolean IsExpandable { get; };  // false when the group can't expand (e.g. empty)
	// Boolean IsExpanded { get; };

	// Culture-formatted display strings for the built-in content template.
	// Computed lazily: a template binding only Key / ItemCount pays nothing.
	// String KeyText { get; };
	// String ItemCountText { get; };
}
