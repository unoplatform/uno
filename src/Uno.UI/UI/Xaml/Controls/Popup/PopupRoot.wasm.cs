using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	internal partial class PopupRoot : Panel
	{
		/// <summary>
		/// This value is an arbitrary value for the placement of
		/// a popup below its anchor.
		/// </summary>
		private const int PopupPlacementTargetMargin = 5;

		public PopupRoot()
		{
			Background = new SolidColorBrush(Colors.Transparent);
			UpdateIsHitTestVisible();
		}

		protected override void OnChildrenChanged()
		{
			base.OnChildrenChanged();
			UpdateIsHitTestVisible();
		}

		private bool _pointerHandlerRegistered = false;

		private void UpdateIsHitTestVisible()
		{
			var anyChildren = Children.Any();
			IsHitTestVisible = anyChildren;
			if (anyChildren)
			{
				if (!_pointerHandlerRegistered)
				{
					PointerReleased += PopupRoot_PointerReleased;
					_pointerHandlerRegistered = true;
				}
			}
			else
			{
				if (_pointerHandlerRegistered)
				{
					PointerReleased -= PopupRoot_PointerReleased;
					_pointerHandlerRegistered = false;
				}
			}
		}

		private void PopupRoot_PointerReleased(object sender, Input.PointerRoutedEventArgs e)
		{
			var lastDismissablePopup = Children.Select(GetPopup)
				.LastOrDefault(p => p.IsLightDismissEnabled);

			if(lastDismissablePopup != null)
			{
				lastDismissablePopup.IsOpen = false;
			}
		}

		private static MatrixTransform _identityTransform = new MatrixTransform();

		protected override Size ArrangeOverride(Size finalSize)
		{
			foreach (var child in Children)
			{
				var desiredSize = child.DesiredSize;
				var popup = GetPopup(child);

				if (popup.Anchor is FrameworkElement)
				{
					var rect = CalculateFlyoutPlacement(popup, desiredSize);
					child.Arrange(rect);
				}
				else
				{
					var locationTransform1 = popup.TransformToVisual(this) ?? _identityTransform;
					var r1 = new Rect(new Point(popup.HorizontalOffset, popup.VerticalOffset), desiredSize);
					var rect = locationTransform1.TransformBounds(r1);
					child.Arrange(rect);
				}
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

		private Rect CalculateFlyoutPlacement(Popup popup, Size desiredSize)
		{
			// TODO-REFACTOR: This logic should be moved in the "FlyoutBase" class.

			var anchor = (FrameworkElement)popup.Anchor;
			//var anchorLocationTransform = anchor.TransformToVisual(this) ?? _identityTransform;
			//var layoutSlot = LayoutInformation.GetLayoutSlot(anchor);
			//var anchorRect = anchorLocationTransform.TransformBounds(layoutSlot);
			var anchorRect = anchor.GetBoundsRectRelativeTo(popup);
			//Console.WriteLine($"CalculateFlyoutPlacement. Anchor: {anchor}, Transform={anchorLocationTransform}, Slot={layoutSlot}");

			var visibleBounds = ApplicationView.GetForCurrentView().VisibleBounds;

			Console.WriteLine($"CalculateFlyoutPlacement. Anchor: {anchorRect}, Size={desiredSize}, VisibleBounds={visibleBounds}, Preferred Placement={popup.AnchorPlacement}");

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
			var placementsToTry = PlacementsToTry.TryGetValue(popup.AnchorPlacement, out var p)
				? p
				: new[] {popup.AnchorPlacement};

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

				Console.WriteLine($"Position with placement {placement}: {finalRect}");

				if (RectHelper.Union(visibleBounds, finalRect).Equals(visibleBounds))
				{
					break; // this placement is acceptable
				}
			}

			return finalRect;
		}

		#region Popup

		public static Popup GetPopup(DependencyObject obj)
		{
			return (Popup)obj.GetValue(PopupProperty);
		}

		public static void SetPopup(DependencyObject obj, Popup value)
		{
			obj.SetValue(PopupProperty, value);
		}

		public static readonly DependencyProperty PopupProperty =
			DependencyProperty.RegisterAttached("Popup", typeof(Popup), typeof(PopupRoot), new PropertyMetadata(null));

		#endregion
	}
}
