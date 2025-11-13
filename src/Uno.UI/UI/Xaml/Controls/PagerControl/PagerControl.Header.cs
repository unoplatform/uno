// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference PagerControl.cpp.h, tag winui3/release/1.7.3, commit 65718e2813a9

using Uno.Disposables;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class PagerControl
{
	private int m_lastSelectedPageIndex = -1;
	private int m_lastNumberOfPagesCount = 0;

	private ComboBox m_comboBox;
	private NumberBox m_numberBox;
	private ItemsRepeater m_numberPanelRepeater;
	private FrameworkElement m_selectedPageIndicator;

	private SerialDisposable m_rootGridKeyDownRevoker = new SerialDisposable();
	private SerialDisposable m_comboBoxSelectionChangedRevoker = new SerialDisposable();
	private SerialDisposable m_numberBoxValueChangedRevoker = new SerialDisposable();
	private SerialDisposable m_firstPageButtonClickRevoker = new SerialDisposable();
	private SerialDisposable m_previousPageButtonClickRevoker = new SerialDisposable();
	private SerialDisposable m_nextPageButtonClickRevoker = new SerialDisposable();
	private SerialDisposable m_lastPageButtonClickRevoker = new SerialDisposable();

	IObservableVector<object> m_comboBoxEntries;
	IObservableVector<object> m_numberPanelElements;
}
