using System;
using Microsoft.UI.Xaml.Controls;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Uno.UI.Xaml.Core;

internal partial class RootScrollViewer : ScrollViewer
{
	private void SetRootScrollViewerAllowImplicitStyle()
	{
		m_isAllowImplicitStyle = true;
	}

	internal void SetRootScrollContentPresenter(ScrollContentPresenter pScrollContentPresenter)
	{
		Presenter = null;
		Presenter = pScrollContentPresenter;
	}

	private protected override bool IsRootScrollViewer => true;

	private protected override bool IsRootScrollViewerAllowImplicitStyle => m_isAllowImplicitStyle;

	private protected override bool IsInputPaneShow => m_isInputPaneShow;

#pragma warning disable CS0649
	// Indicates the root ScrollViewer for InputPane
	private bool m_isInputPaneShow;
	private bool m_isInputPaneTransit;
	private bool m_isInputPaneTransitionCompleted;
	private bool m_isAllowImplicitStyle;

	// InputPane offset variables to restore the original scroll position when InputPane is closed
	private double m_preInputPaneOffsetX;
	private double m_preInputPaneOffsetY;
	private double m_postInputPaneOffsetX;
	private double m_postInputPaneOffsetY;
}
