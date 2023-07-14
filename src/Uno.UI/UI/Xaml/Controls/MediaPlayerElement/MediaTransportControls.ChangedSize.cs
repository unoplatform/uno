using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	public partial class MediaTransportControls
	{
		private void OnSizeChanged(SizeChangedEventArgs args) // todo: maybe adjust event source
		{
			if (m_transportControlsEnabled)
			{
				//SetMeasureCommandBar(default);
				MeasureCommandBar();
			}
		}

		private void SetMeasureCommandBar(double availableSize) // todo: replace
		{
			var infiniteBounds = new Size(double.PositiveInfinity, double.PositiveInfinity);
			if (m_tpCommandBar is not { })
			{
				return;
			}
			m_tpCommandBar.Measure(infiniteBounds);
			var desiredSize = m_tpCommandBar.DesiredSize;

			var spPrimaryButtons = m_tpCommandBar.PrimaryCommands.ToArray();

			if (availableSize < desiredSize.Width)
			{
				Dropout(availableSize, desiredSize);
			}
			else
			{
				Expand(availableSize, desiredSize);
			}
		}

		/// <summary>
		/// Measure CommandBar to fit the buttons in given width.
		/// </summary>
		private void MeasureCommandBar()
		{
			if (m_tpCommandBar is { })
			{
#if !HAS_UNO
				ResetMargins();
#endif
				var availableSize = this.ActualWidth;
				m_tpCommandBar.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				var desiredSize = m_tpCommandBar.DesiredSize;

				if (availableSize < desiredSize.Width)
				{
					Dropout(availableSize, desiredSize);
				}
				else
				{
					Expand(availableSize, desiredSize);
#if !HAS_UNO
					AddMarginsBetweenGroups();
#endif
				}

#if !HAS_UNO
				// Remove this code to disable and hide only after Deliverable 19012797: Fullscreen media works in ApplicationWindow and Win32 XAML Islands is complete
				// since Expand or Dropout can make the full window button visible again, this code is used to hide it again
				CContentRoot* contentRoot = VisualTree::GetContentRootForElement(GetHandle());
				if (contentRoot->GetType() == CContentRoot::Type::XamlIsland)
				{
					IFC(m_tpFullWindowButton.Cast<ButtonBase>()->put_Visibility(xaml::Visibility_Collapsed));
				}
#endif
			}
		}

		private void Dropout(double availableSize, Size desiredSize)
		{
			var spPrimaryButtons = m_tpCommandBar.PrimaryCommands.ToArray();
			var buttonsCount = spPrimaryButtons.Length;
			var infiniteBounds = new Size(double.PositiveInfinity, double.PositiveInfinity);

			while (availableSize < desiredSize.Width)
			{
				var lowestVisibleOrder = int.MaxValue;
				int lowestElementIndex = 0;

				for (int i = 0; i < buttonsCount; i++)
				{
					var spCommandElement = spPrimaryButtons[i];

					if (spCommandElement as UIElement is { } spElement)
					{
						if (spElement.Visibility == Visibility.Visible)
						{
							var spOrder = MediaTransportControlsHelper.GetDropoutOrder(spElement);

							if (spOrder is { } order && order > 0 && lowestVisibleOrder > order)
							{
								lowestVisibleOrder = order;
								lowestElementIndex = i;
							}
						}
					}
				}
				if (lowestVisibleOrder == int.MaxValue)
				{
					break;
				}
				else
				{
					var spCommandElement = spPrimaryButtons[lowestElementIndex];
					if (spCommandElement as UIElement is { } spElement)
					{
						spElement.Visibility = Visibility.Collapsed;
					}
					m_tpCommandBar.Measure(infiniteBounds);
					desiredSize = m_tpCommandBar.DesiredSize;
				}
			}
		}
		private void Expand(double availableSize, Size desiredSize)
		{
			var spPrimaryButtons = m_tpCommandBar.PrimaryCommands.ToArray();
			var buttonsCount = spPrimaryButtons.Length;
			var infiniteBounds = new Size(double.PositiveInfinity, double.PositiveInfinity);

			while (availableSize > desiredSize.Width)
			{
				var highestCollapseOrder = -1;
				int highestElementIndex = 0;

				for (int i = 0; i < buttonsCount; i++)
				{
					var spCommandElement = spPrimaryButtons[i];
					if (spCommandElement as UIElement is { } spElement)
					{
						if (spElement.Visibility == Visibility.Collapsed && !IsButtonCollapsedbySystem(spElement))
						{
							var spOrder = MediaTransportControlsHelper.GetDropoutOrder(spElement);

							if (spOrder is { } order && order > highestCollapseOrder)
							{
								highestCollapseOrder = order;
								highestElementIndex = i;
							}
						}
					}
				}
				if (highestCollapseOrder == -1)
				{
					break;
				}
				else
				{
					var spCommandElement = spPrimaryButtons[highestElementIndex];

					// Make sure it should be complete space but not partial space to fit the button
					if (spCommandElement as UIElement is { } spElement &&
						spElement as FrameworkElement is { } frameworkElement &&
						availableSize >= (desiredSize.Width + frameworkElement.Width))
					{
						spElement.Visibility = Visibility.Visible;
						m_tpCommandBar.Measure(infiniteBounds);
						desiredSize = m_tpCommandBar.DesiredSize;
					}
					else
					{
						break;
					}
				}
			}
		}

		/// <summary>
		/// Determine whether button collapsed by system.
		/// </summary>
		private bool IsButtonCollapsedbySystem(UIElement element)
		{
			//In case of Compact mode this button should collapse
			if (element == m_tpPlayPauseButton && IsCompact)
			{
				return true;
			}
			//In case of the Missing Audio tracks this button should collapse
			else if (element == m_tpTHAudioTrackSelectionButton
#if !HAS_UNO
				&& IsCompact) {&& !m_hasMultipleAudioStreams
#endif
				)
			{
				return true;
			}
			//In case of the Missing CC tracks this button should collapse
			else if (element == m_tpCCSelectionButton
#if !HAS_UNO
				&& !m_hasCCTracks
#endif
				)
			{
				return true;
			}
			//Remaining check whether thru APIs Button is collapsed.
			else if (element == m_tpPlaybackRateButton)
			{
				return !IsPlaybackRateButtonVisible;
			}
			else if (element == m_tpTHVolumeButton)
			{
				return !IsVolumeButtonVisible;
			}
			else if (element == m_tpFullWindowButton)
			{
				return !IsFullWindowButtonVisible;
			}
			else if (element == m_tpZoomButton)
			{
				return !IsZoomButtonVisible;
			}
			else if (element == m_tpFastForwardButton)
			{
				return !IsFastForwardButtonVisible;
			}
			else if (element == m_tpFastRewindButton)
			{
				return !IsFastRewindButtonVisible;
			}
			else if (element == m_tpStopButton)
			{
				return !IsStopButtonVisible;
			}
			// In case of the Cast doesn't supports this button should always collapse
			else if (element == m_tpCastButton
#if !HAS_UNO
				 && !m_isCastSupports
#endif
				)
			{
				return true;
			}
			else if (element == m_tpSkipForwardButton)
			{
				return !IsSkipForwardButtonVisible;
			}
			else if (element == m_tpSkipBackwardButton)
			{
				return !IsSkipBackwardButtonVisible;
			}
			else if (element == m_tpNextTrackButton)
			{
				return !IsNextTrackButtonVisible;
			}
			else if (element == m_tpPreviousTrackButton)
			{
				return !IsPreviousTrackButtonVisible;
			}
			else if (element == m_tpRepeatButton)
			{
				return !IsRepeatButtonVisible;
			}
			else if (element == m_tpCompactOverlayButton)
			{
				return !IsCompactOverlayButtonVisible;
			}
			return false;
		}
	}
}
