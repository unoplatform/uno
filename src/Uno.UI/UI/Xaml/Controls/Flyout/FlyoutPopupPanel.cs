#if !__UWP__
using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	internal partial class FlyoutBasePopupPanel : PlacementPopupPanel
	{
		private readonly FlyoutBase _flyout;

		public FlyoutBasePopupPanel(FlyoutBase flyout) : base(flyout._popup)
		{
			_flyout = flyout;

			// Required for the dismiss handling
			// This should however be customized depending of the Popup.DismissMode
			Background = new SolidColorBrush(Windows.UI.Colors.Transparent);
		}

		protected override FlyoutPlacementMode PopupPlacement => _flyout.Placement;

		protected override FrameworkElement AnchorControl => _flyout.Target as FrameworkElement;

		internal FlyoutBase Flyout => _flyout;

		protected override Rect CalculateFlyoutPlacement(Size desiredSize, Size maxSize)
		{
			// Use implementation from PlacementPopupPanel unless a position is provided.
			if (!(_flyout.ShowOptions?.Position is { } position))
			{
				return base.CalculateFlyoutPlacement(desiredSize, maxSize);
			}

			var anchor = AnchorControl;
			if (anchor == null)
			{
				return default;
			}

			var visibleBounds = ApplicationView.GetForCurrentView().VisibleBounds;
			var anchorRect = anchor.GetBoundsRectRelativeTo(this);

			// Make sure the desiredSize fits in the panel
			desiredSize.Width = Math.Min(desiredSize.Width, visibleBounds.Width);
			desiredSize.Height = Math.Min(desiredSize.Height, visibleBounds.Height);

			var x = anchorRect.X + position.X;
			var y = anchorRect.Y + position.Y;

			if (PopupPlacement != FlyoutPlacementMode.Full)
			{
				var majorPlacement = FlyoutBase.GetMajorPlacementFromPlacement(PopupPlacement);
				if (majorPlacement == FlyoutBase.MajorPlacementMode.Top)
				{
					y -= desiredSize.Height;
				}
				else if (majorPlacement == FlyoutBase.MajorPlacementMode.Left)
				{
					x -= desiredSize.Width;
				}

				// Popup can overflow free to the left when left-aligned.
				// It can also overflow with justification, but its anchor point must be within visible bounds,
				// hence we split the clamp on the x-axis into 2-parts: low-bound here, high-bound later.
				if (majorPlacement != FlyoutBase.MajorPlacementMode.Left)
				{
					x = Math.Max(x, visibleBounds.Left);
				}

				// note: Justification aligns the edge of popup with the same edge of target element,
				// so we need to offset in the opposite direction
				var justification = FlyoutBase.GetJustificationFromPlacementMode(PopupPlacement);
				if (justification == FlyoutBase.PreferredJustification.Bottom)
				{
					y -= desiredSize.Height;
				}
				else if (justification == FlyoutBase.PreferredJustification.Right)
				{
					x -= desiredSize.Width;
				}
				else if (justification == FlyoutBase.PreferredJustification.Center)
				{
					// note: Justification is on the opposite axis of the major placement
					if (IsPlacementModeVertical(majorPlacement)) // horizontally
					{
						x -= desiredSize.Width / 2;
					}
					else // vertically
					{
						y -= desiredSize.Height / 2;
					}
				}
			}

			// Ensure the popup is within visible bounds. (Also, see comment above on the x-axis.)
			x = Math.Min(x, visibleBounds.Right - desiredSize.Width);
			y = MathEx.Clamp(y, visibleBounds.Top, visibleBounds.Bottom - desiredSize.Height);

			return new Rect(x, y, desiredSize.Width, desiredSize.Height);
		}
	}
}
#endif
