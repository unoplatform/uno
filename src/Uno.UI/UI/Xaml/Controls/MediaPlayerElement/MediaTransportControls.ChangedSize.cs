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
		private void OnSizeChanged(SizeChangedEventArgs args)
		{
			var width = args.NewSize.Width;
			if (m_transportControlsEnabled)
			{
				SetMeasureCommandBar(width);
			}
		}

		private void SetMeasureCommandBar(double availableSize)
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
						// !IsButtonCollapsedbySystem(spElement.Get())) ??
						if (spElement.Visibility == Visibility.Collapsed)
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
	}
}
