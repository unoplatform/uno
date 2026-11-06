// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ICustomGeneratorItemsHost.g.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Collections;

namespace DirectUI;

internal interface ICustomGeneratorItemsHost
{
	void RegisterItemsHost(IGeneratorHost host);

	void DisconnectItemsHost();

	IItemContainerMapping GetItemContainerMapping();

	IGroupHeaderMapping GetGroupHeaderMapping();

	void Refresh();

	void NotifyOfItemsChanging(IVectorChangedEventArgs e);

	void NotifyOfItemsChanged(IVectorChangedEventArgs e);

	void NotifyOfItemsReordered(uint count);

	IContainerContentChangingIterator GetContainersForIncrementalVisualization();
}
