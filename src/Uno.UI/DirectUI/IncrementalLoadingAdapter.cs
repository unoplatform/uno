// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference IncrementalLoadingAdapter.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

//  Abstract:
//      Implements an adapter that makes using an ICollectionView
//      or instance of ISupportIncrementalLoading seamless.

#nullable enable

using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

namespace DirectUI;

internal sealed class IncrementalLoadingAdapter : ISupportIncrementalLoading
{
	private IncrementalLoadingAdapter(ICollectionView pCV)
	{
		m_spCV = pCV;
	}

	// ISupportIncrementalLoading interface
	public bool HasMoreItems => m_spCV.HasMoreItems;

	public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count) => m_spCV.LoadMoreItemsAsync(count);

	// Note: This method will create a new wrapper for ICollectionView everytime it is called therefore
	// you cannot use equality comparisons to find out if they represent the same object
	// TODO: If we need to compare wether two different ISupportIncrementalLoading interfaces are the same
	// that have obtained from this method then we need to do some sort of aggregation and return
	// a "cannonical" IUnknown/IInspectable for comparison.
	internal static ISupportIncrementalLoading? AsIncrementalLoading(object? pSource)
	{
		// If the source is already an incremental loading vector then we're done
		if (pSource is ISupportIncrementalLoading pResult)
		{
			return pResult;
		}

		// Try to see if this is a collection view and if so wrap it in the adapter
		// otherwise we're done
		if (pSource is ICollectionView pCV)
		{
			return new IncrementalLoadingAdapter(pCV);
		}

		return null;
	}

	internal static bool SupportsIncrementalLoading(object? pSource)
		=> pSource is ISupportIncrementalLoading or ICollectionView;

	private readonly ICollectionView m_spCV;
}
