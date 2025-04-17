#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Uno.UI;
using Uno.Foundation.Logging;
using System.Linq;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

#if __APPLE_UIKIT__
using View = UIKit.UIView;
#elif __ANDROID__
using Android.Views;
#endif

#if HAS_UNO_WINUI
using WindowSizeChangedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.WindowSizeChangedEventArgs;
using XamlWindow = Microsoft/* UWP don't rename */.UI.Xaml.Window;
#else
using WindowSizeChangedEventArgs = Windows.UI.Core.WindowSizeChangedEventArgs;
#endif

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
			var desiredSize = child.DesiredSize;
			Size maxSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
			if (child is FrameworkElement fe)
			{
				maxSize = fe.GetMaxSize();// UWP takes FlyoutPresenter's MaxHeight and MaxWidth into consideration, but ignores Height and Width
			}
			var rect = CalculatePopupPlacement(popup, desiredSize, maxSize);

			if (Flyout?.IsTargetPositionSet ?? false)
			{
				rect = Flyout.UpdateTargetPosition(GetVisibleBounds(), desiredSize, rect);
			}

			child.Arrange(rect);
		}

		return finalSize;
	}

#if __ANDROID__ || __APPLE_UIKIT__
	/// <summary>
	/// A native view to use as the anchor, in the case that the managed <see cref="AnchorControl"/> is a proxy that's not actually
	/// included in the visual tree.
	/// </summary>
	protected virtual View? NativeAnchor => null;
#endif

	private Rect GetAnchorRect(Popup popup)
	{
#if __ANDROID__ || __APPLE_UIKIT__
		if (NativeAnchor is not null)
		{
			return NativeAnchor.GetBoundsRectRelativeTo(this);
		}
#endif

		return popup.PlacementTarget.GetBoundsRectRelativeTo(this);
	}

	protected virtual Rect CalculatePopupPlacement(Popup popup, Size childDesiredSize, Size maxSize)
	{
		var anchorRect = GetAnchorRect(popup);

		var visibleBounds = GetVisibleBounds();

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

		var finalRect = default(Rect);

		for (var i = 0; i < placementsToTry.Length; i++)
		{
			var placement = placementsToTry.Span[i];

			Point finalPosition;

			switch (placement)
			{
				case FlyoutBase.MajorPlacementMode.Top:
					finalPosition = new Point(
						x: anchorRect.Left + halfAnchorWidth - halfChildWidth + popup.HorizontalOffset,
						y: anchorRect.Top - PopupPlacementTargetMargin - childDesiredSize.Height + popup.VerticalOffset);
					break;
				case FlyoutBase.MajorPlacementMode.Bottom:
					finalPosition = new Point(
						x: anchorRect.Left + halfAnchorWidth - halfChildWidth + popup.HorizontalOffset,
						y: anchorRect.Bottom + PopupPlacementTargetMargin + popup.VerticalOffset);
					break;
				case FlyoutBase.MajorPlacementMode.Left:
					finalPosition = new Point(
						x: anchorRect.Left - PopupPlacementTargetMargin - childDesiredSize.Width + popup.HorizontalOffset,
						y: anchorRect.Top + halfAnchorHeight - halfChildHeight + popup.VerticalOffset);
					break;
				case FlyoutBase.MajorPlacementMode.Right:
					finalPosition = new Point(
						x: anchorRect.Right + PopupPlacementTargetMargin + popup.HorizontalOffset,
						y: anchorRect.Top + halfAnchorHeight - halfChildHeight + popup.VerticalOffset);
					break;
				case FlyoutBase.MajorPlacementMode.Full:
#if __SKIA__
					// For skia-ios/droid, we let the flyout to occupy the entire area available,
					// as dictated by the root skia-canvas. This extends into status bar, and bottom bar if present.
					childDesiredSize = m_unclippedDesiredSize
#elif !__APPLE_UIKIT__
					childDesiredSize = visibleBounds.Size
#else
					// The mobile status bar should always remain visible.
					// On droid, this panel is placed beneath the status bar.
					// On iOS, this panel will cover the status bar, so we have to substract it out.
					childDesiredSize = new Size(ActualWidth, ActualHeight)
						.Subtract(0, visibleBounds.Y)
#endif
						.AtMost(maxSize);
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

	private Rect GetVisibleBounds() => XamlRoot?.VisualTree.VisibleBounds ?? default;
}
