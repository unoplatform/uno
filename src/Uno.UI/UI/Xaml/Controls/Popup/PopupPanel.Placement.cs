using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Uno.UI;
using Uno.Foundation.Logging;
using System.Linq;


#if HAS_UNO_WINUI
using WindowSizeChangedEventArgs = Microsoft.UI.Xaml.WindowSizeChangedEventArgs;
using XamlWindow = Microsoft.UI.Xaml.Window;
#else
using WindowSizeChangedEventArgs = Windows.UI.Core.WindowSizeChangedEventArgs;
#endif

namespace Windows.UI.Xaml.Controls.Primitives;

partial class PopupPanel
{
	// This is a base popup panel to calculate the placement near an anchor control.
	// This class exists mostly to reuse the same logic between a Flyout and a ToolTip.
	// This class should eventually be removed, and Uno should match WinUI's approach, where Flyout sets Popup.HorizontalOffset and VerticalOffset
	// as well as Width and Height on FlyoutPresenter when it opens, and then allows the popup layouting to do its job.

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

	private void Window_SizeChanged(object sender, WindowSizeChangedEventArgs e)
		=> InvalidateMeasure();

	// TODO: Use this whenever popup placement is Auto
	protected virtual PopupPlacementMode DefaultPopupPlacement { get; }

	protected virtual bool FullPlacementRequested { get; }

	protected virtual Point? PositionInAnchorControl { get; }

	internal virtual FlyoutBase Flyout => null;

	private Size PlacementArrangeOverride(Size finalSize)
	{
		foreach (var child in Children.OfType<Popup>())
		{
			var desiredSize = child.DesiredSize;
			var maxSize = (child as FrameworkElement).GetMaxSize(); // UWP takes FlyoutPresenter's MaxHeight and MaxWidth into consideration, but ignores Height and Width
			var rect = CalculatePopupPlacement(child, desiredSize, maxSize);

			if (Flyout?.IsTargetPositionSet ?? false)
			{
				rect = Flyout.UpdateTargetPosition(ApplicationView.GetForCurrentView().VisibleBounds, desiredSize, rect);
			}

			child.Arrange(rect);
		}

		return finalSize;
	}

	private Rect? GetAnchorRect()
	{
#if __ANDROID__ || __IOS__
		if (NativeAnchor != null)
		{
			return NativeAnchor.GetBoundsRectRelativeTo(this);
		}
#endif
		var anchor = Children.OfType<Popup>().FirstOrDefault()?.PlacementTarget;
		if (anchor == null)
		{
			return default;
		}

		return anchor.GetBoundsRectRelativeTo(this);
	}

	protected virtual Rect CalculatePopupPlacement(Popup popup, Size desiredSize, Size maxSize)
	{
		if (!(GetAnchorRect() is { } anchorRect))
		{
			return default;
		}

		var visibleBounds = ApplicationView.GetForCurrentView().VisibleBounds;

		// Make sure the desiredSize fits in the panel
		desiredSize.Width = Math.Min(desiredSize.Width, visibleBounds.Width);
		desiredSize.Height = Math.Min(desiredSize.Height, visibleBounds.Height);

		// Try all placements...
		var preferredPlacement = FlyoutBase.GetMajorPlacementFromPlacement(popup.DesiredPlacement, FullPlacementRequested);
		var placementsToTry = PlacementsToTry.TryGetValue(preferredPlacement, out var p)
			? p
			: new[] { preferredPlacement };

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().LogDebug($"Calculating actual placement for preferredPlacement={preferredPlacement} with justification={FlyoutBase.GetJustificationFromPlacementMode(popup.DesiredPlacement, FullPlacementRequested)} from PopupPlacement={popup.DesiredPlacement}, for desiredSize={desiredSize}, maxSize={maxSize}");
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
#if __IOS__ || __ANDROID__
					// The status bar should remain visible. On droid, this panel is placed beneath the status bar.
					desiredSize = new Size(
						ActualWidth,
						ActualHeight
#if __IOS__
						// On iOS, this panel will cover the status bar, so we have to substract it out.
						- visibleBounds.Y
#endif
					).AtMost(maxSize);
#else
					desiredSize = visibleBounds.Size.AtMost(maxSize);
#endif
					finalPosition = new Point(
						x: FindOptimalOffset(desiredSize.Width, visibleBounds.X, visibleBounds.Width, ActualWidth),
						y: FindOptimalOffset(desiredSize.Height, visibleBounds.Y, visibleBounds.Height, ActualHeight));

					double FindOptimalOffset(double length, double visibleOffset, double visibleLength, double constraint)
					{
						// Center the flyout inside the first area that fits: within visible bounds, below status bar, the screen
						if (visibleLength >= length)
						{
							return visibleOffset + (visibleLength - length) / 2;
						}
						if (constraint - visibleOffset >= length)
						{
							return visibleOffset + ((constraint - visibleOffset) - length) / 2;
						}
						else
						{
							return (constraint - length) / 2;
						}
					}
					break;
				default: // Other unsupported placements
					finalPosition = new Point(
						x: (visibleBounds.Width - desiredSize.Width) / 2.0,
						y: (visibleBounds.Height - desiredSize.Height) / 2.0);
					break;
			}

			var justification = FlyoutBase.GetJustificationFromPlacementMode(popup.DesiredPlacement, FullPlacementRequested);

			var fits = true;
			if (!FullPlacementRequested)
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

		// Ensure the popup is not positioned fully beyond visible bounds.
		// This can happen for example when the user is trying to show a flyout at
		// Window.Current.Content - which will match Top position, which would be outside
		// the visible bounds (negative Y).

		if (finalRect.Bottom < visibleBounds.Top)
		{
			finalRect = new Rect(
				finalRect.Left,
				visibleBounds.Top,
				finalRect.Width,
				finalRect.Height);
		}

		if (finalRect.Top > visibleBounds.Bottom)
		{
			finalRect = new Rect(
				finalRect.Left,
				visibleBounds.Bottom - finalRect.Height,
				finalRect.Width,
				finalRect.Height);
		}

		if (finalRect.Right < visibleBounds.Left)
		{
			finalRect = new Rect(
				visibleBounds.Left,
				finalRect.Top,
				finalRect.Width,
				finalRect.Height);
		}

		if (finalRect.Left > visibleBounds.Right)
		{
			finalRect = new Rect(
				visibleBounds.Right - finalRect.Width,
				finalRect.Top,
				finalRect.Width,
				finalRect.Height);
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

		if (anchorSize == 0 && typeof(PopupPanel).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(PopupPanel).Log().LogWarning($"{nameof(anchorSize)} is 0");
		}

		if (controlSize == 0 && typeof(PopupPanel).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(PopupPanel).Log().LogWarning($"{nameof(anchorSize)} is 0");
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
