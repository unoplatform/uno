// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference IItemContainerGenerator2.g.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using Microsoft.UI.Xaml;

namespace DirectUI;

internal interface IItemContainerGenerator2
{
	UIElement? GenerateContainerAtIndex(int index);

	UIElement? GenerateHeaderAtGroupIndex(int index);

	bool GetContainerRecycleQueueEmpty();

	void RecycleContainer(UIElement container);

	bool TryRecycleContainer(UIElement container);

	void RecycleAllContainers();

	bool GetHeaderRecycleQueueEmpty();

	void RecycleHeader(UIElement header);

	void RecycleAllHeaders();

	bool FindRecyclingCandidate(int index);

	uint GetQueueLength();
}
