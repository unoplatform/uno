// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference FlyoutBase_partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Input;
using Uno.Disposables;
using Uno.UI.Dispatching;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Extensions;
using Uno.UI.Helpers.Boxes;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.UI.Core;
using static DirectUI.RectUtil;
using DoubleUtil = DirectUI.DoubleUtil;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class FlyoutBase
{
	//------------------------------------------------------------------------
	// Helpers
	//------------------------------------------------------------------------

	private static PreferredJustification GetJustificationFromPlacementMode(FlyoutPlacementMode placement)
	{
		var result = PreferredJustification.Center;

		switch (placement)
		{
			case FlyoutPlacementMode.Full:
			case FlyoutPlacementMode.Top:
			case FlyoutPlacementMode.Bottom:
			case FlyoutPlacementMode.Left:
			case FlyoutPlacementMode.Right:
				result = PreferredJustification.Center;
				break;
			case FlyoutPlacementMode.TopEdgeAlignedLeft:
			case FlyoutPlacementMode.BottomEdgeAlignedLeft:
				result = PreferredJustification.Left;
				break;
			case FlyoutPlacementMode.TopEdgeAlignedRight:
			case FlyoutPlacementMode.BottomEdgeAlignedRight:
				result = PreferredJustification.Right;
				break;
			case FlyoutPlacementMode.LeftEdgeAlignedTop:
			case FlyoutPlacementMode.RightEdgeAlignedTop:
				result = PreferredJustification.Top;
				break;
			case FlyoutPlacementMode.LeftEdgeAlignedBottom:
			case FlyoutPlacementMode.RightEdgeAlignedBottom:
				result = PreferredJustification.Bottom;
				break;
			default:
				Debug.Fail("Unsupported FlyoutPlacementMode");
				break;
		}

		return result;
	}

	internal static MajorPlacementMode GetMajorPlacementFromPlacement(FlyoutPlacementMode placement)
	{
		var result = MajorPlacementMode.Full;

		switch (placement)
		{
			case FlyoutPlacementMode.Full:
				result = MajorPlacementMode.Full;
				break;
			case FlyoutPlacementMode.Top:
			case FlyoutPlacementMode.TopEdgeAlignedLeft:
			case FlyoutPlacementMode.TopEdgeAlignedRight:
				result = MajorPlacementMode.Top;
				break;
			case FlyoutPlacementMode.Bottom:
			case FlyoutPlacementMode.BottomEdgeAlignedLeft:
			case FlyoutPlacementMode.BottomEdgeAlignedRight:
				result = MajorPlacementMode.Bottom;
				break;
			case FlyoutPlacementMode.Left:
			case FlyoutPlacementMode.LeftEdgeAlignedTop:
			case FlyoutPlacementMode.LeftEdgeAlignedBottom:
				result = MajorPlacementMode.Left;
				break;
			case FlyoutPlacementMode.Right:
			case FlyoutPlacementMode.RightEdgeAlignedTop:
			case FlyoutPlacementMode.RightEdgeAlignedBottom:
				result = MajorPlacementMode.Right;
				break;
			default:
				Debug.Fail("Unsupported FlyoutPlacementMode");
				break;
		}

		return result;
	}

	private static FlyoutPlacementMode GetPlacementFromMajorPlacement(MajorPlacementMode majorPlacement)
	{
		var result = FlyoutPlacementMode.Full;

		switch (majorPlacement)
		{
			case MajorPlacementMode.Full:
				result = FlyoutPlacementMode.Full;
				break;
			case MajorPlacementMode.Top:
				result = FlyoutPlacementMode.Top;
				break;
			case MajorPlacementMode.Bottom:
				result = FlyoutPlacementMode.Bottom;
				break;
			case MajorPlacementMode.Left:
				result = FlyoutPlacementMode.Left;
				break;
			case MajorPlacementMode.Right:
				result = FlyoutPlacementMode.Right;
				break;
			default:
				Debug.Fail("Unsupported FlyoutPlacementMode");
				break;
		}

		return result;
	}

	// Return true if placement is along vertical axis, false otherwise.
	private static bool IsPlacementModeVertical(MajorPlacementMode placementMode)
	{
		// We are safe even if placementMode is Full. because the case for placementMode is Full has already been put in another if branch in function PerformPlacement.
		// if necessary, we can add another function : IsPlacementModeHorizontal
		return placementMode == MajorPlacementMode.Top ||
			placementMode == MajorPlacementMode.Bottom;
	}

	// Helper for finding parent of element whose type is IAppBar.
	private static AppBar FindParentAppBar(FrameworkElement pElement)
	{
		var spCurrent = pElement;

		while (spCurrent is not null)
		{
			if (spCurrent is AppBar spAppBar)
			{
				return spAppBar;
			}

			spCurrent = spCurrent.Parent as FrameworkElement;
		}

		return null;
	}

	// Helper which depending on FlowDirection will either directly copy placementMode to adjustedPlacementMode (LTR)
	// or will reverse Left/Right if order is RTL.
	private static void GetEffectivePlacementMode(
		MajorPlacementMode placementMode,
		FlowDirection flowDirection,
		out MajorPlacementMode pAdjustedPlacementMode)
	{
		pAdjustedPlacementMode = placementMode;

		if (flowDirection == FlowDirection.RightToLeft)
		{
			if (placementMode == MajorPlacementMode.Left)
			{
				pAdjustedPlacementMode = MajorPlacementMode.Right;
			}
			else if (placementMode == MajorPlacementMode.Right)
			{
				pAdjustedPlacementMode = MajorPlacementMode.Left;
			}
		}
	}

	// Fails if placementMode is not a valid value.
	private static void ValidateFlyoutPlacementMode(FlyoutPlacementMode placementMode)
	{
		switch (placementMode)
		{
			case FlyoutPlacementMode.Top:
			case FlyoutPlacementMode.Bottom:
			case FlyoutPlacementMode.Left:
			case FlyoutPlacementMode.Right:
			case FlyoutPlacementMode.Full:
			case FlyoutPlacementMode.TopEdgeAlignedLeft:
			case FlyoutPlacementMode.TopEdgeAlignedRight:
			case FlyoutPlacementMode.BottomEdgeAlignedLeft:
			case FlyoutPlacementMode.BottomEdgeAlignedRight:
			case FlyoutPlacementMode.LeftEdgeAlignedTop:
			case FlyoutPlacementMode.LeftEdgeAlignedBottom:
			case FlyoutPlacementMode.RightEdgeAlignedTop:
			case FlyoutPlacementMode.RightEdgeAlignedBottom:
				return;
			default:
				throw new ArgumentException("Invalid FlyoutPlacementMode", nameof(placementMode));
		}
	}

	// Helper for getting minimum size of FrameworkElement.
	private static Size GetMinimumSize(FrameworkElement pFrameworkElement) =>
		new Size(pFrameworkElement.MinWidth, pFrameworkElement.MinHeight);

	// Helper for getting maximum size of FrameworkElement.
	private static Size GetMaximumSize(FrameworkElement pFrameworkElement) =>
		new Size(pFrameworkElement.MaxWidth, pFrameworkElement.MaxHeight);

	// Placement helpers

	// Align centers of anchor and control while keeping control coordinates within limits.
	private static bool TestAndCenterAlignWithinLimits(
		double anchorPos,
		double anchorSize,
		double controlSize,
		double lowLimit,
		double highLimit,
		PreferredJustification justification,
		out double pControlPos)
	{
		bool fits = true;

		Debug.Assert(anchorSize >= 0.0);
		Debug.Assert(controlSize >= 0.0);

		pControlPos = 0.0;

		if ((highLimit - lowLimit) > controlSize &&
			anchorSize >= 0.0 &&
			controlSize >= 0.0)
		{
			if (justification == PreferredJustification.Center)
			{
				pControlPos = anchorPos + 0.5 * (anchorSize - controlSize);
			}
			else if (justification == PreferredJustification.Top || justification == PreferredJustification.Left)
			{
				pControlPos = anchorPos;
			}
			else if (justification == PreferredJustification.Bottom || justification == PreferredJustification.Right)
			{
				pControlPos = anchorPos + (anchorSize - controlSize);
			}
			else
			{
				Debug.Fail("Unsupported FlyoutBase::PreferredJustification");
			}

			if (pControlPos < lowLimit)
			{
				pControlPos = lowLimit;
			}
			else if (pControlPos + controlSize > highLimit)
			{
				pControlPos = highLimit - controlSize;
			}
		}
		else
		{
			pControlPos = lowLimit;
			fits = false;
		}

		return fits;
	}

	// Test if control can fit in space between anchor and limit.  Regardless of outcome, calculate
	// position and clamp it against limits and anchor.
	private static bool TestAgainstLimitsAndPlace(
		double anchorPos,
		double anchorSize,
		bool isIncreasing,
		double controlSize,
		double lowLimit,
		double highLimit,
		out double pControlPos)
	{
		bool fits = true;
		double extraSpace = 0.0;
		double anchorEnd = anchorPos + anchorSize;

		Debug.Assert(anchorSize >= 0.0);
		Debug.Assert(controlSize >= 0.0);

		pControlPos = lowLimit;

		// Pick the correct limit for comparison.
		if (isIncreasing)
		{
			extraSpace = highLimit - anchorEnd - controlSize;
			pControlPos = anchorPos + anchorSize;
		}
		else
		{
			extraSpace = anchorPos - lowLimit - controlSize;
			pControlPos = anchorPos - controlSize;
		}

		if (extraSpace < 0.0)
		{
			fits = false;
		}

		// Clamp against limits...

		if (pControlPos < lowLimit)
		{
			pControlPos = lowLimit;
		}
		else if (pControlPos + controlSize > highLimit)
		{
			pControlPos = highLimit - controlSize;
		}

		return fits;
	}

	// Test if control has enough space to fit on the primary axis and center on the secondary axis.
	// Even if it does not have enough room calculate the position.
	private static bool TryPlacement(
		Rect placementTarget,
		Size controlSize,
		Rect containerRect,
		MajorPlacementMode placementMode,
		PreferredJustification justification,
		out Point pControlPos)
	{
		double controlXPos = 0.0;
		double controlYPos = 0.0;
		bool placementFits = false;

		Debug.Assert(placementMode != MajorPlacementMode.Full, "PlacementMode should not be Full when we come here.");

		if (IsPlacementModeVertical(placementMode))
		{
			if (TestAgainstLimitsAndPlace(
				placementTarget.Y,
				placementTarget.Height,
				placementMode == MajorPlacementMode.Bottom,
				controlSize.Height,
				containerRect.Y,
				containerRect.Y + containerRect.Height,
				out controlYPos))
			{
				placementFits = true;
			}

			placementFits &= TestAndCenterAlignWithinLimits(
				placementTarget.X,
				placementTarget.Width,
				controlSize.Width,
				containerRect.X,
				containerRect.X + containerRect.Width,
				justification,
				out controlXPos);
		}
		else
		{
			if (TestAgainstLimitsAndPlace(
				placementTarget.X,
				placementTarget.Width,
				placementMode == MajorPlacementMode.Right,
				controlSize.Width,
				containerRect.X,
				containerRect.X + containerRect.Width,
				out controlXPos))
			{
				placementFits = true;
			}

			placementFits &= TestAndCenterAlignWithinLimits(
				placementTarget.Y,
				placementTarget.Height,
				controlSize.Height,
				containerRect.Y,
				containerRect.Y + containerRect.Height,
				justification,
				out controlYPos);
		}

		pControlPos = new Point(controlXPos, controlYPos);

		return placementFits;
	}

	// Try to place a control according to fallback order.  If none of the fallbacks produces result which does not
	// violate placement rules, use the first one specified.
	// Return true if control can fit entirely without adjusting its size.
	private static bool PerformPlacementWithFallback(
		Rect placementTarget,
		Size controlSize,
		Rect containerRect,
		MajorPlacementMode[] orderedPlacements,
		int placementCount,
		PreferredJustification justification,
		out Point pControlPos,
		out MajorPlacementMode pEffectiveMode)
	{
		Debug.Assert(placementCount > 0);

		pControlPos = default;
		pEffectiveMode = MajorPlacementMode.Top;

		// Try the initial placement and retain the coordinate regardless of the outcome.
		if (TryPlacement(placementTarget, controlSize, containerRect, orderedPlacements[0], justification, out var firstChoicePos))
		{
			pEffectiveMode = orderedPlacements[0];
			pControlPos = firstChoicePos;
			return true;
		}
		else
		{
			// If failed, work through the fallback list.
			bool fallbackSuccessful = false;

			for (int placementIndex = 1; placementIndex < placementCount; ++placementIndex)
			{
				if (TryPlacement(placementTarget, controlSize, containerRect, orderedPlacements[placementIndex], justification, out pControlPos))
				{
					pEffectiveMode = orderedPlacements[placementIndex];
					fallbackSuccessful = true;
					break;
				}
			}

			// If none of the fallbacks was successful, go back to the first one.
			if (!fallbackSuccessful)
			{
				pEffectiveMode = orderedPlacements[0];
				pControlPos = firstChoicePos;
			}

			return fallbackSuccessful;
		}
	}

	// Calculates space between container boundary and placement target for a given placement mode.
	private static double CalculateAvailableSpace(
		MajorPlacementMode placement,
		Rect placementTargetBounds,
		Rect containerRect)
	{
		double availableSpace = 0.0;

		switch (placement)
		{
			case MajorPlacementMode.Top:
				availableSpace = placementTargetBounds.Y - containerRect.Y;
				availableSpace = Math.Min(containerRect.Height, availableSpace);
				break;

			case MajorPlacementMode.Bottom:
				availableSpace = (containerRect.Y + containerRect.Height) - (placementTargetBounds.Y + placementTargetBounds.Height);
				availableSpace = Math.Min(containerRect.Height, availableSpace);
				break;

			case MajorPlacementMode.Left:
				availableSpace = placementTargetBounds.X - containerRect.X;
				availableSpace = Math.Min(containerRect.Width, availableSpace);
				break;

			case MajorPlacementMode.Right:
				availableSpace = (containerRect.X + containerRect.Width) - (placementTargetBounds.X + placementTargetBounds.Width);
				availableSpace = Math.Min(containerRect.Width, availableSpace);
				break;

			default:
				Debug.Fail("Unsupported FlyoutPlacementMode");
				break;
		}

		availableSpace = Math.Max(0.0, availableSpace);

		return availableSpace;
	}

	// Update placement mode and calculate available space for the best placement mode
	// along the same axis as specified by pPlacement.
	private static void SelectSideWithMoreSpace(
		ref MajorPlacementMode pPlacement,
		Rect placementTargetBounds,
		Rect containerRect,
		out double pAvailableSpace)
	{
		pAvailableSpace = 0.0;

		Debug.Assert(pPlacement != MajorPlacementMode.Full, "PlacementMode should not be Full when we come here.");

		if (IsPlacementModeVertical(pPlacement))
		{
			double availableSpaceTop = CalculateAvailableSpace(
				MajorPlacementMode.Top,
				placementTargetBounds,
				containerRect);

			double availableSpaceBottom = CalculateAvailableSpace(
				MajorPlacementMode.Bottom,
				placementTargetBounds,
				containerRect);

			if (availableSpaceTop > availableSpaceBottom)
			{
				pPlacement = MajorPlacementMode.Top;
				pAvailableSpace = availableSpaceTop;
			}
			else if (availableSpaceTop < availableSpaceBottom)
			{
				pPlacement = MajorPlacementMode.Bottom;
				pAvailableSpace = availableSpaceBottom;
			}
			else
			{
				pAvailableSpace = availableSpaceTop;
			}
		}
		else
		{
			double availableSpaceLeft = CalculateAvailableSpace(
				MajorPlacementMode.Left,
				placementTargetBounds,
				containerRect);

			double availableSpaceRight = CalculateAvailableSpace(
				MajorPlacementMode.Right,
				placementTargetBounds,
				containerRect);

			if (availableSpaceLeft > availableSpaceRight)
			{
				pPlacement = MajorPlacementMode.Left;
				pAvailableSpace = availableSpaceLeft;
			}
			else if (availableSpaceLeft < availableSpaceRight)
			{
				pPlacement = MajorPlacementMode.Right;
				pAvailableSpace = availableSpaceRight;
			}
			else
			{
				pAvailableSpace = availableSpaceLeft;
			}
		}
	}

	// Size and position control to fill available space constrained by container rectangle.
	private static void ResizeToFit(
		ref MajorPlacementMode pPlacement,
		Rect placementTargetBounds,
		Rect containerRect,
		Size minControlSize,
		ref Point pControlPos,
		ref Size pControlSize)
	{
		double availableSpace = CalculateAvailableSpace(
			pPlacement,
			placementTargetBounds,
			containerRect);

		Debug.Assert(pPlacement != MajorPlacementMode.Full, "PlacementMode should not be Full when we come here.");

		// For primary axis, if exceeding available size on side specified by pPlacement
		// find side with more space and adjust position and size to be next to target.
		// For the secondary axis, if exceeding container size, set it to fill all available space.

		if (IsPlacementModeVertical(pPlacement))
		{
			if (pControlSize.Height > availableSpace)
			{
				var moreSpacePlacement = pPlacement;

				SelectSideWithMoreSpace(
					ref moreSpacePlacement,
					placementTargetBounds,
					containerRect,
					out double moreAvailableSpace);

				// Switch sides only if there's enough space to fit minimum size Flyout.

				if (moreSpacePlacement != pPlacement && moreAvailableSpace >= minControlSize.Height)
				{
					pPlacement = moreSpacePlacement;
					availableSpace = moreAvailableSpace;
				}

				// Adjust size only if control has enough space to fit without going under minimum size.

				if (pControlSize.Height > availableSpace && availableSpace >= minControlSize.Height)
				{
					pControlSize.Height = availableSpace;
				}

				// Clamp against container size.

				if (pControlSize.Height > containerRect.Height)
				{
					pControlSize.Height = containerRect.Height;
				}

				// Adjust position to be next to placement target.
				// Note that the position will be corrected not to exceed boundaries at the end of this function.

				if (pPlacement == MajorPlacementMode.Top)
				{
					pControlPos.Y = placementTargetBounds.Y - pControlSize.Height;
				}
				else
				{
					pControlPos.Y = placementTargetBounds.Y + placementTargetBounds.Height;
				}
			}

			if (pControlSize.Width > containerRect.Width)
			{
				pControlSize.Width = containerRect.Width;
				pControlPos.X = containerRect.X;
			}
		}
		else
		{
			if (pControlSize.Width > availableSpace)
			{
				var moreSpacePlacement = pPlacement;

				SelectSideWithMoreSpace(
					ref moreSpacePlacement,
					placementTargetBounds,
					containerRect,
					out double moreAvailableSpace);

				// Switch sides only if there's enough space to fit minimum size Flyout.

				if (moreSpacePlacement != pPlacement && moreAvailableSpace >= minControlSize.Width)
				{
					pPlacement = moreSpacePlacement;
					availableSpace = moreAvailableSpace;
				}

				// Adjust size only if control has enough space to fit without going under minimum size.

				if (pControlSize.Width > availableSpace && availableSpace >= minControlSize.Width)
				{
					pControlSize.Width = availableSpace;
				}

				// Clamp against container size.

				if (pControlSize.Width > containerRect.Width)
				{
					pControlSize.Width = containerRect.Width;
				}

				// Adjust position to be next to placement target.
				// Note that the position will be corrected not to exceed boundaries at the end of this function.

				if (pPlacement == MajorPlacementMode.Left)
				{
					pControlPos.X = placementTargetBounds.X - pControlSize.Width;
				}
				else
				{
					pControlPos.X = placementTargetBounds.X + placementTargetBounds.Width;
				}
			}

			if (pControlSize.Height > containerRect.Height)
			{
				pControlSize.Height = containerRect.Height;
				pControlPos.Y = containerRect.Y;
			}
		}

		// Finally, make sure we do not exceed container boundaries.

		if (pControlPos.X < containerRect.X)
		{
			pControlPos.X = containerRect.X;
		}
		else if ((pControlPos.X + pControlSize.Width) > (containerRect.X + containerRect.Width))
		{
			pControlPos.X = (containerRect.X + containerRect.Width) - pControlSize.Width;
		}

		if (pControlPos.Y < containerRect.Y)
		{
			pControlPos.Y = containerRect.Y;
		}
		else if ((pControlPos.Y + pControlSize.Height) > (containerRect.Y + containerRect.Height))
		{
			pControlPos.Y = (containerRect.Y + containerRect.Height) - pControlSize.Height;
		}
	}

	//------------------------------------------------------------------------
	// FlyoutBase
	//------------------------------------------------------------------------

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == LightDismissOverlayModeProperty)
		{
			// The overlay is configured whenever we open the popup,
			// so if this property changes while it's open, then we
			// need to re-configure it.
			if (_popup is not null && _popup.IsOpen)
			{
				ConfigurePopupOverlay();
			}
		}
		else if (args.Property == OverlayInputPassThroughElementProperty)
		{
			m_ownsOverlayInputPassThroughElement = false;
		}
		else if (args.Property == ShowModeProperty)
		{
			UpdateStateToShowMode(ShowMode);
		}
		else if (args.Property == ShouldConstrainToRootBoundsProperty)
		{
			if (_popup is not null)
			{
				_popup.ShouldConstrainToRootBounds = ShouldConstrainToRootBounds;
			}
		}
#if HAS_UNO
		else if (args.Property == AllowFocusOnInteractionProperty || args.Property == AllowFocusWhenDisabledProperty)
		{
			// Forces the values to the popup, as it is not a child of the flyout in the visual tree.
			_popup?.SetValue(args.Property == AllowFocusOnInteractionProperty ? Popup.AllowFocusOnInteractionProperty : Popup.AllowFocusWhenDisabledProperty, args.NewValue, precedence: DependencyPropertyValuePrecedences.Local);
		}
#endif
	}

	protected virtual Control CreatePresenter() => null;

	// Associates this flyout with the correct XamlRoot for its context, or returns an error
	// if the context is ambiguous.
	private void EnsureAssociatedXamlRoot(DependencyObject placementTarget)
	{
		VisualTree visualTree = VisualTree.GetUniqueVisualTreeNoRef(
			this,
			placementTarget,
			null);

		if (visualTree is not null)
		{
			visualTree.AttachElement(this);
		}
	}

	/// <summary>
	/// Shows the flyout placed in relation to the specified element.
	/// </summary>
	/// <param name="placementTarget">The element to use as the flyout's placement target.</param>
	public void ShowAt(FrameworkElement placementTarget)
	{
		EnsureAssociatedXamlRoot(placementTarget);

		FlyoutShowOptions showOptions = new();

		showOptions.ShowMode = ShowMode;

		ShowAt(placementTarget, showOptions);
	}

	/// <summary>
	/// Shows the flyout placed in relation to the specified element using the specified options.
	/// </summary>
	/// <param name="placementTarget">The element to use as the flyout's placement target.</param>
	/// <param name="showOptions">The options to use when showing the flyout.</param>
	public void ShowAt(DependencyObject placementTarget, FlyoutShowOptions showOptions)
	{
		bool shouldConstrainToRootBounds = ShouldConstrainToRootBounds;

		try
		{
			// MenuFlyouts are always windowed, whereas other flyouts are windowed if ShouldConstrainToRootBounds is false.
			m_openingWindowedInProgress = !shouldConstrainToRootBounds || this is MenuFlyout;

			Point? position = null;
			Rect? exclusionRect = null;
			var showMode = FlyoutShowMode.Auto;
			var placement = FlyoutPlacementMode.Auto;
			Media.GeneralTransform transformToRoot = null;
			FrameworkElement visualRootAsFE;

			m_hasPlacementOverride = false;

			if (showOptions is not null)
			{
				position = showOptions.Position;
				exclusionRect = showOptions.ExclusionRect;
				showMode = showOptions.ShowMode;
				placement = showOptions.Placement;
			}

			var placementTargetAsUIE = placementTarget as UIElement;
			if (placementTargetAsUIE is null && position is null)
			{
				throw new ArgumentException("The placement target and the position of the flyout show options cannot both be null.");
			}

			EnsureAssociatedXamlRoot(placementTarget);
			var xamlRoot = XamlRoot;

			// The call to EnsureAssociatedXamlRoot guarantees that if it succeeds we'll have a valid xamlRoot pointer.
			if (xamlRoot is null)
			{
				throw new InvalidOperationException("The flyout has no XamlRoot.");
			}

			// If we're going to need to transform to root, let's create the transform now.
			if (placementTargetAsUIE is not null && (position is not null || exclusionRect is not null))
			{
				transformToRoot = placementTargetAsUIE.TransformToVisual(null);
			}

			if (position is { } positionNotNull)
			{
				m_isPositionedAtPoint = true;

				var positionValue = positionNotNull;

				if (transformToRoot is not null)
				{
					positionValue = transformToRoot.TransformPoint(positionValue);
				}

				EnsurePopupAndPresenter();

				Rect contentRect = default;

				if (IsWindowedPopup())
				{
					contentRect = Popup.CalculateAvailableMonitorRect(xamlRoot, positionValue);
				}
				else
				{
					var contentSize = xamlRoot.Size;

					contentRect.X = 0;
					contentRect.Y = 0;
					contentRect.Width = contentSize.Width;
					contentRect.Height = contentSize.Height;
				}

				if (double.IsNaN(positionValue.X) || double.IsNaN(positionValue.Y))
				{
					throw new ArgumentException("Invalid flyout position");
				}
				else
				{
					// Adjust the target position such that it is within the contentRect boundaries.
					positionValue.X = Math.Clamp(positionValue.X, contentRect.X, contentRect.X + contentRect.Width);
					positionValue.Y = Math.Clamp(positionValue.Y, contentRect.Y, contentRect.Y + contentRect.Height);

					// Set the target position to show the flyout
					SetTargetPosition(positionValue);
				}
			}
			else
			{
				m_isPositionedAtPoint = false;
			}

			if (exclusionRect is { } exclusionRectNotNull)
			{
				var exclusionRectValue = exclusionRectNotNull;

				if (transformToRoot is not null)
				{
					exclusionRectValue = transformToRoot.TransformBounds(exclusionRectValue);
				}

				m_exclusionRect = exclusionRectValue;
			}
			else
			{
				m_exclusionRect = CreateEmptyRect();
			}

			UpdateStateToShowMode(showMode);

			if (placement != FlyoutPlacementMode.Auto)
			{
				m_hasPlacementOverride = true;
				m_placementOverride = placement;
			}

			// Show at the target element, if target is null use the frame as target
			if (placementTargetAsUIE is not null)
			{
				visualRootAsFE = placementTargetAsUIE as FrameworkElement;
			}
			else
			{
				visualRootAsFE = xamlRoot.Content as FrameworkElement;
			}

			bool openDelayed = false;
			bool succeeded = false;
			bool isOpen = false;

			try
			{
				ShowAtCore(visualRootAsFE, out openDelayed);

				// Set popup flow direction must be called after ShowAt
				// to ensure the placement target in the flyout is initialized.
				ForwardPopupFlowDirection();

				// Check if the popup successfully opened unless the opening was intentionally
				// delayed for instance when a submenu closes and another one opens.
				if (!openDelayed)
				{
					isOpen = _popup?.IsOpen == true;
				}

				succeeded = true;
			}
			finally
			{
				if (!succeeded || (!openDelayed && !isOpen))
				{
					// Since the popup actually did not open, reset the m_hasPlacementOverride flag so that
					// it has the correct value at the next opening attempt.
					m_hasPlacementOverride = false;
				}
			}
		}
		finally
		{
			m_openingWindowedInProgress = false;
		}
	}

	private protected virtual void ShowAtCore(FrameworkElement pPlacementTarget, out bool openDelayed)
	{
		openDelayed = false;

		var oldPlacementTarget = Target;

		ValidateAndSetParameters(pPlacementTarget, out bool shouldOpen);

		// If we shouldn't open, we'll early out here.
		if (!shouldOpen)
		{
			return;
		}

		EnsureAssociatedXamlRoot(pPlacementTarget);

		// Determine whether this flyout is opening as a child flyout.
		var parentFlyout = FindParentFlyoutFromElement(Target);
		if (parentFlyout is not null)
		{
			// Allocate the metadata object for the parent's instance to track
			// the child flyouts.
			parentFlyout.m_childFlyoutMetadata ??= new();

			// By setting the parent reference, we're marking ourselves as being a child flyout.
			m_tpParentFlyout = parentFlyout;
		}

		// Determine whether there is already an open flyout tracked by the appropriate
		// flyout metadata, and stage this instance until it closes rather than opening
		// immediately.
		CheckAndHandleOpenFlyout(out shouldOpen);

		if (shouldOpen)
		{
			Open();

			// If our placement target has changed, then we'll be reopening the flyout and placement will happen on presenter load.
			// Otherwise, we should perform placement now, as that step may not happen.
			if (oldPlacementTarget == Target)
			{
				var presenterSize = GetPresenterSize();

				// RS3: Adjust the placement based on the screen size. Skip the PreparePopupTheme call
				// to avoid setting m_tpThemeTransition for compatibility with RS2.
				PerformPlacement(presenterSize, 0.0, false /*preparePopupTheme*/);
			}
		}
		else
		{
			openDelayed = true;
		}
	}

	private void HideImpl()
	{
		// Provide an opportunity for the app to cancel hiding the flyout.
		bool cancel = false;
		OnClosing(ref cancel);
		if (cancel)
		{
			return;
		}

		// If we're in the process of opening a flyout, a call to Hide() should cancel that.
		m_openingCanceled = true;

		if (m_childFlyoutMetadata is not null)
		{
			// Hide any child flyouts above this instance.  We do this
			// here rather than letting the presenter unloaded handler
			// handle this to produce the effect of the entire chain of
			// child flyouts closing at the same time.  If we waited for
			// the presenter unloaded handler, you would see the chain
			// close one at a time from the bottom up, which would produce
			// an obvious delay.
			m_childFlyoutMetadata.GetOpenFlyout(out var childFlyout, out _);
			childFlyout?.Hide();
		}

		RemoveRootVisualPointerMovedHandler();

		m_wrRootVisual = null;

		// Since popup is light-dismiss all closing logic is in unloaded event handler.  Here, we only hide the popup.
		ClosePopup();
	}

	// TODO Uno: Native pickers (Android) have no popup to hide, they override this to dismiss their dialog.
	private protected virtual void ClosePopup()
	{
		if (_popup is not null)
		{
			_popup.IsOpen = false;
		}
	}

	// This will be called from either ShowAtImpl if there were no staged Flyouts or from OnPresenterUnloaded when
	// previous FlyoutBase was closed.  Parameters should be validated and cached prior to calling this method.
	internal virtual void Open()
	{
		// Placement target should be validated and set.
		Debug.Assert(Target is not null);

		if (Target?.XamlRoot?.VisualTree.PublicRootVisual is { } publicRootVisual)
		{
			m_wrRootVisual = WeakReferencePool.RentWeakReference(this, publicRootVisual);
		}

		var flyoutMetadata = GetFlyoutMetadata();

		m_isPositionedForDateTimePicker = false;

#if DEBUG
		{
			flyoutMetadata.GetOpenFlyout(out var openFlyout, out var openFlyoutPlacementTarget);

			Debug.Assert(openFlyout is null);
			Debug.Assert(openFlyoutPlacementTarget is null);
		}
#endif

		EnsurePopupAndPresenter();

		// Set how the popup will be dismissed. This needs to be done each time the flyout is
		// opened because the placement mode may change. In theory we should wait until the
		// placement logic has been executed to get the final placement, but the light dismiss
		// value must be set before the popup is opened to have an effect.
		SetPopupLightDismissBehavior();

		// Configure the popup to have an overlay (depends on the value of LightDismissOverlayMode),
		// as well as set the correct brush to use.
		ConfigurePopupOverlay();

		ForwardTargetPropertiesToPresenter();

		var presenterAsFE = m_tpPresenter;
		if (m_majorPlacementMode == MajorPlacementMode.Full)
		{
			// In full mode we don't need to measure the presenter's size to determine placement.
			// By default Popup will give it infinite size, which we don't want for perf reasons.
			// The actual size will be set after the popup is loaded. For now we'll just set size to 1.
			presenterAsFE.Width = 1.0;
			presenterAsFE.Height = 1.0;
		}
		else
		{
			// Reset forced resizing of width/height to get content's auto dimensions.
			presenterAsFE.Width = double.NaN;
			presenterAsFE.Height = double.NaN;
		}

		m_presenterResized = false;

		OnOpening();

		if (m_openingCanceled)
		{
			return;
		}

		ApplyTargetPosition();

		if (m_shouldHideIfPointerMovesAway)
		{
			AddRootVisualPointerMovedHandler();
		}

		_popup.SetShouldTakeFocus(m_shouldTakeFocus);
		_popup.Opacity = 1.0;

		var placementMode = GetEffectivePlacement();
		m_majorPlacementMode = GetMajorPlacementFromPlacement(placementMode);
		m_preferredJustification = GetJustificationFromPlacementMode(placementMode);

		flyoutMetadata.SetOpenFlyout(this, Target);

		if (this.GetVisualTree() is { } visualTree)
		{
			// Currently we can't set Popup.XamlRoot here because that API doesn't allow the Popup to move between
			// two different XamlRoots/VisualTrees, and we want to support that here.  Instead, we use SetVisualTree
			// which doesn't enforce this.  This is safe as long as Popup isn't active.
			_popup.SetVisualTree(visualTree);
		}

		_popup.IsOpen = true;

		// Note: This is the call that propagates the SystemBackdrop object set on either this FlyoutBase or its
		// MenuFlyoutPresenter to the Popup. A convenient place to do this is in ForwardTargetPropertiesToPresenter, but we
		// see cases where the MenuFlyoutPresenter's SystemBackdrop property is null until it enters the tree via the
		// Popup::Open call above. So trying to propagate it before opening the popup actually finds no SystemBackdrop, and
		// the popup is left with a transparent background. Do the propagation after the popup opens instead. Windowed
		// popups support having a backdrop set after the popup is open.
		ForwardSystemBackdropToPopup();

		IsOpen = true;
	}

	internal void EnsurePopupAndPresenter()
	{
		if (_popup is null)
		{
			Popup spPopup = new();

			// Callback to derived class to create and initialize presenter instance.
			var spPresenter = CreatePresenter();

			if (spPresenter is FlyoutPresenter spPresenterAsFlyoutPresenter)
			{
				spPresenterAsFlyoutPresenter.SetOwnerFlyout(this);
			}

			PreparePresenter(spPresenter);

			// Hookup presenter to popup.
			spPopup.Child = spPresenter;

			spPopup.ShouldConstrainToRootBounds = ShouldConstrainToRootBounds;
			spPopup.AssociatedFlyout = this;

			spPopup.LostFocus += OnPopupLostFocus;

			// TODO Uno: WinUI closes from the presenter's Unloaded, raised asynchronously and even if Loaded wasn't raised.
			// Uno raises it synchronously in Popup.Close and only after Loaded. Popup.Closed has the timing of WinUI's Unloaded.
			spPopup.Closed += OnPopupClosed;

			m_tpPresenter = spPresenter;

			// Set IsDialog property to True for popup if inherited class is Flyout
			if (this is Flyout)
			{
				AutomationProperties.SetIsDialog(spPopup, true);
			}

			if (spPresenter.GetCurrentHighestValuePrecedence(AutomationProperties.AutomationIdProperty) == DependencyPropertyValuePrecedences.DefaultValue &&
				this.GetCurrentHighestValuePrecedence(AutomationProperties.AutomationIdProperty) != DependencyPropertyValuePrecedences.DefaultValue)
			{
				AutomationProperties.SetAutomationId(spPresenter, AutomationProperties.GetAutomationId(this));
			}

			if (spPresenter.GetCurrentHighestValuePrecedence(AutomationProperties.NameProperty) == DependencyPropertyValuePrecedences.DefaultValue &&
				this.GetCurrentHighestValuePrecedence(AutomationProperties.NameProperty) != DependencyPropertyValuePrecedences.DefaultValue)
			{
				AutomationProperties.SetName(spPresenter, AutomationProperties.GetName(this));
			}

			_popup = spPopup;

#if HAS_UNO
			// Forces the values to the popup, as it is not a child of the flyout in the visual tree.
			spPopup.SetValue(Popup.AllowFocusOnInteractionProperty, Boxer.Box(AllowFocusOnInteraction), precedence: DependencyPropertyValuePrecedences.Local);
			spPopup.SetValue(Popup.AllowFocusWhenDisabledProperty, Boxer.Box(AllowFocusWhenDisabled), precedence: DependencyPropertyValuePrecedences.Local);
#endif
		}

		Debug.Assert(m_tpPresenter is not null);
		Debug.Assert(_popup is not null);
	}

	private void SetPopupLightDismissBehavior()
	{
		_popup.IsLightDismissEnabled = m_isLightDismissOverlayEnabled;

		if (m_shouldOverlayPassThroughAllInput)
		{
			if (OverlayInputPassThroughElement is null)
			{
				if (m_wrRootVisual?.Target is UIElement rootVisual)
				{
					OverlayInputPassThroughElement = rootVisual;
					m_ownsOverlayInputPassThroughElement = true;
				}
			}
		}
		else if (m_ownsOverlayInputPassThroughElement)
		{
			OverlayInputPassThroughElement = null;
		}
	}

	private void ForwardTargetPropertiesToPresenter()
	{
		Debug.Assert(m_tpPresenter is not null);
		Debug.Assert(Target is not null);

		var spPresenterAsFE = m_tpPresenter;
		var placementTarget = Target;

		spPresenterAsFE.DataContext = placementTarget.DataContext;

		spPresenterAsFE.FlowDirection = placementTarget.FlowDirection;

		spPresenterAsFE.Language = placementTarget.Language;

		spPresenterAsFE.IsTextScaleFactorEnabled = GetIsTextScaleFactorEnabledInternal(placementTarget);

		var allowFocusOnInteraction = AllowFocusOnInteraction;

		// First check to see if FlyoutBase has the property set to false. If not, then pull from the placement target instead,
		// unless:
		//    - This flyout is acting as a sub-menu, in which case AllowFocusOnInteraction = true is required for keyboarding
		//      to function correctly when we open a sub-menu via pointer. Or,
		//    - This flyout is a MenuFlyout, which should always be free to take focus when it opens so that the user can use
		//      arrow keys to navigate between menu items immediately without first pressing Tab.
		//
		// Note that this value will get set on the presenter, which will allow the CPopup to set focus on its child when it
		// opens. This has the side effect of going through FocusManagerXamlIslandAdapter::SetFocus to also put Win32 focus
		// on this Xaml island's hwnd. That Win32 focus is necessary for the IInputKeyboardSource2 to raise the LostFocus
		// event, which we're relying on for light dismiss.
		if (allowFocusOnInteraction && this is not MenuFlyout)
		{
			bool isSubMenu = false;

			if (this is IMenu thisAsIMenu)
			{
				isSubMenu = thisAsIMenu.ParentMenu is not null;
			}

			if (!isSubMenu)
			{
				allowFocusOnInteraction = placementTarget.AllowFocusOnInteraction;
			}
		}

		spPresenterAsFE.AllowFocusOnInteraction = allowFocusOnInteraction;

		var allowFocusWhenDisabled = AllowFocusWhenDisabled;

		//First check to see if FlyoutBase has the property set to true. If not, then pull from the placement target instead
		if (!allowFocusWhenDisabled)
		{
			allowFocusWhenDisabled = placementTarget.AllowFocusWhenDisabled;
		}

		spPresenterAsFE.AllowFocusWhenDisabled = allowFocusWhenDisabled;

		ForwardThemeToPresenter();
	}

	// TODO Uno: FrameworkElement::get_IsTextScaleFactorEnabledInternal is virtual in WinUI, the property isn't on FrameworkElement.
	private static bool GetIsTextScaleFactorEnabledInternal(FrameworkElement element) => element switch
	{
		Control control => control.IsTextScaleFactorEnabled,
		TextBlock textBlock => textBlock.IsTextScaleFactorEnabled,
		RichTextBlock richTextBlock => richTextBlock.IsTextScaleFactorEnabled,
		ContentPresenter contentPresenter => contentPresenter.IsTextScaleFactorEnabled,
		_ => true,
	};

	private void ForwardThemeToPresenter()
	{
		var spPresenterAsFE = m_tpPresenter;

		// Ensure the Flyout matches the theme of its placement target.
		var requestedThemeProperty = FrameworkElement.RequestedThemeProperty;

		// We'll only override the requested theme on the presenter if its value hasn't been explicitly set.
		// Otherwise, we'll abide by its existing value.
		if (spPresenterAsFE.GetCurrentHighestValuePrecedence(requestedThemeProperty) == DependencyPropertyValuePrecedences.DefaultValue ||
			m_isFlyoutPresenterRequestedThemeOverridden)
		{
			var currentFlyoutPresenterTheme = spPresenterAsFE.RequestedTheme;
			var requestedTheme = ElementTheme.Default;

			// Walk up the tree from the placement target until we find an element with a RequestedTheme.
			DependencyObject spCurrent = Target;
			while (spCurrent is not null)
			{
				var spCurrentAsFE = spCurrent as FrameworkElement;

				requestedTheme = spCurrentAsFE?.RequestedTheme ?? ElementTheme.Default;

				if (requestedTheme != ElementTheme.Default)
				{
					break;
				}

				var spParent = Media.VisualTreeHelper.GetParent(spCurrent);
				if (spParent is PopupRoot)
				{
					// If the target is in a Popup and the Popup is in the Visual Tree, we want to inherrit the theme
					// from that Popup's parent. Otherwise we will get the App's theme, which might not be what
					// is expected.
					spParent = spCurrentAsFE?.Parent;
				}

				spCurrent = spParent;
			}

			if (requestedTheme != currentFlyoutPresenterTheme)
			{
				spPresenterAsFE.RequestedTheme = requestedTheme;
				m_isFlyoutPresenterRequestedThemeOverridden = true;
			}

			// Also set the popup's theme. If there is a SystemBackdrop on the menu, it'll be watching the theme on the
			// popup itself rather than the presenter set as the popup's child.
			_popup.RequestedTheme = requestedTheme;
		}
	}

	private void ForwardSystemBackdropToPopup()
	{
		// TODO Uno: Popup.SystemBackdrop is not implemented (see Popup.ApplyRootRoundedCornerClipToSystemBackdrop), so GetSystemBackdrop has nowhere to go:
		// FlyoutBase.SystemBackdrop takes precedence over MenuFlyoutPresenter.SystemBackdrop (FlyoutBase_partial.cpp lines 1612-1667).
	}

	private void ValidateAndSetParameters(FrameworkElement pPlacementTarget, out bool shouldOpen)
	{
		var placementMode = GetEffectivePlacement();
		m_preferredJustification = GetJustificationFromPlacementMode(placementMode);
		var majorplacementMode = GetMajorPlacementFromPlacement(placementMode);
		ValidateFlyoutPlacementMode(placementMode);

		var spPlacementTarget = pPlacementTarget;

		if (spPlacementTarget is null)
		{
			throw new ArgumentNullException(nameof(pPlacementTarget));
		}

		m_majorPlacementMode = majorplacementMode;
		SetPlacementTarget(pPlacementTarget);

		// Find out placement target bounds when it's guaranteed to be in visual tree in case if it goes away
		// (as would be the case in flyout invoked by element from another flyout).
		CalculatePlacementTargetBoundsPrivate(
			ref m_majorPlacementMode,
			spPlacementTarget,
			out m_placementTargetBounds,
			out m_allowPlacementFallbacks);

		var contentRoot = VisualTree.GetContentRootForElement(spPlacementTarget);

		if (contentRoot is not null)
		{
			m_inputDeviceTypeUsedToOpen = contentRoot.InputManager.LastInputDeviceType;
		}

		// If the placement target isn't in the live tree, then there's nothing to anchor ourselves on.
		// In that case, we'll silently not open the flyout.
		shouldOpen = spPlacementTarget.IsInLiveTree;
	}

	private void CheckAndHandleOpenFlyout(out bool shouldOpen)
	{
		shouldOpen = true;

		var flyoutMetadata = GetFlyoutMetadata();

		flyoutMetadata.GetOpenFlyout(out var openFlyout, out var openFlyoutPlacementTarget);

		if (openFlyout is not null)
		{
			shouldOpen = false;

			bool isSameFlyoutAsOpen = ReferenceEquals(openFlyout, this);
			bool isSameTargetAsOpen = ReferenceEquals(openFlyoutPlacementTarget, Target);

			bool isSamePosition =
				(!m_isTargetPositionSet && !m_wasTargetPositionSet) ||
				(m_isTargetPositionSet && m_wasTargetPositionSet &&
					m_targetPoint.X == m_lastTargetPoint.X &&
					m_targetPoint.Y == m_lastTargetPoint.Y);

			if (isSameFlyoutAsOpen && isSameTargetAsOpen && isSamePosition)
			{
				// Calling ShowAt on open FlyoutBase with the same placement target and at the same location.  Ignoring.
			}
			else
			{
				flyoutMetadata.GetStagedFlyout(out var stagedFlyout, out _);

				if (stagedFlyout is not null)
				{
					// There is an open AND staged FlyoutBase.  Since we honor the latest request,
					// update staged FlyoutBase to this.
					flyoutMetadata.SetStagedFlyout(this, Target);

					// Clear the staged flyout's parent reference to no longer mark it as a child flyout.
					stagedFlyout.m_tpParentFlyout = null;
				}
				else
				{
					// Either this is a different FlyoutBase or it is the same, but placement target is
					// different AND there is no outstanding request.  Hide previous instance and
					// queue this one to be shown.
					openFlyout.Hide();
					flyoutMetadata.SetStagedFlyout(this, Target);
				}
			}
		}

		m_wasTargetPositionSet = m_isTargetPositionSet;
		m_lastTargetPoint = m_targetPoint;
	}

	private void PerformPlacement(
		Size presenterSize,
		double bottomAppBarVerticalCorrection = 0.0,
		bool preparePopupTheme = true)
	{
		var effectivePlacementMode = GetMajorPlacementFromPlacement(g_defaultPlacementMode);
		var placementTargetFlowDirection = FlowDirection.LeftToRight;
		var pPopupNoRef = _popup;

		Debug.Assert(_popup is not null);
		Debug.Assert(m_tpPresenter is not null);

		var placementTarget = Target;

		if (placementTarget is null)
		{
			// Placement target has been reset, which means request can be ignored.
			return;
		}

		var pPresenterAsControl = m_tpPresenter;

		// Get available area to display Flyout, excluding space occupied by AppBars and IHM.
		var availableWindowRect = CalculateAvailableWindowRect(
			_popup,
			placementTarget,
			m_isTargetPositionSet,
			m_targetPoint,
			m_majorPlacementMode == MajorPlacementMode.Full);

		// If placement target is in live tree, get its coordinates.  If not, reuse ones cached when FlyoutBase
		// ShowAt was first called (support for scenario where flyout is invoked from element in another flyout).

		if (placementTarget.IsInLiveTree)
		{
			CalculatePlacementTargetBoundsPrivate(
				ref m_majorPlacementMode,
				placementTarget,
				out m_placementTargetBounds,
				out m_allowPlacementFallbacks,
				bottomAppBarVerticalCorrection);
		}

		placementTargetFlowDirection = placementTarget.FlowDirection;

		// Reverse meaning of left and right for RTL.
		// Popup's coordinates (0, 0) are always in upper-left hand corner.
		GetEffectivePlacementMode(
			m_majorPlacementMode,
			placementTargetFlowDirection,
			out effectivePlacementMode);

		bool isPlacementPropertyLocal = ReadLocalValue(PlacementProperty) != DependencyProperty.UnsetValue;

		if (!isPlacementPropertyLocal)
		{
			AutoAdjustPlacement(ref effectivePlacementMode);
		}

		var minPresenterSize = GetMinimumSize(pPresenterAsControl);
		var maxPresenterSize = GetMaximumSize(pPresenterAsControl);

		CalculatePlacementPrivate(
			ref effectivePlacementMode,
			m_preferredJustification,
			m_allowPlacementFallbacks,
			m_allowPresenterResizing,
			m_placementTargetBounds,
			presenterSize,
			minPresenterSize,
			maxPresenterSize,
			availableWindowRect,
			IsWindowedPopup(),
			out var presenterRect);

		// Position popup.
		if (m_isTargetPositionSet)
		{
			double showAtVerticalOffset = _popup.VerticalOffset;
			presenterRect = UpdateTargetPosition(availableWindowRect, presenterSize);

			// If this flyout has a target position set, the placement is only used
			// to determine the direction of the MenuFlyout transition which can animate
			// either from Top or Bottom.
			effectivePlacementMode =
				(showAtVerticalOffset > presenterRect.Y) ?
				MajorPlacementMode.Top :
				MajorPlacementMode.Bottom;
		}
		else
		{
			if (placementTargetFlowDirection == FlowDirection.LeftToRight)
			{
				_popup.HorizontalOffset = presenterRect.X;
			}
			else
			{
				_popup.HorizontalOffset = presenterRect.X + presenterRect.Width;
			}

			_popup.VerticalOffset = presenterRect.Y;
		}

		if (presenterSize.Width != presenterRect.Width)
		{
			pPresenterAsControl.Width = presenterRect.Width;
			m_presenterResized = true;
		}

		if (presenterSize.Height != presenterRect.Height)
		{
			pPresenterAsControl.Height = presenterRect.Height;
			m_presenterResized = true;
		}

		// We don't want our popup to grab the focus since we will be focusing the flyout ourselves.
		pPopupNoRef.SetShouldTakeFocus(false);

		if (preparePopupTheme)
		{
			// Note: transitions already respect flow direction, so we undo the flow-direction change to
			// effective placement before setting up the transition.
			GetEffectivePlacementMode(
				effectivePlacementMode,
				placementTargetFlowDirection,
				out effectivePlacementMode);

			PreparePopupTheme(
				pPopupNoRef,
				effectivePlacementMode,
				Target);
		}

		OnPlacementUpdated(effectivePlacementMode);
	}

	private void PreparePresenter(Control pPresenter)
	{
		Debug.Assert(pPresenter is not null);

		pPresenter.SizeChanged += OnPresenterSizeChanged;
		pPresenter.Loaded += OnPresenterLoaded;

		// TODO Uno: Presenter's Unloaded, see OnPopupClosed in EnsurePopupAndPresenter.
	}

	// TODO Uno: Popup.ChildTransitions, PopupThemeTransition and PickerFlyoutThemeTransition are not implemented,
	// so a flyout opens without its entrance transition.
	private protected virtual void PreparePopupTheme(
		Popup pPopup,
		MajorPlacementMode placementMode,
		FrameworkElement pPlacementTarget)
	{
#if false
		if (m_tpThemeTransition is null && AreOpenCloseAnimationsEnabled)
		{
			Transition spTransition;
			TransitionCollection spTransitionCollection = new();

			if (m_usePickerFlyoutTheme)
			{
				spTransition = new PickerFlyoutThemeTransition();
			}
			else
			{
				PopupThemeTransition spPopupThemeTransition = new();
				spTransition = spPopupThemeTransition;

				spPopupThemeTransition.SetOverlayElement(pPopup.OverlayElement);

				SetTransitionParameters(spTransition, placementMode);
			}

			spTransitionCollection.Add(spTransition);

			// Deliverable 19819460: Allow LTEs to target Popup.Child when Popup is windowed
			// ThemeTransition can't be applied to child of a windowed popup, targeting grandchild here.
			if (IsWindowedPopup())
			{
				if (pPopup.Child is { } popupChild && Media.VisualTreeHelper.GetChild(popupChild, 0) is UIElement popupGrandChildAsUIE)
				{
					popupGrandChildAsUIE.Transitions = spTransitionCollection;
					popupGrandChildAsUIE.InvalidateMeasure();
				}
			}
			else
			{
				pPopup.ChildTransitions = spTransitionCollection;
			}

			m_tpThemeTransition = spTransition;
		}
#endif
	}

#if false // TODO Uno: See PreparePopupTheme.
	private void SetTransitionParameters(
		Transition pTransition,
		MajorPlacementMode placementMode)
	{
		Debug.Assert(pTransition is not null);

		if (pTransition is PopupThemeTransition spTransitionAsPopupThemeTransition)
		{
			switch (placementMode)
			{
				case MajorPlacementMode.Full:
					spTransitionAsPopupThemeTransition.FromHorizontalOffset = 0.0;
					spTransitionAsPopupThemeTransition.FromVerticalOffset = 0.0;
					break;

				case MajorPlacementMode.Top:
					spTransitionAsPopupThemeTransition.FromHorizontalOffset = 0.0;
					spTransitionAsPopupThemeTransition.FromVerticalOffset = g_entranceThemeOffset;
					break;

				case MajorPlacementMode.Bottom:
					spTransitionAsPopupThemeTransition.FromHorizontalOffset = 0.0;
					spTransitionAsPopupThemeTransition.FromVerticalOffset = -g_entranceThemeOffset;
					break;

				case MajorPlacementMode.Left:
					spTransitionAsPopupThemeTransition.FromHorizontalOffset = g_entranceThemeOffset;
					spTransitionAsPopupThemeTransition.FromVerticalOffset = 0.0;
					break;

				case MajorPlacementMode.Right:
					spTransitionAsPopupThemeTransition.FromHorizontalOffset = -g_entranceThemeOffset;
					spTransitionAsPopupThemeTransition.FromVerticalOffset = 0.0;
					break;

				default:
					Debug.Fail("Unsupported MajorPlacementMode");
					throw new InvalidOperationException("Unsupported MajorPlacementMode");
			}
		}
	}
#endif

	private void RaiseOpenedEvent() => Opened?.Invoke(this, EventArgs.Empty);

	private protected virtual void OnClosing(ref bool cancel)
	{
		cancel = false;

		FlyoutBaseClosingEventArgs eventArgs = new();

		Closing?.Invoke(this, eventArgs);

		bool cancelRequested = eventArgs.Cancel;

		cancel = cancelRequested;

		if (!cancelRequested)
		{
			ElementSoundPlayer.RequestInteractionSoundForElement(ElementSoundKind.Hide, this);
		}
	}

	private protected virtual void OnClosed()
	{
		m_isTargetPositionSet = false;
		InputDevicePrefersPrimaryCommands = false;

		// Now that the flyout has closed, reset its m_hasPlacementOverride flag so that
		// it will be valid the next time it opens again.
		m_hasPlacementOverride = false;

		// Also reset the m_isLightDismissOverlayEnabled flag to its default TRUE value
		// so that the light dismiss overlay is recreated as needed when reopening.
		m_isLightDismissOverlayEnabled = true;

#if HAS_UNO
		// The presenter outlives the flyout's opening: without this, a shared flyout keeps the last target's DataContext alive.
		m_tpPresenter?.ClearValue(FrameworkElement.DataContextProperty);
#endif

		RaiseClosedEvent();
	}

	private void RaiseClosedEvent() => Closed?.Invoke(this, EventArgs.Empty);

	private protected virtual void OnOpening()
	{
		switch (m_inputDeviceTypeUsedToOpen)
		{
			case InputDeviceType.None:
			case InputDeviceType.Mouse:
			case InputDeviceType.Keyboard:
			case InputDeviceType.GamepadOrRemote:
				InputDevicePrefersPrimaryCommands = false;
				break;
			case InputDeviceType.Touch:
			case InputDeviceType.Pen:
				InputDevicePrefersPrimaryCommands = true;
				break;
		}

		ElementSoundPlayer.RequestInteractionSoundForElement(ElementSoundKind.Show, this);

		RaiseOpeningEvent();
	}

	private void RaiseOpeningEvent()
	{
		m_openingCanceled = false;

		Opening?.Invoke(this, EventArgs.Empty);
	}

	private void OnPresenterSizeChanged(object pSender, SizeChangedEventArgs pArgs)
	{
		// Perform placement when size of presenter is known.
		var presenterSize = pArgs.NewSize;

		if (!m_presenterResized)
		{
			PerformPlacement(presenterSize);
		}
	}

	private void OnPresenterLoaded(object pSender, RoutedEventArgs pArgs)
	{
		var allowFocusOnInteraction = AllowFocusOnInteraction;
		DependencyObject target = this;

		// First check to see if FlyoutBase has the property set to false. If not, then pull from the presenter instead,
		// if we have one - in rare occasions we may not, if OnPresenterLoaded() is called immediately after OnPresenterUnloaded()
		// without any call to ShowAt(), and we don't want to crash in such a circumstance.
		if (allowFocusOnInteraction && Target is { } placementTarget)
		{
			target = placementTarget;
		}

		if (m_shouldTakeFocus)
		{
			var contentRoot = VisualTree.GetContentRootForElement(target);
			var inputManager = contentRoot.InputManager;

			// Propagate focus to the first focusable child.
			var focusState = contentRoot.FocusManager.GetFocusStateFromInputDeviceType(m_inputDeviceTypeUsedToOpen);

			// In some islands scenarios (e.g., Win+X menu) the app calls ShowAt in response to input that XAML is not aware of (because
			// it's happening outside the island)  Since XAML won't know then what the last input device _really_ was, we update XAML's
			// notion of last input device with what the system has here, and open the Flyout with the appropriate FocusState.
			// We only care about this for windowed popups.
			// TODO Uno: CPopup::GetShouldShowKeyboardCues for a windowed popup in a XamlIslandRoot: a popup's content has no
			// input focus controller, and a window's root isn't told about input outside of it.

			// If the user is using a UIA client such as Narrator, we want to treat things as if the device used to open
			// the flyout was a keyboard. Otherwise, AllowFocusOnInteraction being false might result in focus not moving into the
			// flyout for a Narrator user.
			if (inputManager.GetWasUIAFocusSetSinceLastInput())
			{
				focusState = FocusState.Keyboard;
			}

			// Note: Normally we check whether the target has AllowFocusOnInteraction set before having the flyout take
			// focus. The exception is MenuFlyout, which is allowed to take focus regardless of what its target's
			// AllowFocusOnInteraction value is. We do this so the user can immediately navigate between menu items with the
			// arrow keys after opening it. This covers cases like MenuFlyouts opening from AppBarButtons, where the target
			// AppBarButton will have AllowFocusOnInteraction="False" in the default template.
			if (FocusSelection.ShouldUpdateFocus(target, focusState)
				|| (allowFocusOnInteraction && this is MenuFlyout))
			{
				var childFocused = m_tpPresenter.Focus(focusState);

				if (!childFocused)
				{
					// If we failed to focus the flyout, focus the popup itself.
					// If the focused child or the flyout itself has AllowFocusOnInteraction set to false, do not try and set focus
					_popup.Focus(focusState);
				}
			}
		}

		RaiseOpenedEvent();

		// TODO Uno: A reopened presenter that keeps its size raises no SizeChanged, as its size survives closing.
		if (_popup?.IsOpen == true && !m_presenterResized)
		{
			PerformPlacement(GetPresenterSize());
		}
	}

	private void OnPopupClosed(object sender, object e) => OnPresenterUnloaded();

	// Raise closed event and open staged Flyout in case one exists.
	private protected void OnPresenterUnloaded()
	{
		bool wasTargetPositionSet = m_isTargetPositionSet;
		var targetPoint = m_targetPoint;

		IsOpen = false;

		OnClosed();

		var flyoutMetadata = GetFlyoutMetadata();

		flyoutMetadata.SetOpenFlyout(null, null);

		flyoutMetadata.GetStagedFlyout(out var stagedFlyout, out _);

		// If our presenter was unloaded as part of tearing down the island, then don't try to reopen the staged flyout.
		var stagedFlyoutVisualTree = stagedFlyout?.GetVisualTree();

		if (stagedFlyout is not null && stagedFlyoutVisualTree is not null)
		{
			// If not reopening, clear the reference to placement target.  If it is reopening, it will be cleared later.
			// This state can be reached if open flyout is re-opened with a different placement target.
			if (!ReferenceEquals(stagedFlyout, this))
			{
				SetPlacementTarget(null);

				// Clear our parent reference to no longer mark this flyout as a child flyout.
				m_tpParentFlyout = null;
			}
			else
			{
				// If we're reopening a flyout, preserve target position
				Debug.Assert(targetPoint.X == m_targetPoint.X && targetPoint.Y == m_targetPoint.Y);
				m_isTargetPositionSet = wasTargetPositionSet;
			}

			flyoutMetadata.SetStagedFlyout(null, null);
			stagedFlyout.Open();
		}
		else
		{
			SetPlacementTarget(null);

			// Clear our parent reference to no longer mark this flyout as a child flyout.
			m_tpParentFlyout = null;
		}
	}

	private void OnPopupLostFocus(object pSender, RoutedEventArgs pArgs)
	{
		bool isOpen = false;

		if (_popup is not null)
		{
			isOpen = _popup.IsOpen;
		}

		if (isOpen)
		{
			var focusedElement = VisualTree.GetContentRootForElement(this)?.FocusManager.FocusedElement;

			// We only want to close when we lose focus in the case where the newly focused element
			// does not reside in any other flyout - otherwise, this will conflict with
			// the functionality of child flyouts.
			// Note that tap-and-hold can cause the root scroll viewer to take focus temporarily,
			// which we don't want to respond to, so we'll ignore that.
			// TODO Uno: There is no RootScrollViewer.
			if (focusedElement is not null)
			{
				var currentObject = focusedElement;
				var currentElement = currentObject as UIElement;

				// If the focused element is a text element (which is not a UI element),
				// then we need to get its containing element.
				if (currentElement is null)
				{
					if (currentObject is Documents.TextElement currentTextElement)
					{
						currentElement = currentTextElement.GetContainingFrameworkElement();
					}
				}

				Popup popupAncestor = null;
				bool popupRootExists = false;
				bool popupAncestorIsLightDismiss = false;
				bool popupAncestorHasComboBoxLightDismiss = false;

				while (currentElement is not null)
				{
					if (currentElement is PopupRoot)
					{
						popupRootExists = true;
						currentElement = currentElement.GetUIElementAdjustedParentInternal(false);
						continue;
					}

					if (currentElement is Popup elementCast)
					{
						popupAncestor = elementCast;

						// Check if this ancestor popup is a light dismiss one.
						popupAncestorIsLightDismiss = popupAncestor.IsSelfOrAncestorLightDismiss;

						if (!popupAncestorIsLightDismiss)
						{
							// A ComboBox dropdown is not marked with m_fIsLightDismiss==True but has a grandchild of type CComboBoxLightDismiss.
							// Check if the ancestor popup is one of those.
							popupAncestorHasComboBoxLightDismiss = HasComboBoxLightDismiss(elementCast);
						}
						break;
					}

					currentElement = currentElement.GetUIElementAdjustedParentInternal(false);
				}

				// In the case of XamlIslandRoots, it may be the case that we can't retrieve a popup object.
				// In that case, we'll assume that it was in a popup if we found the popup root.
				// We can't tell if it was a light-dismiss popup in that case, but we'll err on the side
				// of not hiding in that circumstance.
				if (!popupRootExists && (popupAncestor is null || (!popupAncestorIsLightDismiss && !popupAncestorHasComboBoxLightDismiss)))
				{
					Hide();
				}
			}
		}
	}

	// Hides the flyout in TransientWithDismissOnPointerMoveAway mode when the pointer moves more than 80 pixels away.
	private void OnRootVisualPointerMoved(object pSender, PointerRoutedEventArgs pArgs)
	{
		Debug.Assert(_popup is not null);
		Debug.Assert(_popup.IsOpen);

		// The flyout is hidden when the pointer is moved more than 80 pixels
		// away from its bounds. This is the square of that threshold distance.
		const double hidingThreshold = 6400;

		var pointerPosition = pArgs.GetCurrentPoint(null).Position;

		var presenterSize = GetPresenterSize();

		var transformToRoot = m_tpPresenter.TransformToVisual(null);

		var rootPresenterBounds = transformToRoot.TransformBounds(new Rect(0, 0, presenterSize.Width, presenterSize.Height));

		double left = rootPresenterBounds.X;
		double top = rootPresenterBounds.Y;
		double right = left + rootPresenterBounds.Width;
		double bottom = top + rootPresenterBounds.Height;
		double xDistance = 0;
		double yDistance = 0;

		if (pointerPosition.X < left)
		{
			xDistance = left - pointerPosition.X;
		}
		else if (pointerPosition.X > right)
		{
			xDistance = pointerPosition.X - right;
		}

		if (pointerPosition.Y < top)
		{
			yDistance = top - pointerPosition.Y;
		}
		else if (pointerPosition.Y > bottom)
		{
			yDistance = pointerPosition.Y - bottom;
		}

		if (xDistance * xDistance + yDistance * yDistance > hidingThreshold)
		{
			Hide();
		}
	}

	// Perform placement on a control using fallbacks.
	private static void CalculatePlacementPrivate(
		ref MajorPlacementMode pMajorPlacement,
		PreferredJustification justification,
		bool allowFallbacks,
		bool allowPresenterResizing,
		Rect placementTargetBounds,
		Size controlSize,
		Size minControlSize,
		Size maxControlSize,
		Rect containerRect,
		bool isWindowed,
		out Rect pControlRect)
	{
		pControlRect = default;

		// Full placement is not relative to the placement target, so there's no need
		// to check for fallbacks or make any further adjustments.
		if (pMajorPlacement == MajorPlacementMode.Full)
		{
			pControlRect.X = containerRect.X;
			pControlRect.Y = containerRect.Y;
			pControlRect.Width = containerRect.Width;
			pControlRect.Height = containerRect.Height;

			// if the presenterRect.Width is greater than maxWidth, the final width will be cut to
			// maxWidth and then a bigger margin will appear on the right.
			// Move the presenter to center. Same logic applies for Height.
			if (maxControlSize.Width < pControlRect.Width)
			{
				pControlRect.X += (pControlRect.Width - maxControlSize.Width) / 2;
				pControlRect.Width = maxControlSize.Width;
			}
			if (maxControlSize.Height < pControlRect.Height)
			{
				pControlRect.Y += (pControlRect.Height - maxControlSize.Height) / 2;
				pControlRect.Height = maxControlSize.Height;
			}
		}
		else
		{
			MajorPlacementMode[] placementOrder = new MajorPlacementMode[4];

			pControlRect.X = 0.0;
			pControlRect.Y = 0.0;
			pControlRect.Width = controlSize.Width;
			pControlRect.Height = controlSize.Height;

			var controlSizeWithMargins = new Size(controlSize.Width, controlSize.Height);

			var minControlSizeWithMargins = new Size(minControlSize.Width, minControlSize.Height);

			switch (pMajorPlacement)
			{
				case MajorPlacementMode.Top:
					placementOrder[0] = MajorPlacementMode.Top;
					placementOrder[1] = MajorPlacementMode.Bottom;
					placementOrder[2] = MajorPlacementMode.Left;
					placementOrder[3] = MajorPlacementMode.Right;
					break;

				case MajorPlacementMode.Bottom:
					placementOrder[0] = MajorPlacementMode.Bottom;
					placementOrder[1] = MajorPlacementMode.Top;
					placementOrder[2] = MajorPlacementMode.Left;
					placementOrder[3] = MajorPlacementMode.Right;
					break;

				case MajorPlacementMode.Left:
					placementOrder[0] = MajorPlacementMode.Left;
					placementOrder[1] = MajorPlacementMode.Right;
					placementOrder[2] = MajorPlacementMode.Top;
					placementOrder[3] = MajorPlacementMode.Bottom;
					break;

				case MajorPlacementMode.Right:
					placementOrder[0] = MajorPlacementMode.Right;
					placementOrder[1] = MajorPlacementMode.Left;
					placementOrder[2] = MajorPlacementMode.Top;
					placementOrder[3] = MajorPlacementMode.Bottom;
					break;

				default:
					throw new InvalidOperationException("Unsupported MajorPlacementMode");
			}

			if (!PerformPlacementWithFallback(
					placementTargetBounds,
					controlSizeWithMargins,
					containerRect,
					placementOrder,
					allowFallbacks ? placementOrder.Length : 1,
					justification,
					out var controlPos,
					out pMajorPlacement))
			{
				if (allowPresenterResizing)
				{
					ResizeToFit(
						ref pMajorPlacement,
						placementTargetBounds,
						containerRect,
						minControlSizeWithMargins,
						ref controlPos,
						ref controlSizeWithMargins);
				}
				pControlRect.Width = Math.Max(0.0, controlSizeWithMargins.Width);
				pControlRect.Height = Math.Max(0.0, controlSizeWithMargins.Height);
			}

			pControlRect.X = controlPos.X;
			pControlRect.Y = controlPos.Y;

			// We want there to be a margin between the flyout and the UI element it's attached to.  To accomplish
			// that, we add a margin only in the direction in which the flyout is appearing.
			switch (pMajorPlacement)
			{
				case MajorPlacementMode.Top:
					pControlRect.Y -= FlyoutMargin;
					break;

				case MajorPlacementMode.Bottom:
					pControlRect.Y += FlyoutMargin;
					break;

				case MajorPlacementMode.Left:
					pControlRect.X -= FlyoutMargin;
					break;

				case MajorPlacementMode.Right:
					pControlRect.X += FlyoutMargin;
					break;

				default:
					throw new InvalidOperationException("Unsupported MajorPlacementMode");
			}

			// If the flyout is windowed, then a xy-position less than 0 is acceptable - that means we're displaying above or to the left of the window.
			if (!isWindowed)
			{
				pControlRect.X = Math.Max(0.0, pControlRect.X);
				pControlRect.Y = Math.Max(0.0, pControlRect.Y);
			}
		}
	}

	// Calculate bounding rectangle of the placement target taking into consideration whether target is on AppBar.
	private static void CalculatePlacementTargetBoundsPrivate(
		ref MajorPlacementMode pPlacement,
		FrameworkElement pPlacementTarget,
		out Rect pPlacementTargetBounds,
		out bool pAllowFallbacks,
		double bottomAppBarVerticalCorrection = 0.0)
	{
		var appBarMode = AppBarMode.Floating;
		bool isTopOrBottomAppBar = false;

		pAllowFallbacks = false;
		pPlacementTargetBounds = new Rect(0, 0, pPlacementTarget.ActualWidth, pPlacementTarget.ActualHeight);

		var spAppBar = FindParentAppBar(pPlacementTarget);

		if (spAppBar is not null)
		{
			appBarMode = spAppBar.GetMode();
			isTopOrBottomAppBar = appBarMode == AppBarMode.Top || appBarMode == AppBarMode.Bottom;
		}

		if (!isTopOrBottomAppBar || (pPlacement == MajorPlacementMode.Full))
		{
			// Non-AppBar case and non-Top/BottomAppBar and FullPlacementMode, transform target to root screen coordinates.
			var spTargetToRootTransform = pPlacementTarget.TransformToVisual(null);
			pPlacementTargetBounds = spTargetToRootTransform.TransformBounds(pPlacementTargetBounds);
			pAllowFallbacks = true;
		}
		else
		{
			// For Top/Bottom AppBars, expand target bounds to AppBar size and transform to root screen coordinates.
			var spTargetToAppBarTransform = pPlacementTarget.TransformToVisual(spAppBar);
			var placementTargetBoundsInAppBar = spTargetToAppBarTransform.TransformBounds(pPlacementTargetBounds);

			placementTargetBoundsInAppBar.Y = 0.0;
			placementTargetBoundsInAppBar.Height = spAppBar.ActualHeight;

			var spAppBarToRootTransform = spAppBar.TransformToVisual(null);
			pPlacementTargetBounds = spAppBarToRootTransform.TransformBounds(placementTargetBoundsInAppBar);

			// Explicitly override placement mode for AppBar contained targets.
			if (appBarMode == AppBarMode.Top)
			{
				pPlacement = MajorPlacementMode.Bottom;
			}
			else if (appBarMode == AppBarMode.Bottom)
			{
				pPlacement = MajorPlacementMode.Top;

				// When the InputPaneState changes to Hidden, the bottom AppBar is still laid out at its
				// old position above where the InputPane was, and thus the PerformPlacement() in
				// NotifyInputPaneStateChanged() must correct for the height of the InputPane.
				pPlacementTargetBounds.Y += bottomAppBarVerticalCorrection;
			}
			else
			{
				Debug.Fail("Unsupported AppBarMode");
				throw new InvalidOperationException("Unsupported AppBarMode");
			}
		}
	}

	// TODO Uno: Returns the flyout it hid, for InputManager.FlyoutClosedByCurrentPress.
	internal static FlyoutBase CloseOpenFlyout(FlyoutBase parentFlyoutCore = null)
	{
		FlyoutMetadata flyoutMetadata = parentFlyoutCore is null
			? s_flyoutMetadata
			: parentFlyoutCore.m_childFlyoutMetadata;

		FlyoutBase openFlyout = null;

		if (flyoutMetadata is not null)
		{
			// Clear the staged flyout before we hide the open flyout to make sure we don't
			// end up just opening a new flyout.
			flyoutMetadata.SetStagedFlyout(null, null);

			flyoutMetadata.GetOpenFlyout(out openFlyout, out _);

			openFlyout?.Hide();
		}

		return openFlyout;
	}

	internal static void HideFlyout(FlyoutBase flyout) => flyout?.Hide();

	internal static Rect CalculateAvailableMonitorRect(UIElement targetElement, Point targetPointClientLogical) => Popup.CalculateAvailableMonitorRect(targetElement.XamlRoot, targetPointClientLogical);

	private static Rect GetVisibleBounds(Popup popup, FrameworkElement placementTarget)
	{
		var xamlRoot = XamlRoot.GetImplementationForElement(popup) ?? placementTarget?.XamlRoot;
		return xamlRoot?.VisualTree.VisibleBounds ?? default;
	}

	internal static Rect CalculateAvailableWindowRect(
		Popup popup,
		FrameworkElement placementTarget,
		bool hasTargetPosition,
		Point targetPosition,
		bool isFull)
	{
		Rect pAvailableRect;

		// If the popup is windowed, then the available window rect is the monitor that it will appear in.
		// The exception is if we're opening in full mode, in which case we want it to overlay the content area,
		// not the entire monitor area.
		if (!isFull && popup.IsWindowed)
		{
			Point popupPosition;

			if (hasTargetPosition)
			{
				popupPosition = targetPosition;
			}
			else
			{
				Debug.Assert(placementTarget is not null);

				var centerOfTarget = new Point(placementTarget.ActualWidth / 2.0, placementTarget.ActualHeight / 2.0);

				var transformToRoot = placementTarget.TransformToVisual(null);
				centerOfTarget = transformToRoot.TransformPoint(centerOfTarget);

				popupPosition = centerOfTarget;
			}

			pAvailableRect = Popup.CalculateAvailableMonitorRect(popup.XamlRoot ?? placementTarget?.XamlRoot, popupPosition);
		}
		else
		{
			// Apply the visible bounds for calculating the available bounds area
			// that excludes the system tray and soft steering wheel navigation bar.
			pAvailableRect = GetVisibleBounds(popup, placementTarget);

			// TODO Uno: Missing Uno equivalent: IApplicationBarService::GetAppBarStatus, which subtracts the open top and
			// bottom app bars, and DXamlCore::GetInputPaneOccludeRect, which subtracts the input pane.

			pAvailableRect.Height = Math.Max(0.0, pAvailableRect.Height);
		}

		return pAvailableRect;
	}

	// Convenience method which sets presenter style or clears the property if passed pStyle is null.
	private protected static void SetPresenterStyle(
		Control pPresenter,
		Style pStyle)
	{
		Debug.Assert(pPresenter is not null);

		if (pStyle is not null)
		{
			pPresenter.Style = pStyle;
		}
		else
		{
			pPresenter.ClearValue(FrameworkElement.StyleProperty);
		}
	}

	// React to the IHM (touch keyboard) showing or hiding.
	// Flyout should always be shown above the IHM.
	// TODO Uno: Missing Uno equivalent of InputPaneHandler, nothing calls this yet.
	internal void NotifyInputPaneStateChange(
		InputPaneState inputPaneState,
		Rect inputPaneBounds)
	{
		double bottomAppBarVerticalCorrection = 0.0;
		var zoomScale = XamlRoot?.RasterizationScale ?? 1.0;

		var presenterSize = GetPresenterSize();

		if (inputPaneState == InputPaneState.InputPaneShowing)
		{
			m_cachedInputPaneHeight = inputPaneBounds.Height;
			bottomAppBarVerticalCorrection = -m_cachedInputPaneHeight / zoomScale;
		}
		else if (inputPaneState == InputPaneState.InputPaneHidden)
		{
			bottomAppBarVerticalCorrection = m_cachedInputPaneHeight / zoomScale;

			if (m_presenterResized && m_majorPlacementMode != MajorPlacementMode.Full)
			{
				// The presenter's size was reduced by us to fit it on screen.
				// Now that the IHM is going away, let it size itself again.
				// Reset forced resizing of width/height to get content's auto dimensions.
				//
				// This is not needed for the case where the InputPane is being shown
				// since the Flyout can only get smaller in that case and the PerformPlacement()
				// call below will size it appropriately.
				m_tpPresenter.Width = double.NaN;
				m_tpPresenter.Height = double.NaN;
				m_presenterResized = false;
			}
		}

		PerformPlacement(presenterSize, bottomAppBarVerticalCorrection);
	}

	// Helper method to set/clear m_tpPlacementTarget and maintain associated state and event handlers.
	private void SetPlacementTarget(FrameworkElement pPlacementTarget)
	{
		// Clear the old placement target if it exists.
		m_placementTargetSubscriptions.Disposable = null;
		Target = null;

		if (pPlacementTarget is not null)
		{
			Target = pPlacementTarget;

			// Hook up an event handler that will dismiss the flyout if the placement target gets unloaded.
			RoutedEventHandler onUnloaded = OnPlacementTargetUnloaded;
			pPlacementTarget.Unloaded += onUnloaded;

			// Hook up an event handler that will forward any theme changes to the flyout.
			pPlacementTarget.ActualThemeChanged += OnPlacementTargetActualThemeChanged;

			m_placementTargetSubscriptions.Disposable = Disposable.Create(() =>
			{
				pPlacementTarget.Unloaded -= onUnloaded;
				pPlacementTarget.ActualThemeChanged -= OnPlacementTargetActualThemeChanged;
			});
		}
	}

	// TODO Uno: WinUI raises Unloaded asynchronously, so a flyout whose target left with the parent flyout has detached by then.
	private void OnPlacementTargetUnloaded(object sender, RoutedEventArgs args) =>
		NativeDispatcher.Main.Enqueue(() =>
		{
			if (ReferenceEquals(Target, sender))
			{
				Hide();
			}
		}, NativeDispatcherPriority.Normal);

	private void OnPlacementTargetActualThemeChanged(FrameworkElement sender, object args) => ForwardThemeToPresenter();

	internal void PlaceFlyoutForDateTimePicker(Point point)
	{
		m_isPositionedForDateTimePicker = true;

		ForwardPopupFlowDirection();
		SetTargetPosition(point);
		ApplyTargetPosition();
		var presenterSize = GetPresenterSize();
		PerformPlacement(presenterSize);

#if false // TODO Uno: See PreparePopupTheme.
		if (m_tpThemeTransition is PickerFlyoutThemeTransition spTransitionAsPickerFlyoutThemeTransition)
		{
			double popupOffset = _popup.VerticalOffset;
			Point centerOfTarget = default;

			if (Target is { } placementTarget)
			{
				var spTransformFromTargetToWindow = placementTarget.TransformToVisual(null);
				centerOfTarget = spTransformFromTargetToWindow.TransformPoint(centerOfTarget);

				centerOfTarget.Y += placementTarget.ActualHeight / 2.0;
			}

			double center = popupOffset + presenterSize.Height / 2;
			double offsetFromCenter = centerOfTarget.Y - center;
			spTransitionAsPickerFlyoutThemeTransition.OpenedLength = presenterSize.Height;
			spTransitionAsPickerFlyoutThemeTransition.OffsetFromCenter = offsetFromCenter;
		}
#endif
	}

	// Hooks up the root visual's PointerMoved event so the flyout can be automatically
	// hidden in TransientWithDismissOnPointerMoveAway mode.
	private void AddRootVisualPointerMovedHandler()
	{
		if (!m_isRootVisualPointerMovedAttached && m_wrRootVisual?.Target is UIElement rootVisual)
		{
			rootVisual.PointerMoved += OnRootVisualPointerMoved;
			m_isRootVisualPointerMovedAttached = true;
		}
	}

	// Unhooks the root visual's PointerMoved event.
	private void RemoveRootVisualPointerMovedHandler()
	{
		if (m_isRootVisualPointerMovedAttached)
		{
			if (m_wrRootVisual?.Target is UIElement rootVisual)
			{
				rootVisual.PointerMoved -= OnRootVisualPointerMoved;
			}

			m_isRootVisualPointerMovedAttached = false;
		}
	}

	public static void ShowAttachedFlyout(FrameworkElement flyoutOwner)
	{
		var spFlyoutBase = GetAttachedFlyout(flyoutOwner);

		spFlyoutBase?.ShowAt(flyoutOwner);
	}

	private void ForwardPopupFlowDirection()
	{
		if (_popup is not null)
		{
			// Propagate the target flow direction to the Popup
			// The popup is reused across openings, so a missing target resets the direction rather than keeping the previous target's.
			_popup.FlowDirection = Target?.FlowDirection ?? FlowDirection.LeftToRight;
		}
	}

	private void SetTargetPosition(Point targetPoint)
	{
		m_isTargetPositionSet = true;
		m_targetPoint = targetPoint;
	}

	private void ApplyTargetPosition()
	{
		if (m_isTargetPositionSet && _popup is not null)
		{
			_popup.HorizontalOffset = m_targetPoint.X;
			_popup.VerticalOffset = m_targetPoint.Y;
		}
	}

	internal void SetIsWindowedPopup()
	{
		EnsurePopupAndPresenter();

		// Set popup to be windowed, if we can, in order to support rendering the popup out of the XAML window.
		if (Popup.DoesPlatformSupportWindowedPopup())
		{
			// Set popup to be windowed, if we can, in order to support rendering the popup out of the XAML window.
			if (_popup is not null && !_popup.IsWindowed)
			{
				if (!_popup.WasEverOpened)
				{
					_popup.SetIsWindowed();
				}
			}
		}
	}

	internal bool IsWindowedPopup()
	{
		bool areWindowedPopupsSupported = Popup.DoesPlatformSupportWindowedPopup();
		bool isPopupWindowed = _popup?.IsWindowed ?? false;

		return areWindowedPopupsSupported && (isPopupWindowed || m_openingWindowedInProgress);
	}

	// TODO Uno: Missing Uno equivalent of SystemParametersInfo(SPI_GETHANDEDNESS), which falls back to a left-handed menu when it fails.
	private static bool IsRightHandedHandedness() => false;

	private Rect UpdateTargetPosition(
		Rect availableWindowRect,
		Size presenterSize)
	{
		double horizontalOffset = 0.0;
		double verticalOffset = 0.0;
		double maxWidth = double.NaN;
		double maxHeight = double.NaN;
		var flowDirection = FlowDirection.LeftToRight;
		var targetFlowDirection = FlowDirection.LeftToRight;
		bool isMenuFlyout = this is MenuFlyout;
		bool preferTopPlacement = false;

		Debug.Assert(_popup is not null);
		Debug.Assert(m_isTargetPositionSet);

		horizontalOffset = m_targetPoint.X;
		verticalOffset = m_targetPoint.Y;

		var placementMode = GetEffectivePlacement();

		// We want to preserve existing MenuFlyout behavior - it will continue to ignore the Placement property.
		// We also don't want to adjust anything if we've been positioned for a DatePicker or TimePicker -
		// in those cases, we've already been put at exactly the position we want to be at.
		if (!isMenuFlyout && !m_isPositionedForDateTimePicker)
		{
			switch (placementMode)
			{
				case FlyoutPlacementMode.Top:
					horizontalOffset -= presenterSize.Width / 2;
					verticalOffset -= presenterSize.Height;
					break;
				case FlyoutPlacementMode.Bottom:
					horizontalOffset -= presenterSize.Width / 2;
					break;
				case FlyoutPlacementMode.Left:
					horizontalOffset -= presenterSize.Width;
					verticalOffset -= presenterSize.Height / 2;
					break;
				case FlyoutPlacementMode.Right:
					verticalOffset -= presenterSize.Height / 2;
					break;
				case FlyoutPlacementMode.TopEdgeAlignedLeft:
				case FlyoutPlacementMode.RightEdgeAlignedBottom:
					verticalOffset -= presenterSize.Height;
					break;
				case FlyoutPlacementMode.TopEdgeAlignedRight:
				case FlyoutPlacementMode.LeftEdgeAlignedBottom:
					horizontalOffset -= presenterSize.Width;
					verticalOffset -= presenterSize.Height;
					break;
				case FlyoutPlacementMode.BottomEdgeAlignedLeft:
				case FlyoutPlacementMode.RightEdgeAlignedTop:
					// Nothing changes in this case - we want the point to be the top-left corner of the flyout,
					// which it already is.
					break;
				case FlyoutPlacementMode.BottomEdgeAlignedRight:
				case FlyoutPlacementMode.LeftEdgeAlignedTop:
					horizontalOffset -= presenterSize.Width;
					break;
			}
		}

		preferTopPlacement = (m_inputDeviceTypeUsedToOpen == InputDeviceType.Touch) && isMenuFlyout;
		bool useHandednessPlacement = (m_inputDeviceTypeUsedToOpen == InputDeviceType.Pen) && isMenuFlyout;

		if (preferTopPlacement)
		{
			verticalOffset -= presenterSize.Height;
		}

		var majorPlacementMode = preferTopPlacement
			? MajorPlacementMode.Top
			: GetMajorPlacementFromPlacement(placementMode);

		flowDirection = _popup.FlowDirection;
		if (m_isPositionedAtPoint)
		{
			targetFlowDirection = Target.FlowDirection;
			Debug.Assert(flowDirection == targetFlowDirection);
		}

		GetEffectivePlacementMode(
			majorPlacementMode,
			flowDirection,
			out majorPlacementMode);

		var originalMajorPlacementMode = majorPlacementMode;

		// If the desired placement of the flyout is inside the exclusion area, we'll shift it in the direction of the placement direction
		// so that it no longer is inside that area.
		void accountForExclusionRect()
		{
			if (!AreDisjoint(m_exclusionRect, new Rect(horizontalOffset, verticalOffset, presenterSize.Width, presenterSize.Height)))
			{
				switch (majorPlacementMode)
				{
					case MajorPlacementMode.Top:
						verticalOffset = m_exclusionRect.Y - presenterSize.Height;
						break;
					case MajorPlacementMode.Bottom:
						verticalOffset = m_exclusionRect.Y + m_exclusionRect.Height;
						break;
					case MajorPlacementMode.Left:
						horizontalOffset = m_exclusionRect.X - presenterSize.Width;
						break;
					case MajorPlacementMode.Right:
						horizontalOffset = m_exclusionRect.X + m_exclusionRect.Width;
						break;
				}

				// (0, 0) is at the right side in RTL rather than the left side,
				// so we'll add on the presenter width in that case to the offset.
				if ((majorPlacementMode == MajorPlacementMode.Left ||
					majorPlacementMode == MajorPlacementMode.Right) &&
					flowDirection == FlowDirection.RightToLeft)
				{
					horizontalOffset += presenterSize.Width;
				}
			}
		}

		accountForExclusionRect();

		bool isRTL = flowDirection == FlowDirection.RightToLeft;
		bool shiftLeftForRightHandedness = useHandednessPlacement && (IsRightHandedHandedness() != isRTL);
		if (shiftLeftForRightHandedness)
		{
			if (!isRTL)
			{
				horizontalOffset -= presenterSize.Width;
			}
			else
			{
				horizontalOffset += presenterSize.Width;
			}
		}

		// Get the current presenter max width/height
		maxWidth = m_tpPresenter.MaxWidth;
		maxHeight = m_tpPresenter.MaxHeight;

		var targetPoint = new Point(horizontalOffset, verticalOffset);
		Rect availableRect = default;

		bool useMonitorBounds = IsWindowedPopup();

		if (useMonitorBounds)
		{
			// Calculate the available monitor bounds to set the target position within the monitor bounds
			availableRect = Popup.CalculateAvailableMonitorRect(_popup.XamlRoot ?? XamlRoot, targetPoint);
		}
		else
		{
			availableRect = availableWindowRect;
		}

		// Set the max width and height with the available bounds
		m_tpPresenter.MaxWidth = double.IsNaN(maxWidth) ? availableRect.Width : Math.Min(maxWidth, availableRect.Width);
		m_tpPresenter.MaxHeight = double.IsNaN(maxHeight) ? availableRect.Height : Math.Min(maxHeight, availableRect.Height);

		// Adjust the target position if the current target is out of bounds
		if (flowDirection == FlowDirection.LeftToRight)
		{
			if (targetPoint.X + presenterSize.Width > (availableRect.X + availableRect.Width))
			{
				if (useMonitorBounds)
				{
					// Update the target horizontal position if the target is out of the available bounds.
					// If the presenter width is greater than the current target left point from the screen,
					// the menu target left position is set to the begin of the screen position.
					horizontalOffset -= Math.Min(
						presenterSize.Width,
						Math.Max(0, targetPoint.X - availableRect.X));
				}
				else
				{
					if (m_isPositionedAtPoint)
					{
						// Update the target horizontal position if the target is out of the available rect
						horizontalOffset -= Math.Min(presenterSize.Width, horizontalOffset);
					}
					else
					{
						// Used for date and time picker flyouts
						horizontalOffset = availableRect.X + availableRect.Width - presenterSize.Width;
						horizontalOffset = Math.Max(availableRect.X, horizontalOffset);
					}
				}

				// If we were positioned on the right, we're now positioned on the left.
				if (majorPlacementMode == MajorPlacementMode.Right)
				{
					majorPlacementMode = MajorPlacementMode.Left;
				}
			}
		}
		else
		{
			if (targetPoint.X - availableRect.X < presenterSize.Width)
			{
				if (useMonitorBounds)
				{
					// Update the target horizontal position if the target is outside the available monitor
					// if the presenter width is greater than the current target right point from the screen,
					// the menu target left position is set to the end of the screen position.
					horizontalOffset += Math.Min(
						presenterSize.Width,
						Math.Max(0, availableRect.Width - targetPoint.X + availableRect.X));
				}
				else
				{
					// Adjust the target position if the current target is out of the Xaml window bounds
					if (m_isPositionedAtPoint)
					{
						// Update the target horizontal position if the target is out of the available rect
						horizontalOffset += Math.Min(presenterSize.Width, availableRect.Width + availableRect.X - horizontalOffset);
					}
					else
					{
						// Used for date and time picker flyouts
						horizontalOffset = presenterSize.Width + availableRect.X;
						horizontalOffset = Math.Min(availableRect.Width + availableRect.X, horizontalOffset);
					}
				}

				// If we were positioned on the left, we're now positioned on the right.
				if (majorPlacementMode == MajorPlacementMode.Left)
				{
					majorPlacementMode = MajorPlacementMode.Right;
				}
			}
		}

		// If we couldn't actually fit to the left, flip back to show right.
		if (shiftLeftForRightHandedness)
		{
			if (!isRTL && targetPoint.X < availableRect.X)
			{
				horizontalOffset += presenterSize.Width;
				targetPoint.X += presenterSize.Width;

				// If we were positioned on the left, we're now positioned on the right.
				if (majorPlacementMode == MajorPlacementMode.Left)
				{
					majorPlacementMode = MajorPlacementMode.Right;
				}
			}
			else if (isRTL && targetPoint.X + presenterSize.Width >= availableRect.Width)
			{
				horizontalOffset -= presenterSize.Width;
				targetPoint.X -= presenterSize.Width;

				// If we were positioned on the right, we're now positioned on the left.
				if (majorPlacementMode == MajorPlacementMode.Right)
				{
					majorPlacementMode = MajorPlacementMode.Left;
				}
			}
		}

		// If opening up would cause the flyout to get clipped, we fall back to opening down
		if (preferTopPlacement && targetPoint.Y < availableRect.Y)
		{
			verticalOffset += presenterSize.Height;
			targetPoint.Y += presenterSize.Height;

			// If we were positioned on the top, we're now positioned on the bottom.
			if (majorPlacementMode == MajorPlacementMode.Top)
			{
				majorPlacementMode = MajorPlacementMode.Bottom;
			}
		}

		if (targetPoint.Y + presenterSize.Height > availableRect.Y + availableRect.Height)
		{
			// WINAPPSDK_CHANGEID_62771256 (the alignment for side placements) is enabled at this tag.
			// For side placements (Left/Right) with an exclusion rect,
			// verticalOffset is the anchor's top edge, not its bottom.
			// Use exclusionRect.Bottom so the flipped flyout aligns to
			// the anchor's bottom edge.
			double bottomAlignmentY = verticalOffset;
			bool sidePlacement =
				majorPlacementMode == MajorPlacementMode.Left ||
				majorPlacementMode == MajorPlacementMode.Right;
			if (sidePlacement && !GetIsEmpty(m_exclusionRect) &&
				Math.Abs(verticalOffset - m_exclusionRect.Y) <= 1.0)
			{
				bottomAlignmentY = m_exclusionRect.Y + m_exclusionRect.Height;
			}

			if (useMonitorBounds)
			{
				// Update the target vertical position if the target is out of the available monitor.
				// If the presenter height is greater than the current target top point from the screen,
				// the menu target top position is set to the begin of the screen position.
				if (verticalOffset > availableRect.Y)
				{
					verticalOffset = bottomAlignmentY - Math.Min(
						presenterSize.Height,
						Math.Max(0, bottomAlignmentY - availableRect.Y));
				}
				else // if it spans two monitors, make it start at the second.
				{
					verticalOffset = availableRect.Y;
				}
			}
			else
			{
				// Update the target vertical position if the target is out of the available rect
				if (m_isPositionedAtPoint)
				{
					// Same bottom-alignment correction as the windowed branch above.
					// The non-windowed branch implicitly treats availableRect.Y as 0,
					// so the clamp's second argument simplifies to bottomAlignmentY.
					verticalOffset = bottomAlignmentY - Math.Min(presenterSize.Height, bottomAlignmentY);
				}
				else
				{
					verticalOffset = availableRect.Y + availableRect.Height - presenterSize.Height;
				}
			}

			// If we were positioned on the bottom, we're now positioned on the top.
			if (majorPlacementMode == MajorPlacementMode.Bottom)
			{
				majorPlacementMode = MajorPlacementMode.Top;
			}
		}

		if (!useMonitorBounds)
		{
			verticalOffset = Math.Max(availableRect.Y, verticalOffset);
		}

		// The above accounting may have shifted our position, so we'll account for the exclusion rect again.
		accountForExclusionRect();

		// Accounting for the exclusion rect is important, but keeping the popup fully in view is more important, if possible.
		// If we are still not completely in view at this point, we'll fall back to our original positioning and then
		// nudge the popup into full view, as long as it will fit on screen.
		bool presenterIsOffScreenHorizontally = isRTL ?
			(horizontalOffset - presenterSize.Width < availableRect.X ||
				horizontalOffset > availableRect.X + availableRect.Width) :
			(horizontalOffset < availableRect.X ||
				horizontalOffset + presenterSize.Width > availableRect.X + availableRect.Width);

		bool presenterIsOffScreenVertically =
			verticalOffset < availableRect.Y ||
			verticalOffset + presenterSize.Height > availableRect.Y + availableRect.Height;

		// First we'll put the positioning back and re-account for the exclusion rect.
		if ((presenterSize.Width <= availableRect.Width && presenterIsOffScreenHorizontally) ||
			(presenterSize.Height <= availableRect.Height && presenterIsOffScreenVertically))
		{
			majorPlacementMode = originalMajorPlacementMode;
			accountForExclusionRect();
		}

		/// Now we'll nudge the popup into view.
		if (presenterSize.Width <= availableRect.Width)
		{
			// In RTL, the horizontal offset represents the top-right corner instead of the top-left corner,
			// so we need to adjust our calculations accordingly.
			if (isRTL)
			{
				if (horizontalOffset - presenterSize.Width < availableRect.X)
				{
					horizontalOffset = availableRect.X + presenterSize.Width;
				}
				else if (horizontalOffset > availableRect.X + availableRect.Width)
				{
					horizontalOffset = availableRect.X + availableRect.Width;
				}
			}
			else
			{
				if (horizontalOffset < availableRect.X)
				{
					horizontalOffset = availableRect.X;
				}
				else if (horizontalOffset + presenterSize.Width > availableRect.X + availableRect.Width)
				{
					horizontalOffset = availableRect.X + availableRect.Width - presenterSize.Width;
				}
			}
		}

		if (presenterSize.Height <= availableRect.Height)
		{
			if (verticalOffset < availableRect.Y)
			{
				verticalOffset = availableRect.Y;
			}
			else if (verticalOffset + presenterSize.Height > availableRect.Y + availableRect.Height)
			{
				verticalOffset = availableRect.Y + availableRect.Height - presenterSize.Height;
			}
		}

		_popup.HorizontalOffset = horizontalOffset;
		_popup.VerticalOffset = verticalOffset;

		double leftMostEdge = (flowDirection == FlowDirection.LeftToRight) ? horizontalOffset : horizontalOffset - presenterSize.Width;

		return new Rect(leftMostEdge, verticalOffset, presenterSize.Width, presenterSize.Height);
	}

	// TODO Uno: DXamlCore::GetFlyoutMetadata, Uno has no DXamlCore state per thread and only one UI thread.
	private static readonly FlyoutMetadata s_flyoutMetadata = new();

	// Return the appropriate metadata depending on whether this flyout is a child
	// flyout or not.  For child flyouts, it returns the metadata object for its
	// parent.  For root flyouts, it returns the global metadata from DXamlCore.
	private FlyoutMetadata GetFlyoutMetadata() => m_tpParentFlyout is not null ? m_tpParentFlyout.m_childFlyoutMetadata : s_flyoutMetadata;

	private static bool GetIsAncestorOfTraverseThroughPopups(UIElement pParentElem, UIElement pMaybeDescendant)
	{
		if (pParentElem is null)
		{
			return false;
		}

		while (pMaybeDescendant is not null && !ReferenceEquals(pMaybeDescendant, pParentElem))
		{
			pMaybeDescendant = pMaybeDescendant.GetUIElementAdjustedParentInternal(false);
		}

		return ReferenceEquals(pParentElem, pMaybeDescendant);
	}

	private static FlyoutBase FindParentFlyoutFromElement(FrameworkElement element)
	{
		// If the element is a CommandBar element, then we'll find the parent CommandBar first -
		// this element might be in the CommandBar's popup and not be a visual child of the flyout
		// that the CommandBar is a child of.
		var elementLocal = element;

		if (elementLocal is ICommandBarElement elementAsCommandBarElement)
		{
			var parentCommandBar = CommandBar.FindParentCommandBarForElement(elementAsCommandBarElement);

			if (parentCommandBar is not null)
			{
				elementLocal = parentCommandBar;
			}
		}

		var visualTree = VisualTree.GetForElement(elementLocal);

		var popups = Media.VisualTreeHelper.GetOpenPopups(visualTree);

		// Iterate over the open popups, and for the ones that are associated with a flyout
		// check to see if the given element is a descendent of that flyout's presenter.
		foreach (var popup in popups)
		{
			var flyout = popup.AssociatedFlyout;
			if (flyout is not null)
			{
				// If the flyout is open, it should have a presenter.
				Debug.Assert(flyout.m_tpPresenter is not null);

				var isAncestorOf = GetIsAncestorOfTraverseThroughPopups(popup, elementLocal);

				if (isAncestorOf || ReferenceEquals(popup, elementLocal))
				{
					return flyout;
				}
			}
		}

		return null;
	}

	// Callback for ShowAt() from core layer
	internal static void ShowAtStatic(FlyoutBase pCoreFlyoutBase, FrameworkElement pCoreTarget)
	{
		Debug.Assert(pCoreFlyoutBase is not null);
		Debug.Assert(pCoreTarget is not null);

		pCoreFlyoutBase.ShowAt(pCoreTarget);
	}

	// Callback for ShowAt() from core layer
	internal static void ShowAtStatic(
		FlyoutBase pFlyoutBase,
		FrameworkElement pTarget,
		Point point,
		Rect exclusionRect,
		FlyoutShowMode flyoutShowMode)
	{
		Debug.Assert(pFlyoutBase is not null);
		Debug.Assert(pTarget is not null);

		FlyoutShowOptions showOptions = new();

		showOptions.Position = point;
		showOptions.ExclusionRect = exclusionRect;
		showOptions.ShowMode = flyoutShowMode;

		pFlyoutBase.ShowAt(pTarget, showOptions);
	}

	internal static void OnClosingStatic(FlyoutBase @object, ref bool cancel) => @object.OnClosing(ref cancel);

	private void ConfigurePopupOverlay()
	{
		Debug.Assert(_popup is not null);

		bool isOverlayVisible = LightDismissOverlayHelper.IsOverlayVisibleForMode(LightDismissOverlayMode);

		bool isLightDismissEnabled = _popup.IsLightDismissEnabled;

		// Some modes of flyout configure their popup as not light-dismissible;
		// for those cases don't allow the overlay to be visible.
		isOverlayVisible &= isLightDismissEnabled;

		if (isOverlayVisible)
		{
			_popup.LightDismissOverlayMode = LightDismissOverlayMode.On;

			// Configure the overlay with the correct theme brush.
			string themeBrush;
			if (this is MenuFlyout)
			{
				themeBrush = "MenuFlyoutLightDismissOverlayBackground";
			}
			else if (this is DatePickerFlyout)
			{
				themeBrush = "DatePickerLightDismissOverlayBackground";
			}
			else if (this is TimePickerFlyout)
			{
				themeBrush = "TimePickerLightDismissOverlayBackground";
			}
			else
			{
				// CalendarDatePicker doesn't have it's own flyout type, so we check for whether
				// the placement target has a templated parent that is a CalendarDatePicker.
				if (Target?.GetTemplatedParent() is CalendarDatePicker)
				{
					themeBrush = "CalendarDatePickerLightDismissOverlayBackground";
				}
				else
				{
					themeBrush = "FlyoutLightDismissOverlayBackground";
				}
			}

			// Hook up our brush to the popup's overlay using theme resources.
			_popup.SetOverlayThemeBrush(themeBrush);
		}
		else
		{
			_popup.LightDismissOverlayMode = LightDismissOverlayMode.Off;
		}
	}

	/// <summary>
	/// Attempts to invoke a keyboard shortcut (accelerator).
	/// </summary>
	/// <param name="args">The ProcessKeyboardAcceleratorEventArgs.</param>
	public void TryInvokeKeyboardAccelerator(ProcessKeyboardAcceleratorEventArgs args) => OnProcessKeyboardAccelerators(args);

	/// <summary>
	/// Called just before a keyboard shortcut (accelerator) is processed in your app. Invoked whenever application code or internal
	/// processes call ProcessKeyboardAccelerators. Override this method to influence the default accelerator handling.
	/// </summary>
	/// <param name="args">The ProcessKeyboardAcceleratorEventArgs.</param>
	protected virtual void OnProcessKeyboardAccelerators(ProcessKeyboardAcceleratorEventArgs args)
	{
	}

	private Size GetPresenterSize() => new Size(m_tpPresenter.ActualWidth, m_tpPresenter.ActualHeight);

	private void UpdateStateToShowMode(FlyoutShowMode showMode)
	{
		if (showMode == FlyoutShowMode.Auto)
		{
			showMode = FlyoutShowMode.Standard;
		}

		ShowMode = showMode;

		bool oldShouldHideIfPointerMovesAway = m_shouldHideIfPointerMovesAway;

		switch (showMode)
		{
			case FlyoutShowMode.Standard:
				m_shouldTakeFocus = true;
				m_shouldHideIfPointerMovesAway = false;
				m_shouldOverlayPassThroughAllInput = false;
				break;
			case FlyoutShowMode.Transient:
				m_shouldTakeFocus = false;
				m_shouldHideIfPointerMovesAway = false;
				m_shouldOverlayPassThroughAllInput = true;
				break;
			case FlyoutShowMode.TransientWithDismissOnPointerMoveAway:
				m_shouldTakeFocus = false;
				m_shouldHideIfPointerMovesAway = true;
				m_shouldOverlayPassThroughAllInput = true;
				break;
			default:
				Debug.Fail("Unsupported FlyoutShowMode");
				break;
		}

		if (_popup is not null)
		{
			bool isOpen = _popup.IsOpen;
			if (isOpen)
			{
				SetPopupLightDismissBehavior();
				ConfigurePopupOverlay();

				if (m_shouldHideIfPointerMovesAway && !oldShouldHideIfPointerMovesAway)
				{
					AddRootVisualPointerMovedHandler();
				}
				else if (!m_shouldHideIfPointerMovesAway && oldShouldHideIfPointerMovesAway)
				{
					RemoveRootVisualPointerMovedHandler();
				}
			}
		}
	}

	private FlyoutPlacementMode GetEffectivePlacement() => m_hasPlacementOverride ? m_placementOverride : Placement;

	// Checks whether the provided popup is for a ComboBox dropdown which is identified by a grandchild of type CComboBoxLightDismiss.
	// The recognized ComboBox tree structure is:
	//     CPopup
	//       CPanel (a CCanvas in practice)
	//         CComboBoxLightDismiss
	//         second child (a CBorder in practice)
	private static bool HasComboBoxLightDismiss(Popup popup)
	{
		if (popup.Child is not Panel panel)
		{
			return false;
		}

		var panelChildren = panel.Children;

		if (panelChildren.Count < 2)
		{
			return false;
		}

		return panelChildren[0] is ComboBoxLightDismiss;
	}

	private protected bool IsLightDismissOverlayEnabled
	{
		get => m_isLightDismissOverlayEnabled;
		set => m_isLightDismissOverlayEnabled = value;
	}

	/// <summary>
	/// Whether touch was the input device that opened this flyout. Set before <see cref="Opening"/> fires.
	/// </summary>
	private protected bool WasOpenedByTouch => m_inputDeviceTypeUsedToOpen == InputDeviceType.Touch;

	// TODO Uno: Native pickers (Android) have no popup, nor presenter, whose lifetime would close them.
	private protected void SetOpenNativeFlyout() => GetFlyoutMetadata().SetOpenFlyout(this, Target);

	// The chain of open flyouts from the root down, for tests.
	internal static IReadOnlyList<FlyoutBase> OpenFlyouts
	{
		get
		{
			List<FlyoutBase> openFlyouts = new();

			for (var metadata = s_flyoutMetadata; metadata is not null;)
			{
				metadata.GetOpenFlyout(out var openFlyout, out _);
				if (openFlyout is null)
				{
					break;
				}

				openFlyouts.Add(openFlyout);
				metadata = openFlyout.m_childFlyoutMetadata;
			}

			return openFlyouts;
		}
	}

	internal virtual Control GetPresenter() => _popup?.Child as Control;
}
