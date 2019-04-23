#if !__UWP__
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	internal partial class FlyoutBasePopupPanel : PopupPanel
	{
		/// <summary>
		/// This value is an arbitrary value for the placement of
		/// a popup below its anchor.
		/// </summary>
		private const int PopupPlacementTargetMargin = 5;

		private readonly FlyoutBase _flyout;

		public FlyoutBasePopupPanel(FlyoutBase flyout) : base(flyout._popup)
		{
			_flyout = flyout;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			foreach (var child in Children)
			{
				if (!(child is UIElement elem))
				{
					continue;
				}

				var desiredSize = elem.DesiredSize;
				var rect = CalculateFlyoutPlacement(desiredSize);
				elem.Arrange(rect);
			}

			return finalSize;
		}

		private static readonly Dictionary<FlyoutPlacementMode, Memory<FlyoutPlacementMode>> PlacementsToTry =
			new Dictionary<FlyoutPlacementMode, Memory<FlyoutPlacementMode>>()
			{
				{FlyoutPlacementMode.Top, new []
				{
					FlyoutPlacementMode.Top,
					FlyoutPlacementMode.Bottom,
					FlyoutPlacementMode.Left,
					FlyoutPlacementMode.Right,
					FlyoutPlacementMode.Full // last resort placement
				}},
				{FlyoutPlacementMode.Bottom, new []
				{
					FlyoutPlacementMode.Bottom,
					FlyoutPlacementMode.Top,
					FlyoutPlacementMode.Left,
					FlyoutPlacementMode.Right,
					FlyoutPlacementMode.Full // last resort placement
				}},
				{FlyoutPlacementMode.Left, new []
				{
					FlyoutPlacementMode.Left,
					FlyoutPlacementMode.Right,
					FlyoutPlacementMode.Top,
					FlyoutPlacementMode.Bottom,
					FlyoutPlacementMode.Full // last resort placement
				}},
				{FlyoutPlacementMode.Right, new []
				{
					FlyoutPlacementMode.Right,
					FlyoutPlacementMode.Left,
					FlyoutPlacementMode.Top,
					FlyoutPlacementMode.Bottom,
					FlyoutPlacementMode.Full // last resort placement
				}},
			};

		private Rect CalculateFlyoutPlacement(Size desiredSize)
		{
			var anchor = _flyout.Target as FrameworkElement;
			if (anchor == null)
			{
				return default;
			}

			var visibleBounds = ApplicationView.GetForCurrentView().VisibleBounds;
			var anchorRect = anchor.GetAbsoluteBoundsRect();

			// Make sure the desiredSize fits in visibleBounds
			if (desiredSize.Width > visibleBounds.Width)
			{
				desiredSize.Width = visibleBounds.Width;
			}
			if (desiredSize.Height > visibleBounds.Height)
			{
				desiredSize.Height = visibleBounds.Height;
			}

			// Try all placements...
			var placementsToTry = PlacementsToTry.TryGetValue(_flyout.Placement, out var p)
				? p
				: new[] { _flyout.Placement };

			var halfAnchorWidth = anchorRect.Width / 2;
			var halfAnchorHeight = anchorRect.Height / 2;
			var halfChildWidth = desiredSize.Width / 2;
			var halfChildHeight = desiredSize.Height / 2;

			var finalRect = default(Rect);

			for (var i = 0; i < placementsToTry.Length; i++)
			{
				var placement = placementsToTry.Span[i];

				Point finalPosition;

				switch (placement)
				{
					case FlyoutPlacementMode.Top:
						finalPosition = new Point(
							x: anchorRect.Left + halfAnchorWidth - halfChildWidth,
							y: anchorRect.Top - PopupPlacementTargetMargin - desiredSize.Height);
						break;
					case FlyoutPlacementMode.Bottom:
						finalPosition = new Point(
							x: anchorRect.Left + halfAnchorWidth - halfChildWidth,
							y: anchorRect.Bottom + PopupPlacementTargetMargin);
						break;
					case FlyoutPlacementMode.Left:
						finalPosition = new Point(
							x: anchorRect.Left - PopupPlacementTargetMargin - desiredSize.Width,
							y: anchorRect.Top + halfAnchorHeight - halfChildHeight);
						break;
					case FlyoutPlacementMode.Right:
						finalPosition = new Point(
							x: anchorRect.Right + PopupPlacementTargetMargin,
							y: anchorRect.Top + halfAnchorHeight - halfChildHeight);
						break;
					default: // Full + other unsupported placements
						finalPosition = new Point(
							x: (visibleBounds.Width - desiredSize.Width) / 2.0,
							y: (visibleBounds.Height - desiredSize.Height) / 2.0);
						break;
				}

				finalRect = new Rect(finalPosition, desiredSize);

				if (RectHelper.Union(visibleBounds, finalRect).Equals(visibleBounds))
				{
					break; // this placement is acceptable
				}
			}

			return finalRect;
		}
	}
}
#endif
