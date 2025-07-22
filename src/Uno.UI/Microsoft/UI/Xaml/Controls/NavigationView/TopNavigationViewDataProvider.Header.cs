// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TopNavigationViewDataProvider.h, commit 65718e2813

using System;
using System.Collections.Specialized;

namespace Microsoft.UI.Xaml.Controls;

internal enum NavigationViewSplitVectorID
{
	NotInitialized = 0,
	PrimaryList = 1,
	OverflowList = 2,
	SkippedList = 3,
	Size = 4
};

internal partial class TopNavigationViewDataProvider
{
	private ItemsSourceView m_dataSource;
	private object m_rawDataSource;

	private Action<NotifyCollectionChangedEventArgs> m_dataChangeCallback;
	private double m_overflowButtonCachedWidth;
}
