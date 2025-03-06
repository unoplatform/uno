using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class AppBar : IHasLightDismissOverlay
{
	private void UnregisterEvents()
	{
		m_contentRootSizeChangedEventHandler.Disposable = null;
		m_xamlRootChangedEventHandler.Disposable = null;
		m_expandButtonClickEventHandler.Disposable = null;
		m_displayModeStateChangedEventHandler.Disposable = null;
		m_overlayElementPointerPressedEventHandler.Disposable = null;

		m_tpLayoutRoot = null;
		m_tpContentRoot = null;
		m_tpExpandButton = null;
		m_tpDisplayModesStateGroup = null;

		m_overlayClosingStoryboard = null;
		m_overlayOpeningStoryboard = null;
	}
}
