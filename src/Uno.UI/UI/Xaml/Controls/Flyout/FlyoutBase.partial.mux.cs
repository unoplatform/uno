// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference FlyoutBase_partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

using System;
using System.Diagnostics;
using System.Linq;
using Uno.Disposables;
using Uno.UI;
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

		if (args.Property == ShouldConstrainToRootBoundsProperty && _popup is { } popup)
		{
			popup.ShouldConstrainToRootBounds = (bool)args.NewValue;
		}
		else if (args.Property == LightDismissOverlayModeProperty && _popup is not null)
		{
			ReevaluateIsOverlayVisible();
		}
	}

	private void SetPopupLightDismissBehavior()
	{
		// TODO Uno: put_IsLightDismissEnabled(m_isLightDismissOverlayEnabled) is applied by IsLightDismissOverlayEnabled.
		if (m_shouldOverlayPassThroughAllInput)
		{
			if (OverlayInputPassThroughElement is null && XamlRoot?.VisualTree.PublicRootVisual is { } rootVisual)
			{
				OverlayInputPassThroughElement = rootVisual;
				m_ownsOverlayInputPassThroughElement = true;
			}
		}
		else if (m_ownsOverlayInputPassThroughElement)
		{
			OverlayInputPassThroughElement = null;
			m_ownsOverlayInputPassThroughElement = false;
		}
	}

	private void ReevaluateIsOverlayVisible()
	{
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

	internal static FlyoutBase CloseOpenFlyout(FlyoutBase parentFlyoutCore)
	{
		// TODO Uno: FlyoutMetadata. The open flyouts are a flat list, in which a child comes after its parent.
		var openFlyout = parentFlyoutCore is null
			? _openFlyouts.FirstOrDefault()
			: _openFlyouts.SkipWhile(f => f != parentFlyoutCore).Skip(1).FirstOrDefault();

		openFlyout?.Hide();

		return openFlyout;
	}

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
					contentRect = CalculateAvailableMonitorRect(_popup, positionValue);
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

#if HAS_UNO
		// TODO Uno: Stand-in for the flyout metadata and CheckAndHandleOpenFlyout, which stage a flyout until
		// the open one has closed (FlyoutBase_partial.cpp lines 1696-1757).
		EnsurePopupAndPresenter();

		if (_isClosedPending)
		{
			return;
		}

		if (IsOpen)
		{
			if (pPlacementTarget == Target)
			{
				return;
			}

			// Close at previous placement target before opening at new one (without raising Closing)
			bool wasTargetPositionSet = m_isTargetPositionSet;
			bool hadPlacementOverride = m_hasPlacementOverride;
			Hide(canCancel: false);
			m_isTargetPositionSet = wasTargetPositionSet;
			m_hasPlacementOverride = hadPlacementOverride;
		}
#endif

		ValidateAndSetParameters(pPlacementTarget, out bool shouldOpen);

		// If we shouldn't open, we'll early out here.
		if (!shouldOpen)
		{
			return;
		}

		EnsureAssociatedXamlRoot(pPlacementTarget);

#if HAS_UNO
		XamlRoot = pPlacementTarget.XamlRoot;
		_popup.XamlRoot = XamlRoot;
#endif

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

#if HAS_UNO
		if (!_popup.IsOpen)
		{
			return;
		}

		IsOpen = true;

		// UNO-FIX: Defer the raising of the Opened event to ensure everything is well initialized before opening it.
		_ = Dispatcher.RunAsync(CoreDispatcherPriority.Idle, () =>
		{
			if (IsOpen)
			{
				OnOpened();
				Opened?.Invoke(this, EventArgs.Empty);
			}
		});
#endif
	}

	// This will be called from either ShowAtImpl if there were no staged Flyouts or from OnPresenterUnloaded when
	// previous FlyoutBase was closed.  Parameters should be validated and cached prior to calling this method.
	internal virtual void Open()
	{
		// Placement target should be validated and set.
		Debug.Assert(Target is not null);

		m_isPositionedForDateTimePicker = false;

		EnsurePopupAndPresenter();

		// Set how the popup will be dismissed. This needs to be done each time the flyout is
		// opened because the placement mode may change. In theory we should wait until the
		// placement logic has been executed to get the final placement, but the light dismiss
		// value must be set before the popup is opened to have an effect.
		_popup.IsLightDismissEnabled = _isLightDismissEnabled;

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

		_popup.SetShouldTakeFocus(m_shouldTakeFocus);
		SetPopupLightDismissBehavior();
		_popup.Opacity = 1.0;

		var placementMode = GetEffectivePlacement();
		m_majorPlacementMode = GetMajorPlacementFromPlacement(placementMode);
		m_preferredJustification = GetJustificationFromPlacementMode(placementMode);

#if HAS_UNO
		if (XamlRoot is not null && _popup.XamlRoot is null)
		{
			_popup.XamlRoot = XamlRoot;
		}
#endif

		_popup.IsOpen = true;

#if HAS_UNO
		AddToOpenFlyouts();
#endif
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

			// TODO Uno: NOT PORTED - PreparePopupTheme and the theme transitions (FlyoutBase_partial.cpp lines 1950-2068).
		}

		// TODO Uno: NOT PORTED - CFlyoutBase::OnPlacementUpdated(effectivePlacementMode), its subscribers are not ported.
	}

	private void PreparePresenter(Control pPresenter)
	{
		Debug.Assert(pPresenter is not null);

		pPresenter.SizeChanged += OnPresenterSizeChanged;
		pPresenter.Loaded += OnPresenterLoaded;

		// TODO Uno: NOT PORTED - OnPresenterUnloaded (FlyoutBase_partial.cpp lines 2299-2352).
	}

	private void OnPresenterSizeChanged(object pSender, SizeChangedEventArgs pArgs)
	{
		// Perform placement when size of presenter is known.
		var presenterSize = pArgs.NewSize;

		if (!m_presenterResized && _popup?.IsOpen == true)
		{
			PerformPlacement(presenterSize);
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

	// TODO Uno: windowed - DXamlCore::CalculateAvailableMonitorRect is not ported, so this is only the XAML root's visible bounds.
	private static Rect CalculateAvailableMonitorRect(Popup popup, Point targetPosition) => GetVisibleBounds(popup, null);

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

			pAvailableRect = CalculateAvailableMonitorRect(popup, popupPosition);
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

	private void OnPlacementTargetUnloaded(object sender, RoutedEventArgs args) => Hide();

	private void OnPlacementTargetActualThemeChanged(FrameworkElement sender, object args) => ForwardThemeToPresenter();

	internal void PlaceFlyoutForDateTimePicker(Point point)
	{
		m_isPositionedForDateTimePicker = true;

		ForwardPopupFlowDirection();
		SetTargetPosition(point);
		ApplyTargetPosition();
		var presenterSize = GetPresenterSize();
		PerformPlacement(presenterSize);

		// TODO Uno: NOT PORTED - PickerFlyoutThemeTransition OpenedLength and OffsetFromCenter (FlyoutBase_partial.cpp lines 3056-3081).
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
					// TODO Uno: windowed - CPopup::SetIsWindowed is not ported.
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
			availableRect = CalculateAvailableMonitorRect(_popup, targetPoint);
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

	private Size GetPresenterSize() => new Size(m_tpPresenter.ActualWidth, m_tpPresenter.ActualHeight);

	private FlyoutPlacementMode GetEffectivePlacement() => m_hasPlacementOverride ? m_placementOverride : Placement;
}
