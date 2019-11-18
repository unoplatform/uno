using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// This is a base popup panel to calculate the placement near an anchor control.
	/// </summary>
	/// <remarks>
	/// This class exists mostly to reuse the same logic between a Flyout and a ToolTip
	/// </remarks>
	internal abstract partial class PlacementPopupPanel : PopupPanel
	{
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

		/// <summary>
		/// This value is an arbitrary value for the placement of
		/// a popup below its anchor.
		/// </summary>
		protected virtual int PopupPlacementTargetMargin => 5;

		protected PlacementPopupPanel(Popup popup) : base(popup)
		{
			Loaded += (s, e) => Windows.UI.Xaml.Window.Current.SizeChanged += Current_SizeChanged;
			Unloaded += (s, e) => Windows.UI.Xaml.Window.Current.SizeChanged -= Current_SizeChanged;
		}

		private void Current_SizeChanged(object sender, Core.WindowSizeChangedEventArgs e)
			=> InvalidateMeasure();

		protected abstract FlyoutPlacementMode PopupPlacement { get; }

		protected abstract FrameworkElement AnchorControl { get; }

		protected override Size ArrangeOverride(Size finalSize)
		{
			foreach (var child in Children)
			{
				if (!(child is UIElement elem))
				{
					continue;
				}

				var desiredSize = elem.DesiredSize;
				var maxSize = (elem as FrameworkElement).GetMaxSize(); // UWP takes FlyoutPresenter's MaxHeight and MaxWidth into consideration, but ignores Height and Width
				var rect = CalculateFlyoutPlacement(desiredSize, maxSize);
				elem.Arrange(rect);
			}

			return finalSize;
		}

		protected virtual Rect CalculateFlyoutPlacement(Size desiredSize, Size maxSize)
		{
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

			// Try all placements...
			var placementsToTry = PlacementsToTry.TryGetValue(PopupPlacement, out var p)
				? p
				: new[] { PopupPlacement };

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
							x: Math.Max(anchorRect.Left + halfAnchorWidth - halfChildWidth, 0d),
							y: Math.Max(anchorRect.Top - PopupPlacementTargetMargin - desiredSize.Height, 0d));
						break;
					case FlyoutPlacementMode.Bottom:
						finalPosition = new Point(
							x: Math.Max(anchorRect.Left + halfAnchorWidth - halfChildWidth, 0d),
							y: anchorRect.Bottom + PopupPlacementTargetMargin);
						break;
					case FlyoutPlacementMode.Left:
						finalPosition = new Point(
							x: Math.Max(anchorRect.Left - PopupPlacementTargetMargin - desiredSize.Width, 0d),
							y: Math.Max(anchorRect.Top + halfAnchorHeight - halfChildHeight, 0d));
						break;
					case FlyoutPlacementMode.Right:
						finalPosition = new Point(
							x: anchorRect.Right + PopupPlacementTargetMargin,
							y: Math.Max(anchorRect.Top + halfAnchorHeight - halfChildHeight, 0d));
						break;
					case FlyoutPlacementMode.Full:
						desiredSize = visibleBounds.Size.AtMost(maxSize);
						finalPosition = new Point(
							x: (visibleBounds.Width - desiredSize.Width) / 2.0,
							y: (visibleBounds.Height - desiredSize.Height) / 2.0);
						break;
					default: // Other unsupported placements
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
