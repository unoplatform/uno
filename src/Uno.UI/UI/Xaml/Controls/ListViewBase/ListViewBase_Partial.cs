// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using DirectUI;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml.Controls;

//  Abstract:
//      ListViewBase displays a rich, interactive collection of items.
partial class ListViewBase
{
	/// <summary>
	/// Used to inform the data source of the items it is tracking
	/// in this function, we collect the data
	/// </summary>
	private void InitializeDataSourceSelectionInfo()
	{
#if false // Uno specific: fix setting a new ItemsSource not cleaning up previous ItemsSource
		if (ItemsSource is ISelectionInfo spItemsSourceAsSI)
		{
			DataSourceAsSelectionInfo = spItemsSourceAsSI;
		}
#else
		DataSourceAsSelectionInfo = ItemsSource as ISelectionInfo;
#endif
	}
}
