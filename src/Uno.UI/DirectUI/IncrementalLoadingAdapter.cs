// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference IncrementalLoadingAdapter.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using Microsoft.UI.Xaml.Data;

namespace DirectUI;

internal static class IncrementalLoadingAdapter
{
	internal static bool SupportsIncrementalLoading(object? source)
		=> source is ISupportIncrementalLoading or ICollectionView;
}
