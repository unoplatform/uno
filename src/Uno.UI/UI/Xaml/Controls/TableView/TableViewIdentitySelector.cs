// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewSource.idl, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Stable string identity for a group key (GroupBy overload). Row identity is not app-supplied:
// the projection derives it from each item's object identity.
/// <summary>
/// Represents the stable string identity selector for a group key of a <see cref="TableViewSource"/>.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public delegate string TableViewIdentitySelector(object? item);
