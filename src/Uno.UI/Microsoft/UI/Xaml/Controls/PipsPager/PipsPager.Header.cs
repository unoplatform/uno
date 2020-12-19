// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.ObjectModel;
using Uno.Disposables;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class PipsPager
	{
		/* Refs */
		private ItemsRepeater m_pipsPagerRepeater;
		private ScrollViewer m_pipsPagerScrollViewer;

		/* Revokers */
		private SerialDisposable m_previousPageButtonClickRevoker = new SerialDisposable();
		private SerialDisposable m_nextPageButtonClickRevoker = new SerialDisposable();
		private SerialDisposable m_pipsPagerElementPreparedRevoker = new SerialDisposable();

		/* Items */
		private ObservableCollection<object> m_pipsPagerItems;

		/* Additional variables class variables*/
		private Size m_defaultPipSize = new Size(0.0,0.0);
		private Size m_selectedPipSize = new Size(0.0, 0.0);
		private int m_lastSelectedPageIndex = -1;
		private bool m_isPointerOver = false;
	}
}
