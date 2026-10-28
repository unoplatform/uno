#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Uno.UI;
using Uno.Foundation.Logging;
using System.Linq;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

using WindowSizeChangedEventArgs = Microsoft.UI.Xaml.WindowSizeChangedEventArgs;
using XamlWindow = Microsoft.UI.Xaml.Window;

namespace Microsoft.UI.Xaml.Controls.Primitives;

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
	/// TODO: To be removed in favor of HorizontalOffset and VerticalOffset.
	/// </summary>
	protected virtual int PopupPlacementTargetMargin => 0;

	private void XamlRootChanged(object sender, XamlRootChangedEventArgs e)
		=> InvalidateMeasure();

	// TODO: Use this whenever popup placement is Auto
	protected virtual PopupPlacementMode DefaultPopupPlacement { get; }

	protected virtual bool FullPlacementRequested { get; }

	internal virtual FlyoutBase? Flyout => null;

	private Size PlacementArrangeOverride(Popup popup, Size finalSize)
	{
		foreach (var child in Children)
		{
			// A presenter resized to fit is still placed from its own size, so it keeps the side and the size it was given.
			var desiredSize = Flyout?.PresenterSizeBeforeResize ?? child.DesiredSize;
			var rect = CalculatePopupPlacement(popup, child, desiredSize);

			if (Flyout?.IsTargetPositionSet ?? false)
			{
				rect = Flyout.UpdateTargetPosition(GetAvailableWindowRect(popup), desiredSize, rect);
			}

			child.Arrange(rect);
		}

		return finalSize;
	}

	private Rect GetAnchorRect(Popup popup)
	{
		return popup.PlacementTarget.GetBoundsRectRelativeTo(this);
	}

	/// <summary>
	/// Where the flyout's presenter is placed for the given size, or null when this panel does not place it next to a target.
	/// </summary>
	internal Rect? CalculateFlyoutPlacement(UIElement presenter, Size presenterSize)
		=> Flyout is not null && Popup.PlacementTarget is not null
			? CalculatePopupPlacement(Popup, presenter, presenterSize)
			: default(Rect?);

	private Rect CalculatePopupPlacement(Popup popup, UIElement child, Size childDesiredSize)
	{
		var maxSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
		var minSize = default(Size);
		if (child is FrameworkElement fe)
		{
			maxSize = fe.GetMaxSize();// UWP takes FlyoutPresenter's MaxHeight and MaxWidth into consideration, but ignores Height and Width
			minSize = new Size(fe.MinWidth, fe.MinHeight);
		}

		return CalculatePopupPlacement(popup, childDesiredSize, maxSize, minSize);
	}

	protected virtual Rect CalculatePopupPlacement(Popup popup, Size childDesiredSize, Size maxSize, Size minSize)
	{
		var anchorRect = GetAnchorRect(popup);

		var visibleBounds = GetAvailableWindowRect(popup);

		// Make sure the desiredSize fits in the panel
		childDesiredSize.Width = Math.Min(childDesiredSize.Width, visibleBounds.Width);
		childDesiredSize.Height = Math.Min(childDesiredSize.Height, visibleBounds.Height);

		// Try all placements...
		var preferredPlacement = FlyoutBase.GetMajorPlacementFromPlacement(popup.DesiredPlacement, FullPlacementRequested);
		var placementsToTry = PlacementsToTry.TryGetValue(preferredPlacement, out var p)
			? p
			: new[] { preferredPlacement };

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().LogDebug($"Calculating actual placement for preferredPlacement={preferredPlacement} with justification={FlyoutBase.GetJustificationFromPlacementMode(popup.DesiredPlacement, FullPlacementRequested)} from PopupPlacement={popup.DesiredPlacement}, for childDesiredSize={childDesiredSize}, maxSize={maxSize}");
		}

		var halfAnchorWidth = anchorRect.Width / 2;
		var halfAnchorHeight = anchorRect.Height / 2;
		var halfChildWidth = childDesiredSize.Width / 2;
		var halfChildHeight = childDesiredSize.Height / 2;

		// Like WinUI's TestAgainstLimitsAndPlace, the flyout is kept within the available area: a target beyond it
		// (e.g. behind the input pane) gets the flyout placed against that area's edge.
		var anchorTop = Math.Min(anchorRect.Top, visibleBounds.Bottom);
		var anchorBottom = Math.Max(anchorRect.Bottom, visibleBounds.Top);
		var anchorLeft = Math.Min(anchorRect.Left, visibleBounds.Right);
		var anchorRight = Math.Max(anchorRect.Right, visibleBounds.Left);

		var finalRect = default(Rect);
		var placementFits = false;

		for (var i = 0; i < placementsToTry.Length; i++)
		{
			var placement = placementsToTry.Span[i];

			Point finalPosition;

			switch (placement)
			{
				case FlyoutBase.MajorPlacementMode.Top:
					finalPosition = new Point(
						x: anchorRect.Left + halfAnchorWidth - halfChildWidth + popup.HorizontalOffset,
						y: anchorTop - PopupPlacementTargetMargin - childDesiredSize.Height + popup.VerticalOffset);
					break;
				case FlyoutBase.MajorPlacementMode.Bottom:
					finalPosition = new Point(
						x: anchorRect.Left + halfAnchorWidth - halfChildWidth + popup.HorizontalOffset,
						y: anchorBottom + PopupPlacementTargetMargin + popup.VerticalOffset);
					break;
				case FlyoutBase.MajorPlacementMode.Left:
					finalPosition = new Point(
						x: anchorLeft - PopupPlacementTargetMargin - childDesiredSize.Width + popup.HorizontalOffset,
						y: anchorRect.Top + halfAnchorHeight - halfChildHeight + popup.VerticalOffset);
					break;
				case FlyoutBase.MajorPlacementMode.Right:
					finalPosition = new Point(
						x: anchorRight + PopupPlacementTargetMargin + popup.HorizontalOffset,
						y: anchorRect.Top + halfAnchorHeight - halfChildHeight + popup.VerticalOffset);
					break;
				case FlyoutBase.MajorPlacementMode.Full:
					if (FeatureConfiguration.Popup.ConstrainByVisibleBounds)
					{
						childDesiredSize = visibleBounds.Size;
					}
					else
					{
						// When ConstrainByVisibleBounds is false, we let the flyout to occupy the entire area available,
						// as dictated by the root skia-canvas. This extends into the status bar, and bottom bar if present.
#if __SKIA__
						childDesiredSize = m_unclippedDesiredSize;
#else
						childDesiredSize = DesiredSize;
#endif
					}

					finalPosition = new Point(
#if __SKIA__
						x: FindOptimalOffset(childDesiredSize.Width, visibleBounds.X, visibleBounds.Width, (Parent as FrameworkElement)?.ActualWidth ?? ActualWidth),
						y: FindOptimalOffset(childDesiredSize.Height, visibleBounds.Y, visibleBounds.Height, (Parent as FrameworkElement)?.ActualHeight ?? ActualHeight)
#else
						x: FindOptimalOffset(childDesiredSize.Width, visibleBounds.X, visibleBounds.Width, ActualWidth),
						y: FindOptimalOffset(childDesiredSize.Height, visibleBounds.Y, visibleBounds.Height, ActualHeight)
#endif
					);

					double FindOptimalOffset(double desired, double visibleOffset, double visibleLength, double constraint)
					{
						// Center the flyout inside the first area that fits: within visible bounds, below status bar, the screen
						if (visibleLength >= desired)
						{
							return visibleOffset + (visibleLength - desired) / 2;
						}
						if (constraint - visibleOffset >= desired)
						{
							return visibleOffset + ((constraint - visibleOffset) - desired) / 2;
						}
						else
						{
							return constraint == 0 ? 0 : (constraint - desired) / 2;
						}
					}
					break;
				default: // Other unsupported placements
					finalPosition = new Point(
						x: (visibleBounds.Width - childDesiredSize.Width) / 2.0,
						y: (visibleBounds.Height - childDesiredSize.Height) / 2.0);
					break;
			}

			var justification = FlyoutBase.GetJustificationFromPlacementMode(popup.DesiredPlacement, FullPlacementRequested);

			var fits = true;
			if (!FullPlacementRequested)
			{
				if (IsPlacementModeVertical(placement))
				{
					fits = TestAndCenterAlignWithinLimits(
						anchorRect.X,
						anchorRect.Width,
						childDesiredSize.Width,
						visibleBounds.Left,
						visibleBounds.Right,
						justification,
						out var controlXPos
					);
					finalPosition.X = controlXPos;
				}
				else
				{
					fits = TestAndCenterAlignWithinLimits(
						anchorRect.Y,
						anchorRect.Height,
						childDesiredSize.Height,
						visibleBounds.Top,
						visibleBounds.Bottom,
						justification,
						out var controlYPos
					);
					finalPosition.Y = controlYPos;
				}
			}

			finalRect = new Rect(finalPosition, childDesiredSize);

			if (fits && RectHelper.Union(visibleBounds, finalRect).Equals(visibleBounds))
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Accepted placement {placement} (choice {i}) with finalRect={finalRect} in visibleBounds={visibleBounds}");
				}
				placementFits = true;
				break; // this placement is acceptable
			}
		}

		// Like WinUI's CalculatePlacementPrivate, a flyout that fits nowhere is resized next to its target, from the preferred placement.
		if (!placementFits && Flyout is { CanResizePresenter: true })
		{
			finalRect = ResizeToFit(preferredPlacement, anchorRect, visibleBounds, minSize, finalRect);
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
		out double controlPos
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

	// MUX Reference FlyoutBase_partial.cpp (CalculateAvailableSpace), commit b9fa30e549de859f15fb84f153cc6136b981d10a
	// Calculates space between container boundary and placement target for a given placement mode.
	private static double CalculateAvailableSpace(
		FlyoutBase.MajorPlacementMode placement,
		Rect placementTargetBounds,
		Rect containerRect)
	{
		var availableSpace = placement switch
		{
			FlyoutBase.MajorPlacementMode.Top => Math.Min(containerRect.Height, placementTargetBounds.Y - containerRect.Y),
			FlyoutBase.MajorPlacementMode.Bottom => Math.Min(containerRect.Height, containerRect.Bottom - placementTargetBounds.Bottom),
			FlyoutBase.MajorPlacementMode.Left => Math.Min(containerRect.Width, placementTargetBounds.X - containerRect.X),
			FlyoutBase.MajorPlacementMode.Right => Math.Min(containerRect.Width, containerRect.Right - placementTargetBounds.Right),
			_ => 0.0,
		};

		return Math.Max(0.0, availableSpace);
	}

	// MUX Reference FlyoutBase_partial.cpp (SelectSideWithMoreSpace), commit b9fa30e549de859f15fb84f153cc6136b981d10a
	// Update placement mode and calculate available space for the best placement mode
	// along the same axis as specified by placement.
	private static FlyoutBase.MajorPlacementMode SelectSideWithMoreSpace(
		FlyoutBase.MajorPlacementMode placement,
		Rect placementTargetBounds,
		Rect containerRect,
		out double availableSpace)
	{
		var (lowSide, highSide) = IsPlacementModeVertical(placement)
			? (FlyoutBase.MajorPlacementMode.Top, FlyoutBase.MajorPlacementMode.Bottom)
			: (FlyoutBase.MajorPlacementMode.Left, FlyoutBase.MajorPlacementMode.Right);

		var availableSpaceLow = CalculateAvailableSpace(lowSide, placementTargetBounds, containerRect);
		var availableSpaceHigh = CalculateAvailableSpace(highSide, placementTargetBounds, containerRect);

		if (availableSpaceLow > availableSpaceHigh)
		{
			availableSpace = availableSpaceLow;
			return lowSide;
		}
		else if (availableSpaceLow < availableSpaceHigh)
		{
			availableSpace = availableSpaceHigh;
			return highSide;
		}

		availableSpace = availableSpaceLow;
		return placement;
	}

	// MUX Reference FlyoutBase_partial.cpp (ResizeToFit), commit b9fa30e549de859f15fb84f153cc6136b981d10a
	// Size and position control to fill available space constrained by container rectangle.
	// Uno: the margin to the target is applied here, where WinUI applies FlyoutMargin once the placement is calculated.
	private Rect ResizeToFit(
		FlyoutBase.MajorPlacementMode placement,
		Rect placementTargetBounds,
		Rect containerRect,
		Size minControlSize,
		Rect controlRect)
	{
		var controlPos = new Point(controlRect.X, controlRect.Y);
		var controlSize = new Size(controlRect.Width, controlRect.Height);
		var availableSpace = CalculateAvailableSpace(placement, placementTargetBounds, containerRect);

		// For primary axis, if exceeding available size on side specified by placement
		// find side with more space and adjust position and size to be next to target.
		// For the secondary axis, if exceeding container size, set it to fill all available space.

		if (IsPlacementModeVertical(placement))
		{
			if (controlSize.Height > availableSpace)
			{
				var moreSpacePlacement = SelectSideWithMoreSpace(placement, placementTargetBounds, containerRect, out var moreAvailableSpace);

				// Switch sides only if there's enough space to fit minimum size Flyout.
				if (moreSpacePlacement != placement && moreAvailableSpace >= minControlSize.Height)
				{
					placement = moreSpacePlacement;
					availableSpace = moreAvailableSpace;
				}

				// Adjust size only if control has enough space to fit without going under minimum size.
				if (controlSize.Height > availableSpace && availableSpace >= minControlSize.Height)
				{
					controlSize.Height = availableSpace;
				}

				// Clamp against container size.
				if (controlSize.Height > containerRect.Height)
				{
					controlSize.Height = containerRect.Height;
				}

				// Adjust position to be next to placement target.
				// Note that the position will be corrected not to exceed boundaries at the end of this function.
				controlPos.Y = placement == FlyoutBase.MajorPlacementMode.Top
					? placementTargetBounds.Y - PopupPlacementTargetMargin - controlSize.Height
					: placementTargetBounds.Bottom + PopupPlacementTargetMargin;
			}

			if (controlSize.Width > containerRect.Width)
			{
				controlSize.Width = containerRect.Width;
				controlPos.X = containerRect.X;
			}
		}
		else
		{
			if (controlSize.Width > availableSpace)
			{
				var moreSpacePlacement = SelectSideWithMoreSpace(placement, placementTargetBounds, containerRect, out var moreAvailableSpace);

				// Switch sides only if there's enough space to fit minimum size Flyout.
				if (moreSpacePlacement != placement && moreAvailableSpace >= minControlSize.Width)
				{
					placement = moreSpacePlacement;
					availableSpace = moreAvailableSpace;
				}

				// Adjust size only if control has enough space to fit without going under minimum size.
				if (controlSize.Width > availableSpace && availableSpace >= minControlSize.Width)
				{
					controlSize.Width = availableSpace;
				}

				// Clamp against container size.
				if (controlSize.Width > containerRect.Width)
				{
					controlSize.Width = containerRect.Width;
				}

				// Adjust position to be next to placement target.
				// Note that the position will be corrected not to exceed boundaries at the end of this function.
				controlPos.X = placement == FlyoutBase.MajorPlacementMode.Left
					? placementTargetBounds.X - PopupPlacementTargetMargin - controlSize.Width
					: placementTargetBounds.Right + PopupPlacementTargetMargin;
			}

			if (controlSize.Height > containerRect.Height)
			{
				controlSize.Height = containerRect.Height;
				controlPos.Y = containerRect.Y;
			}
		}

		// Finally, make sure we do not exceed container boundaries.

		if (controlPos.X < containerRect.X)
		{
			controlPos.X = containerRect.X;
		}
		else if (controlPos.X + controlSize.Width > containerRect.Right)
		{
			controlPos.X = containerRect.Right - controlSize.Width;
		}

		if (controlPos.Y < containerRect.Y)
		{
			controlPos.Y = containerRect.Y;
		}
		else if (controlPos.Y + controlSize.Height > containerRect.Bottom)
		{
			controlPos.Y = containerRect.Bottom - controlSize.Height;
		}

		return new Rect(controlPos, controlSize);
	}

	private Rect GetVisibleBounds() => XamlRoot?.VisualTree.VisibleBounds ?? default;

	// Like WinUI's FlyoutBase::PerformPlacement, a flyout is placed within the area left visible by the input pane.
	private Rect GetAvailableWindowRect(Popup popup) => Flyout is { } flyout
		? FlyoutBase.CalculateAvailableWindowRect(flyout is MenuFlyout, popup, popup.PlacementTarget, flyout.IsTargetPositionSet, default, FullPlacementRequested)
		: GetVisibleBounds();
}
