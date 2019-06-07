using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls
{
	//[PortStatus(Complete = true)]
	public partial class RatingControl
	{
		[PortStatus(Complete = true)]
		private TextBlock m_captionTextBlock;

		[PortStatus(Complete = true)]
		private CompositionPropertySet m_sharedPointerPropertySet = null;

		[PortStatus(Complete = true)]
		StackPanel m_backgroundStackPanel;

		[PortStatus(Complete = true)]
		StackPanel m_foregroundStackPanel;

		[PortStatus(Complete = true)]
		bool m_isPointerOver = false;

		[PortStatus(Complete = true)]
		bool m_isPointerDown = false;

		[PortStatus(Complete = true)]
		double m_mousePercentage = 0.0;

		[PortStatus(Complete = true)]
		RatingInfoType m_infoType = RatingInfoType.Font;

		[PortStatus(Complete = true)]
		double m_preEngagementValue = 0.0; // Holds the value of the Rating control at the moment of engagement, used to handle cancel-disengagements where we reset the value.

		[PortStatus(Complete = true)]
		bool m_disengagedWithA = false;

		[PortStatus(Complete = true)]
		bool m_shouldDiscardValue = true;

		// event_token m_pointerCancelledToken;
		// event_token m_pointerCaptureLostToken;
		// event_token m_pointerMovedToken;
		// event_token m_pointerEnteredToken;
		// event_token m_pointerExitedToken;
		// event_token m_pointerPressedToken;
		// event_token m_pointerReleasedToken;
		// event_token m_captionSizeChangedToken;
		// event_token m_fontFamilyChangedToken;

		// UISettings::TextScaleFactorChanged_revoker m_textScaleChangedRevoker = ;
		// static UISettings GetUISettings();

		// DispatcherHelper m_dispatcherHelper = *this ;
	}
}
