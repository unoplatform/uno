using System;
using System.Linq;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	public partial class MediaTransportControls
	{
//		private void OnSizeChanged(object sender, SizeChangedEventArgs e) // todo: maybe adjust event source
//		{
//			if (m_transportControlsEnabled)
//			{
//				SetMeasureCommandBar();
//#if !HAS_UNO
//			// If error is showing, may need to switch between long / short / shortest form
//			IFC(UpdateErrorUI());
//			IFC(UpdateVisualState());
//#endif
//			}
//#if !HAS_UNO
//			// This is arise when clicks the exit fullscreen screen on the title bar, then reset back from the full window
//			if (m_isFullWindow && m_isFullScreen)
//			{
//				ctl::ComPtr<wuv::IApplicationView3> spAppView3;
//				BOOLEAN fullscreenmode = FALSE;

//				IFC(GetFullScreenView(&spAppView3));
//				if (spAppView3)
//				{
//					IFC(spAppView3->get_IsFullScreenMode(&fullscreenmode));
//				}
//				if (!fullscreenmode)
//				{
//					if (!m_isFullScreenPending) // if true means still we are not under fullscreen, exit through titlebar doesn't occur still
//					{
//						if (!m_isMiniView)
//						{
//							IFC(OnFullWindowClick());
//						}
//						else
//						{
//							// While switching from Fullscreen to MiniView, just update the fullscren states.
//							IFC(UpdateFullWindowUI());
//							m_isFullScreen = FALSE;
//						}
//					}
//				}
//				else
//				{
//					// m_isFullScreenPending Complete.
//					m_isFullScreenPending = FALSE;

//					// Find out if the API is available (currently behind a velocity key)
//					ctl::ComPtr<wf::Metadata::IApiInformationStatics> apiInformationStatics;
//					IFC(ctl::GetActivationFactory(
//						wrl_wrappers::HStringReference(RuntimeClass_Windows_Foundation_Metadata_ApiInformation).Get(),
//						&apiInformationStatics));

//					// we are in full screen, so check for spanning mode
//					uint32_t regionCount = 0;

//					boolean isPresent = false;
//					IFC(apiInformationStatics->IsMethodPresent(
//						wrl_wrappers::HStringReference(L"Windows.UI.ViewManagement.ApplicationView").Get(),
//						wrl_wrappers::HStringReference(L"GetDisplayRegions").Get(),
//						&isPresent));

//					if (isPresent)
//					{
//						// Get regions for current view
//						ctl::ComPtr<wuv::IApplicationViewStatics2> applicationViewStatics;
//						IFC(ctl::GetActivationFactory(wrl_wrappers::HStringReference(
//																RuntimeClass_Windows_UI_ViewManagement_ApplicationView)
//																.Get(),
//																&applicationViewStatics));

//						ctl::ComPtr<wuv::IApplicationView> applicationView;

//						// Get Display Regions doesn't work on Win32 Apps, because there is no
//						// application view. For the time being, just don't return an empty vector
//						// when running in an unsupported mode.
//						if (SUCCEEDED(applicationViewStatics->GetForCurrentView(&applicationView)))
//						{
//							ctl::ComPtr<wuv::IApplicationView9> applicationView9;
//							IFC(applicationView.As(&applicationView9));

//							HRESULT hrGetForCurrentView;
//							ctl::ComPtr<wfc::IVectorView<wuwm::DisplayRegion*>> regions;
//							hrGetForCurrentView = applicationView9->GetDisplayRegions(&regions);
//							if (FAILED(hrGetForCurrentView))
//							{
//								// bug 14084372: APIs currently return a failure when there is only one display region.
//								return S_OK;
//							}

//							IFC(regions->get_Size(&regionCount));
//						}
//					}

//					if (regionCount > 1 &&
//						!m_isCompact &&
//						!m_isSpanningCompactEnabled)
//					{
//						put_IsCompact(true);
//						m_isSpanningCompactEnabled = TRUE;
//					}
//				}
//			}
//			else
//			{
//				// not fullscreen, in spanning compact mode is enabled, reset it
//				if (m_isSpanningCompactEnabled)
//				{
//					put_IsCompact(false);
//					m_isSpanningCompactEnabled = FALSE;
//				}
//			}

//		Cleanup:
//#endif
//		}

//		private void SetMeasureCommandBar()
//		{
//			_ = this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, MeasureCommandBar);
//		}

//		/// <summary>
//		/// Measure CommandBar to fit the buttons in given width.
//		/// </summary>
//		private void MeasureCommandBar()
//		{
//			if (m_tpCommandBar is { })
//			{
//				ResetMargins();

//				var availableSize = this.ActualWidth;
//				m_tpCommandBar.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
//				var desiredSize = m_tpCommandBar.DesiredSize;

//				if (availableSize < desiredSize.Width)
//				{
//					Dropout(availableSize, desiredSize);
//				}
//				else
//				{
//					Expand(availableSize, desiredSize);
//					AddMarginsBetweenGroups();
//				}

//#if !HAS_UNO
//				// Remove this code to disable and hide only after Deliverable 19012797: Fullscreen media works in ApplicationWindow and Win32 XAML Islands is complete
//				// since Expand or Dropout can make the full window button visible again, this code is used to hide it again
//				CContentRoot* contentRoot = VisualTree::GetContentRootForElement(GetHandle());
//				if (contentRoot->GetType() == CContentRoot::Type::XamlIsland)
//				{
//					IFC(m_tpFullWindowButton.Cast<ButtonBase>()->put_Visibility(xaml::Visibility_Collapsed));
//				}
//#endif
//			}
//		}

//		private void AddMarginsBetweenGroups()
//		{
//			var isCompact = IsCompact;

//			if ((m_tpLeftAppBarSeparator is { } || m_tpRightAppBarSeparator is { }) && !isCompact)
//			{
//				double leftWidth = 0;
//				double middleWidth = 0;
//				double rightWidth = 0;
//				bool leftComplete = false;
//				bool rightStart = false;


//				var totalWidth = this.ActualWidth;
//				var spPrimaryCommandObsVec = m_tpCommandBar.PrimaryCommands;
//				var spPrimaryButtons = spPrimaryCommandObsVec.ToArray();

//				for (int i = 0; i < spPrimaryButtons.Length; i++)
//				{
//					var spCommandElement = spPrimaryButtons[i];

//					if (spCommandElement as UIElement is { } spElement)
//					{
//						if (spElement.Visibility == Visibility.Visible)
//						{
//							if (spElement == m_tpLeftAppBarSeparator)
//							{
//								leftComplete = true;
//								continue;
//							}
//							if (spElement == m_tpRightAppBarSeparator)
//							{
//								rightStart = true;
//								continue;
//							}
//						}

//						if (spElement as FrameworkElement is { } spFrmElement)
//						{
//							var width = spFrmElement.Width;

//							if (!leftComplete)
//							{
//								leftWidth = leftWidth + width;
//							}
//							else if (!rightStart)
//							{
//								middleWidth = middleWidth + width;
//							}
//							else
//							{
//								rightWidth = rightWidth + width;
//							}
//						}
//					}
//				}

//				var cmdMargin = new Thickness(0);

//				// Consider control panel margin for xbox case
//				if (m_tpControlPanelGrid is { })
//				{
//					m_tpControlPanelGrid.Margin(cmdMargin);
//				}

//				double leftGap = (totalWidth / 2) - (cmdMargin.Left + leftWidth + (middleWidth / 2));
//				double rightGap = (totalWidth / 2) - (cmdMargin.Right + rightWidth + (middleWidth / 2));
//				// If we get negative value, means they are not in equal balance
//				if (leftGap < 0 || rightGap < 0)
//				{
//					leftGap = rightGap = (totalWidth - (leftWidth + middleWidth + rightWidth)) / 2;
//				}

//				if (m_tpLeftAppBarSeparator is { })
//				{
//					var extraMargin = new Thickness(leftGap / 2, 0, leftGap / 2, 0);
//					m_tpLeftAppBarSeparator.Margin(extraMargin);
//				}
//				if (m_tpRightAppBarSeparator is { })
//				{
//					var extraMargin = new Thickness(rightGap / 2, 0, rightGap / 2, 0);
//					m_tpRightAppBarSeparator.Margin(extraMargin);
//				}
//			}
//		}

//		private void ResetMargins()
//		{
//			var zeroMargin = new Thickness(0);

//			if (m_tpLeftAppBarSeparator is { })
//			{
//				m_tpLeftAppBarSeparator.Margin = zeroMargin;
//			}
//			if (m_tpRightAppBarSeparator is { })
//			{
//				m_tpRightAppBarSeparator.Margin = zeroMargin;
//			}
//		}

//		private void Dropout(double availableSize, Size desiredSize)
//		{
//			var spPrimaryButtons = m_tpCommandBar.PrimaryCommands.ToArray();
//			var buttonsCount = spPrimaryButtons.Length;
//			var infiniteBounds = new Size(double.PositiveInfinity, double.PositiveInfinity);

//			while (availableSize < desiredSize.Width)
//			{
//				var lowestVisibleOrder = int.MaxValue;
//				int lowestElementIndex = 0;

//				for (int i = 0; i < buttonsCount; i++)
//				{
//					var spCommandElement = spPrimaryButtons[i];

//					if (spCommandElement as UIElement is { } spElement)
//					{
//						if (spElement.Visibility == Visibility.Visible)
//						{
//							var spOrder = MediaTransportControlsHelper.GetDropoutOrder(spElement);

//							if (spOrder is { } order && order > 0 && lowestVisibleOrder > order)
//							{
//								lowestVisibleOrder = order;
//								lowestElementIndex = i;
//							}
//						}
//					}
//				}
//				if (lowestVisibleOrder == int.MaxValue)
//				{
//					break;
//				}
//				else
//				{
//					var spCommandElement = spPrimaryButtons[lowestElementIndex];
//					if (spCommandElement as UIElement is { } spElement)
//					{
//						spElement.Visibility = Visibility.Collapsed;
//					}
//					m_tpCommandBar.Measure(infiniteBounds);
//					desiredSize = m_tpCommandBar.DesiredSize;
//				}
//			}
//		}
//		private void Expand(double availableSize, Size desiredSize)
//		{
//			var spPrimaryButtons = m_tpCommandBar.PrimaryCommands.ToArray();
//			var buttonsCount = spPrimaryButtons.Length;
//			var infiniteBounds = new Size(double.PositiveInfinity, double.PositiveInfinity);

//			while (availableSize > desiredSize.Width)
//			{
//				var highestCollapseOrder = -1;
//				int highestElementIndex = 0;

//				for (int i = 0; i < buttonsCount; i++)
//				{
//					var spCommandElement = spPrimaryButtons[i];
//					if (spCommandElement as UIElement is { } spElement)
//					{
//						if (spElement.Visibility == Visibility.Collapsed && !IsButtonCollapsedbySystem(spElement))
//						{
//							var spOrder = MediaTransportControlsHelper.GetDropoutOrder(spElement);

//							if (spOrder is { } order && order > highestCollapseOrder)
//							{
//								highestCollapseOrder = order;
//								highestElementIndex = i;
//							}
//						}
//					}
//				}
//				if (highestCollapseOrder == -1)
//				{
//					break;
//				}
//				else
//				{
//					var spCommandElement = spPrimaryButtons[highestElementIndex];

//					// Make sure it should be complete space but not partial space to fit the button
//					if (spCommandElement as UIElement is { } spElement &&
//						spElement as FrameworkElement is { } frameworkElement &&
//						availableSize >= (desiredSize.Width + frameworkElement.Width))
//					{
//						spElement.Visibility = Visibility.Visible;
//						m_tpCommandBar.Measure(infiniteBounds);
//						desiredSize = m_tpCommandBar.DesiredSize;
//					}
//					else
//					{
//						break;
//					}
//				}
//			}
//		}

//		/// <summary>
//		/// Determine whether button collapsed by system.
//		/// </summary>
//		private bool IsButtonCollapsedbySystem(UIElement element)
//		{
//			//In case of Compact mode this button should collapse
//			if (element == m_tpPlayPauseButton && IsCompact)
//			{
//				return true;
//			}
//			//In case of the Missing Audio tracks this button should collapse
//			else if (element == m_tpTHAudioTrackSelectionButton
//#if !HAS_UNO
//				&& IsCompact) {&& !m_hasMultipleAudioStreams
//#endif
//				)
//			{
//				return true;
//			}
//			//In case of the Missing CC tracks this button should collapse
//			else if (element == m_tpCCSelectionButton
//#if !HAS_UNO
//				&& !m_hasCCTracks
//#endif
//				)
//			{
//				return true;
//			}
//			//Remaining check whether thru APIs Button is collapsed.
//			else if (element == m_tpPlaybackRateButton)
//			{
//				return !IsPlaybackRateButtonVisible;
//			}
//			else if (element == m_tpTHVolumeButton)
//			{
//				return !IsVolumeButtonVisible;
//			}
//			else if (element == m_tpFullWindowButton)
//			{
//				return !IsFullWindowButtonVisible;
//			}
//			else if (element == m_tpZoomButton)
//			{
//				return !IsZoomButtonVisible;
//			}
//			else if (element == m_tpFastForwardButton)
//			{
//				return !IsFastForwardButtonVisible;
//			}
//			else if (element == m_tpFastRewindButton)
//			{
//				return !IsFastRewindButtonVisible;
//			}
//			else if (element == m_tpStopButton)
//			{
//				return !IsStopButtonVisible;
//			}
//			// In case of the Cast doesn't supports this button should always collapse
//			else if (element == m_tpCastButton
//#if !HAS_UNO
//				 && !m_isCastSupports
//#endif
//				)
//			{
//				return true;
//			}
//			else if (element == m_tpSkipForwardButton)
//			{
//				return !IsSkipForwardButtonVisible;
//			}
//			else if (element == m_tpSkipBackwardButton)
//			{
//				return !IsSkipBackwardButtonVisible;
//			}
//			else if (element == m_tpNextTrackButton)
//			{
//				return !IsNextTrackButtonVisible;
//			}
//			else if (element == m_tpPreviousTrackButton)
//			{
//				return !IsPreviousTrackButtonVisible;
//			}
//			else if (element == m_tpRepeatButton)
//			{
//				return !IsRepeatButtonVisible;
//			}
//			else if (element == m_tpCompactOverlayButton)
//			{
//				return !IsCompactOverlayButtonVisible;
//			}
//			return false;
//		}
	}
}
