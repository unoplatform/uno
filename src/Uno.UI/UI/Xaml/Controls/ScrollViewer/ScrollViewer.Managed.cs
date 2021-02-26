#nullable enable
#if UNO_HAS_MANAGED_SCROLL_PRESENTER
using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.UI;
using Uno;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	public partial class ScrollViewer
	{
		internal Size ScrollBarSize => (_presenter as ScrollContentPresenter)?.ScrollBarSize ?? default;

		[NotImplemented]
		public Color BackgroundColor
		{
			get => throw new NotImplementedException();
			set => global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ScrollContentPresenter", "Color ScrollContentPresenter.BackgroundColor");
		}

		[NotImplemented]
		private void UpdateZoomedContentAlignment()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ScrollContentPresenter", "float ZoomFactor");
		}

		partial void ChangeViewScroll(double? horizontalOffset, double? verticalOffset, bool disableAnimation)
		{
			(_presenter as ScrollContentPresenter)?.Set(horizontalOffset, verticalOffset, disableAnimation: disableAnimation);
		}

		#region Over scroll support
		/// <summary>
		/// Trim excess scroll, which can be present if the content size is reduced.
		/// </summary>
		partial void TrimOverscroll(Orientation orientation)
		{
			if (_presenter is ContentPresenter presenter && presenter.Content is FrameworkElement presenterContent)
			{
				var presenterViewportSize = GetActualExtent(presenter, orientation);
				var contentExtent = GetActualExtent(presenterContent, orientation);
				var offset = GetOffsetForOrientation(orientation);
				var viewportEnd = offset + presenterViewportSize;
				var overscroll = contentExtent - viewportEnd;
				if (offset > 0 && overscroll < -0.5)
				{
					ChangeViewForOrientation(orientation, overscroll);
				}
			}
		}

		private double GetOffsetForOrientation(Orientation orientation)
			=> orientation == Orientation.Horizontal
				? HorizontalOffset
				: VerticalOffset;

		private static double GetActualExtent(FrameworkElement element, Orientation orientation)
			=> orientation == Orientation.Horizontal
				? element.ActualWidth
				: element.ActualHeight;

		private void ChangeViewForOrientation(Orientation orientation, double scrollAdjustment)
		{
			if (orientation == Orientation.Vertical)
			{
				ChangeView(null, VerticalOffset + scrollAdjustment, null, disableAnimation: true);
			}
			else
			{
				ChangeView(HorizontalOffset + scrollAdjustment, null, null, disableAnimation: true);
			}
		}
		#endregion
	}
}
#endif
