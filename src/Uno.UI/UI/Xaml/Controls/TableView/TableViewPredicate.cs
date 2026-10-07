// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewSource.idl, tag winui3/main, commit dc28206ea35

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

/// <summary>
/// Represents the filter predicate of a <see cref="TableViewSource"/>.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public delegate bool TableViewPredicate(object? item);
