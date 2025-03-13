using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Uno.UI.Xaml.Core;

internal partial class RootScrollViewer
{
	private void NotifyInputPaneStateChange(InputPaneState inputPaneState, Rect inputPaneBounds)
	{
		// TODO: MZ:
//		ApplicationBarService applicationBarService;
//		FlyoutMetadata spFlyoutMetadata;
//		IFlyoutBase spOpenFlyout;
//		FrameworkElement spOpenFlyoutPlacementTarget;
//		XamlRoot xamlRoot = XamlRoot.GetForElement(this);
//		bool isInputPaneShow = inputPaneState != InputPaneState.Hidden;

//		if (inputPaneState == InputPaneState.Showing || inputPaneState == InputPaneState.Hidden)
//		{
//			if (m_trElementScrollContentPresenter is ScrollContentPresenter scrollContentPresenter)
//			{
//				scrollContentPresenter.NotifyInputPaneStateChange(isInputPaneShow);
//			}

//			if (isInputPaneShow && !m_isInputPaneShow)
//			{
//				m_preInputPaneOffsetX = HorizontalOffset;
//				m_preInputPaneOffsetY = VerticalOffset;
//			}

//			if (isInputPaneShow)
//			{
//				// Set VerticalScrollMode/HorizontalScrollMode to Enabled so the content can be scrolled while the input pane is shown.
//				VerticalScrollMode = ScrollMode.Enabled;
//				HorizontalScrollMode = ScrollMode.Enabled;
//#if DBG
//            // IsVerticalRailEnabled/IsHorizontalRailEnabled are expected to be True
//            bool isRailEnabled = false;
//            get_IsVerticalRailEnabled(&isRailEnabled);
//            MUX_ASSERT(isRailEnabled);
//            get_IsHorizontalRailEnabled(&isRailEnabled);
//            MUX_ASSERT(isRailEnabled);
//#endif // DBG
//			}
//			else
//			{
//				// Disabled the ScrollMode when IHM is closed
//				VerticalScrollMode = ScrollMode.Disabled;
//				HorizontalScrollMode = ScrollMode.Disabled;
//			}

//			if (xamlRoot is not null)
//			{
//				xamlRoot.TryGetApplicationBarService(applicationBarService);

//				if (applicationBarService)
//				{
//					applicationBarService.OnBoundsChanged(true);
//				}
//			}
//		}

//		// Ensure Flyout size and position properly with showing/hiding IHM.
//		DXamlCore.GetCurrent().GetFlyoutMetadata(&spFlyoutMetadata);
//		MUX_ASSERT(spFlyoutMetadata != null);
//		spFlyoutMetadata.GetOpenFlyout(&spOpenFlyout, &spOpenFlyoutPlacementTarget);
//		if (spOpenFlyout)
//		{
//			spOpenFlyout.Cast<FlyoutBase>().NotifyInputPaneStateChange(inputPaneState, inputPaneBounds);
//		}

		//m_isInputPaneShow = isInputPaneShow;
	}

	/// <summary>
	/// Called to let the peer know that InputPane ready to transit. 
	/// </summary>
	private void ApplyInputPaneTransition(bool isInputPaneTransitionEnabled)
	{
		m_isInputPaneTransit = isInputPaneTransitionEnabled;

		// TODO: MZ:
		//if (m_trElementScrollContentPresenter is ScrollContentPresenter scrollContentPresenter)
		//{
		//	scrollContentPresenter.ApplyInputPaneTransition(isInputPaneTransitionEnabled);
		//}

		// InputPane is hiding
		if (!m_isInputPaneShow)
		{
			bool isOffsetUpdated = false;

			// Restore the scroll horizontal and vertical offset
			double xOffset = HorizontalOffset;
			double yOffset = VerticalOffset;

			if (m_preInputPaneOffsetX != xOffset && m_postInputPaneOffsetX == xOffset)
			{
				HandleHorizontalScroll(ScrollEventType.ThumbPosition, m_preInputPaneOffsetX);
				isOffsetUpdated = true;
			}
			if (m_preInputPaneOffsetY != yOffset && m_postInputPaneOffsetY == yOffset)
			{
				HandleVerticalScroll(ScrollEventType.ThumbPosition, m_preInputPaneOffsetY);
				isOffsetUpdated = true;
			}
			if (isOffsetUpdated)
			{
				UpdateLayout();
			}
		}
	}

	// Update the InputPane horizontal offset.
	private protected override void UpdateInputPaneOffsetX()
	{
		if (m_isInputPaneTransit)
		{
			m_postInputPaneOffsetX = HorizontalOffset;
			m_isInputPaneTransitionCompleted = true;
		}
	}

	// Update the InputPane vertical offset.
	private protected override void UpdateInputPaneOffsetY()
	{
		if (m_isInputPaneTransit)
		{
			m_postInputPaneOffsetY = VerticalOffset;
			m_isInputPaneTransitionCompleted = true;
		}
	}

	// Update the InputPane transition status
	private protected override void UpdateInputPaneTransition()
	{
		if (m_isInputPaneTransitionCompleted)
		{
			m_isInputPaneTransit = false;
			m_isInputPaneTransitionCompleted = false;
		}
	}

	// RootScrollViewer prevent to show the root ScrollViewer automation peer.
	protected override AutomationPeer OnCreateAutomationPeer() => null;
}
