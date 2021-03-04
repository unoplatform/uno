using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.UI;
using Windows.UI.Core;
using Uno.Extensions;
using Microsoft.Extensions.Logging;

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
		private static readonly Dictionary<FlyoutBase.MajorPlacementMode, Memory<FlyoutBase.MajorPlacementMode>> PlacementsToTry =
			new Dictionary<FlyoutBase.MajorPlacementMode, Memory<FlyoutBase.MajorPlacementMode>>()
			{
				 {FlyoutBase.MajorPlacementMode.Top, new []
				 {
					 FlyoutBase.MajorPlacementMode.Top,
					 FlyoutBase.MajorPlacementMode.Bottom,
					 FlyoutBase.MajorPlacementMode.Left,
					 FlyoutBase.MajorPlacementMode.Right,
					 FlyoutBase.MajorPlacementMode.Top // use preferred choice if no others fit
				 }},
				 {FlyoutBase.MajorPlacementMode.Bottom, new []
				 {
					 FlyoutBase.MajorPlacementMode.Bottom,
					 FlyoutBase.MajorPlacementMode.Top,
					 FlyoutBase.MajorPlacementMode.Left,
					 FlyoutBase.MajorPlacementMode.Right,
					 FlyoutBase.MajorPlacementMode.Bottom // use preferred choice if no others fit
				 }},
				 {FlyoutBase.MajorPlacementMode.Left, new []
				 {
					 FlyoutBase.MajorPlacementMode.Left,
					 FlyoutBase.MajorPlacementMode.Right,
					 FlyoutBase.MajorPlacementMode.Top,
					 FlyoutBase.MajorPlacementMode.Bottom,
					 FlyoutBase.MajorPlacementMode.Left // use preferred choice if no others fit
				 }},
				 {FlyoutBase.MajorPlacementMode.Right, new []
				 {
					 FlyoutBase.MajorPlacementMode.Right,
					 FlyoutBase.MajorPlacementMode.Left,
					 FlyoutBase.MajorPlacementMode.Top,
					 FlyoutBase.MajorPlacementMode.Bottom,
					 FlyoutBase.MajorPlacementMode.Right // use preferred choice if no others fit
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

		private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
			=> InvalidateMeasure();

		protected abstract FlyoutPlacementMode PopupPlacement { get; }

		protected abstract FrameworkElement AnchorControl { get; }

		protected abstract Point? PositionInAnchorControl { get; }

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

			if (PositionInAnchorControl is Point point)
			{
				return new Rect(
					x: anchorRect.X + point.X,
					y: anchorRect.Y + point.Y,
					width: desiredSize.Width,
					height: desiredSize.Height);
			}

			// Try all placements...
			var preferredPlacement = FlyoutBase.GetMajorPlacementFromPlacement(PopupPlacement);
			var placementsToTry = PlacementsToTry.TryGetValue(preferredPlacement, out var p)
				? p
				: new[] { preferredPlacement };

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Calculating actual placement for preferredPlacement={preferredPlacement} with justification={FlyoutBase.GetJustificationFromPlacementMode(PopupPlacement)} from PopupPlacement={PopupPlacement}, for desiredSize={desiredSize}, maxSize={maxSize}");
			}

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
					case FlyoutBase.MajorPlacementMode.Top:
						finalPosition = new Point(
							x: anchorRect.Left + halfAnchorWidth - halfChildWidth,
							y: anchorRect.Top - PopupPlacementTargetMargin - desiredSize.Height);
						break;
					case FlyoutBase.MajorPlacementMode.Bottom:
						finalPosition = new Point(
							x: anchorRect.Left + halfAnchorWidth - halfChildWidth,
							y: anchorRect.Bottom + PopupPlacementTargetMargin);
						break;
					case FlyoutBase.MajorPlacementMode.Left:
						finalPosition = new Point(
							x: anchorRect.Left - PopupPlacementTargetMargin - desiredSize.Width,
							y: anchorRect.Top + halfAnchorHeight - halfChildHeight);
						break;
					case FlyoutBase.MajorPlacementMode.Right:
						finalPosition = new Point(
							x: anchorRect.Right + PopupPlacementTargetMargin,
							y: anchorRect.Top + halfAnchorHeight - halfChildHeight);
						break;
					case FlyoutBase.MajorPlacementMode.Full:
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

				var justification = FlyoutBase.GetJustificationFromPlacementMode(PopupPlacement);

				var fits = true;
				if (PopupPlacement != FlyoutPlacementMode.Full)
				{
					if (IsPlacementModeVertical(placement))
					{
						var controlXPos = finalPosition.X;
						fits = TestAndCenterAlignWithinLimits(
							anchorRect.X,
							anchorRect.Width,
							desiredSize.Width,
							visibleBounds.Left,
							visibleBounds.Right,
							justification,
							ref controlXPos
						);
						finalPosition.X = controlXPos;
					}
					else
					{
						var controlYPos = finalPosition.Y;
						fits = TestAndCenterAlignWithinLimits(
							anchorRect.Y,
							anchorRect.Height,
							desiredSize.Height,
							visibleBounds.Top,
							visibleBounds.Bottom,
							justification,
							ref controlYPos
						);
						finalPosition.Y = controlYPos;
					}
				}

				finalRect = new Rect(finalPosition, desiredSize);

				if (fits && RectHelper.Union(visibleBounds, finalRect).Equals(visibleBounds))
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().LogDebug($"Accepted placement {placement} (choice {i}) with finalRect={finalRect} in visibleBounds={visibleBounds}");
					}
					break; // this placement is acceptable
				}
			}

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Calculated placement, finalRect={finalRect}");
			}
			return finalRect;
		}

		// Return true if placement is along vertical axis, false otherwise.
		private static bool IsPlacementModeVertical(
			FlyoutBase.MajorPlacementMode placementMode)
		{
			// We are safe even if placementMode is Full. because the case for placementMode is Full has already been put in another if branch in function PerformPlacement.
			// if necessary, we can add another function : IsPlacementModeHorizontal
			return (placementMode == FlyoutBase.MajorPlacementMode.Top ||
					placementMode == FlyoutBase.MajorPlacementMode.Bottom);
		}

		// Align centers of anchor and control while keeping control coordinates within limits.
		private static bool TestAndCenterAlignWithinLimits(
			double anchorPos,
			double anchorSize,
			double controlSize,
			double lowLimit,
			double highLimit,
			FlyoutBase.PreferredJustification justification,
			ref double controlPos
		)
		{
			bool fits = true;

			if (anchorSize == 0 && typeof(PlacementPopupPanel).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(PlacementPopupPanel).Log().LogWarning($"{nameof(anchorSize)} is 0");
			}

			if (controlSize == 0 && typeof(PlacementPopupPanel).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(PlacementPopupPanel).Log().LogWarning($"{nameof(anchorSize)} is 0");
			}

			if ((highLimit - lowLimit) > controlSize &&
				anchorSize >= 0.0 &&
				controlSize >= 0.0)
			{

				if (justification == FlyoutBase.PreferredJustification.Center)
				{
					controlPos = anchorPos + 0.5 * (anchorSize - controlSize);
				}
				else if (justification == FlyoutBase.PreferredJustification.Top || justification == FlyoutBase.PreferredJustification.Left)
				{
					controlPos = anchorPos;
				}
				else if (justification == FlyoutBase.PreferredJustification.Bottom || justification == FlyoutBase.PreferredJustification.Right)
				{
					controlPos = anchorPos + (anchorSize - controlSize);
				}
				else
				{
					throw new InvalidOperationException("Unsupported FlyoutBase.PreferredJustification");
				}

				if (controlPos < lowLimit)
				{
					controlPos = lowLimit;
				}
				else if (controlPos + controlSize > highLimit)
				{
					controlPos = highLimit - controlSize;
				}
			}
			else
			{
				controlPos = lowLimit;
				fits = false;
			}

			return fits;
		}
	}
}
