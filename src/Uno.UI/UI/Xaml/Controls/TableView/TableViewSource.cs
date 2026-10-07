// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewSource.idl, tag winui3/main, commit dc28206ea35

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Sealed deliberately: TableView drives the concrete implementation, so an override of a shaping
// verb would be bypassed. Unsealing later is compatible; sealing later is not.
/// <summary>
/// Shapes (filters, groups and sorts) a collection for display in a <see cref="TableView"/>.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public sealed partial class TableViewSource
{
	// Whole class:
	//  * Null items, predicate, or keySelector throws E_INVALIDARG. (Note this differs from
	//    TableView's command surface, which no-ops on bad input.) The optional
	//    groupIdentitySelector may be null and selects the built-in value-type group identity.
	//  * UI-thread affinity, like any XAML items source. The underlying collection must also
	//    raise its change notifications on that thread; raising them from a background thread
	//    is not supported. The verbs hold no lock.
	//  * Rows are identified by their item's OBJECT identity; there is no app-supplied row key.
	//    So selection and focus re-anchor across a reshape, but NOT across an item being
	//    re-created, and the same object in two rows fails fast at materialization.
}
