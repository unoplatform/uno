#nullable enable
#if UNO_HAS_MANAGED_SCROLL_PRESENTER
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.UI;
using Uno;
using Uno.UI;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollViewer
	{
		public bool IsScrollInertiaEnabled
		{
			get => (bool)GetValue(IsScrollInertiaEnabledProperty);
			set => SetValue(IsScrollInertiaEnabledProperty, value);
		}

		public static DependencyProperty IsScrollInertiaEnabledProperty { get; } =
			DependencyProperty.RegisterAttached(
				nameof(IsScrollInertiaEnabled),
				typeof(bool),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(true));

		public static bool GetIsScrollInertiaEnabled(DependencyObject element) =>
			(bool)element.GetValue(IsScrollInertiaEnabledProperty);

		public static void SetIsScrollInertiaEnabled(DependencyObject element, bool isScrollInertiaEnabled) =>
			element.SetValue(IsScrollInertiaEnabledProperty, isScrollInertiaEnabled);

		internal Size ScrollBarSize => (_presenter as ScrollContentPresenter)?.ScrollBarSize ?? default;

		private bool ChangeViewNative(double? horizontalOffset, double? verticalOffset, double? zoomFactor, bool disableAnimation)
			=> (_presenter as ScrollContentPresenter)?.Set(horizontalOffset, verticalOffset, disableAnimation: disableAnimation) ?? true;

		private partial void OnLoadedPartial() { }
		private partial void OnUnloadedPartial() { }

		#region Over scroll support
		/// <summary>
		/// Trim excess scroll, which can be present if the content size is reduced.
		/// </summary>
		partial void TrimOverscroll(Orientation orientation)
		{
			if (_presenter is not null)
			{
				var (contentExtent, presenterViewportSize, offset) = orientation switch
				{
					Orientation.Vertical => (ExtentHeight, ViewportHeight, VerticalOffset),
					_ => (ExtentWidth, ViewportWidth, HorizontalOffset),
				};
				var viewportEnd = offset + presenterViewportSize;
				var overscroll = contentExtent - viewportEnd;
				if (offset > 0 && overscroll < -0.5)
				{
					ChangeViewForOrientation(orientation, overscroll);
				}
			}
		}

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
