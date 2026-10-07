// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\SortIndicator\SortIndicator.idl, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

namespace Microsoft.UI.Private.Controls;

// Numerically aligned with Microsoft.UI.Xaml.Data.SortDirection so the bridge is a trivial mapping.
// They are still distinct WinRT types: a {Binding} between them silently fails, so map in code.
internal enum SortIndicatorDirection
{
	None = 0,
	Ascending = 1,
	Descending = 2,
}
