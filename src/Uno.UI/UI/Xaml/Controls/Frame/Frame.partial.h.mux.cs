using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectUI;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media.Animation;
using Uno.Disposables;

namespace Windows.UI.Xaml.Controls;

partial class Frame
{
	private enum NavigationStateOperation
	{
		Get,
		Set
	}

	private bool m_isInNavigate;

	private bool m_isNavigationStackEnabledForPage = true;
	private bool m_isCanceled;

	private bool m_isNavigationFromMethod;

	private bool m_isLastNavigationBack;

	private readonly SerialDisposable m_nextClick = new();

	private readonly SerialDisposable m_previousClick = new();

	private NavigationCache m_upNavigationCache;

	private NavigationHistory m_tpNavigationHistory;

	private ButtonBase m_tpNext;
	private ButtonBase m_tpPrevious;

	private NavigationTransitionInfo m_tpNavigationTransitionInfo;
}
