// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewSource.idl, tag winui3/main, commit 79209e041

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

/// <summary>
/// Shapes (filters, groups, sorts and nests) a collection for display in a <see cref="TableView"/>.
/// </summary>
/// <remarks>
/// <para>Null items, predicate, or key selector throws <see cref="ArgumentException"/>. This differs from
/// <see cref="TableView"/>'s command surface, which no-ops on bad input.</para>
/// <para>Like any XAML items source, a TableViewSource is UI-thread affine. The underlying collection must also raise
/// its change notifications on that thread. The verbs hold no lock.</para>
/// <para>Rows are identified by their item's object identity; there is no app-supplied row key. Selection and focus
/// re-anchor across a reshape, but not across an item being re-created, and the same object in two rows fails fast at
/// materialization.</para>
/// </remarks>
[global::Windows.Foundation.Metadata.Experimental]
public sealed partial class TableViewSource
{
}
