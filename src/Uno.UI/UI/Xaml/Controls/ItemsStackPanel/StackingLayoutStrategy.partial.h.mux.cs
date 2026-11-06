// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference StackingLayoutStrategy_Partial.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

internal partial class StackingLayoutStrategy : ILayoutStrategy
{
	// internal configuration
	public void SetVirtualizationDirection(Orientation direction) { _layoutStrategyImpl.VirtualizationDirection = direction; }
	public void SetGroupPadding(Thickness padding) { _layoutStrategyImpl.GroupPadding = padding; }

	public Thickness GroupPadding
	{
		get => _layoutStrategyImpl.GroupPadding;
		set => _layoutStrategyImpl.GroupPadding = value;
	}

	// Used to estimate tracked element reposition during items source updates.
	public float GetAverageHeaderSize() { return _layoutStrategyImpl.GetAverageHeaderSize(); }
	public float GetAverageContainerSize() { return _layoutStrategyImpl.GetAverageContainerSize(); }

	// This is where the actual implementation of the layout strategy exists,
	private readonly StackingLayoutStrategyImpl _layoutStrategyImpl = new();

	private ILayoutDataInfoProvider? _spDataInfoProvider;
}
