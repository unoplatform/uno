// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference WrappingLayoutStrategy_Partial.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

internal partial class WrappingLayoutStrategy : ILayoutStrategy
{
	//public:

	// internal configuration
	public void SetVirtualizationDirection(Orientation direction) { _layoutStrategyImpl.VirtualizationDirection = direction; }
	public void SetGroupPadding(Thickness padding) { _layoutStrategyImpl.GroupPadding = padding; }
	public void SetMaximumRowsOrColumns(int maxRowsOrColumns) { _layoutStrategyImpl.SetMaximumRowsOrColumns(maxRowsOrColumns); }
	public void SetItemWidth(double itemWidth) { _layoutStrategyImpl.SetItemWidth(itemWidth); }
	public void SetItemHeight(double itemHeight) { _layoutStrategyImpl.SetItemHeight(itemHeight); }

	//private:
	private WrappingLayoutStrategyImpl _layoutStrategyImpl = new();

	private ILayoutDataInfoProvider? _spDataInfoProvider;
}
