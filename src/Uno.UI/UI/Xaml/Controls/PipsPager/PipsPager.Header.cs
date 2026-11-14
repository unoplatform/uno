// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference PipsPager.h, tag winui3/release/1.8-stable

using System;
using System.Collections.ObjectModel;
using Uno.Disposables;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

internal sealed class PipsPagerViewItemRevokers
{
	public SerialDisposable clickRevoker = new SerialDisposable();
}

public partial class PipsPager
{
	/* Dependency property for pip buttons revokers */
	private static DependencyProperty s_pipButtonHandlersProperty { get; } =
		DependencyProperty.RegisterAttached(
			s_pipButtonHandlersPropertyName,
			typeof(PipsPagerViewItemRevokers),
			typeof(PipsPager),
			new FrameworkPropertyMetadata(null));

	/* Refs */
	private ItemsRepeater m_pipsPagerRepeater;
	private ScrollViewer m_pipsPagerScrollViewer;
	private Button m_previousPageButton;
	private Button m_nextPageButton;

	/* Revokers */
	private SerialDisposable m_previousPageButtonClickRevoker = new SerialDisposable();
	private SerialDisposable m_nextPageButtonClickRevoker = new SerialDisposable();
	private SerialDisposable m_pipsPagerElementPreparedRevoker = new SerialDisposable();
	private SerialDisposable m_pipsAreaGettingFocusRevoker = new SerialDisposable();
	private SerialDisposable m_pipsAreaBringIntoViewRequestedRevoker = new SerialDisposable();
	private SerialDisposable m_scrollViewerBringIntoViewRequestedRevoker = new SerialDisposable();
	private SerialDisposable m_itemsRepeaterStackLayoutChangedRevoker = new SerialDisposable();

	/* Items */
	private ObservableCollection<int> m_pipsPagerItems;

	/* Additional variables class variables*/
	private Size m_defaultPipSize = new Size(0.0, 0.0);
	private Size m_selectedPipSize = new Size(0.0, 0.0);
	private int m_lastSelectedPageIndex = -1;
	private bool m_isPointerOver = false;
	private bool m_isFocused = false;
	private WeakReference<StackLayout> m_itemsRepeaterStackLayout;
	private bool m_cachedIsVirtualizationEnabledFlag = true;
}
