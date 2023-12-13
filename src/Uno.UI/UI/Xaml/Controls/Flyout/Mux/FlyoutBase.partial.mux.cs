// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\FlyoutBase_partial.cpp, tag winui3/release/1.4.3, commit 685d2bf

//  Abstract:
//      FlyoutBase - base class for Flyout provides the following functionality:
//        * Showing/hiding.
//        * Enforcing requirement that only one FlyoutBase is open at the time.
//        * Placement logic.
//        * Raising events.
//        * Entrance/exit transitions.
//        * Expose Attached property as storage from Flyouts in XAML.

#nullable enable

using System;
using System.Runtime.CompilerServices;
using DirectUI;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml.Controls.Primitives;

public partial class FlyoutBase : DependencyObject
{
	private const FlyoutPlacementMode g_defaultPlacementMode = FlyoutPlacementMode.Top;

	// Margin between FlyoutBase and window's edge, AppBar and placement target.
	private const double FlyoutMargin = 4;

	// Offsets for PopupThemeTransition
	private const double g_entranceThemeOffset = 50.0;

	private const string c_visualStateLandscape = "Landscape";
	private const string c_visualStatePortrait = "Portrait";

	//------------------------------------------------------------------------
	// Helpers
	//------------------------------------------------------------------------

	private PreferredJustification GetJustificationFromPlacementMode(FlyoutPlacementMode placement)
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
				MUX_ASSERT(false, "Unsupported FlyoutPlacementMode");
				break;
		}

		return result;
	}

	private MajorPlacementMode GetMajorPlacementFromPlacement(FlyoutPlacementMode placement)
	{
		MajorPlacementMode result = MajorPlacementMode.Full;

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
				MUX_ASSERT(false, "Unsupported FlyoutPlacementMode");
				break;
		}

		return result;
	}

	private FlyoutPlacementMode GetPlacementFromMajorPlacement(MajorPlacementMode majorPlacement)
	{
		FlyoutPlacementMode result = FlyoutPlacementMode.Full;

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
				MUX_ASSERT(false, "Unsupported FlyoutPlacementMode");
				break;
		}

		return result;
	}

	// Return true if placement is along vertical axis, false otherwise.
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsPlacementModeVertical(MajorPlacementMode placementMode)
	{
		// We are safe even if placementMode is Full. because the case for placementMode is Full has already been put in another if branch in function PerformPlacement.
		// if necessary, we can add another function : IsPlacementModeHorizontal
		return (placementMode == MajorPlacementMode.Top ||
				placementMode == MajorPlacementMode.Bottom);
	}

	// Helper for finding parent of element whose type is IAppBar.
	private static AppBar? FindParentAppBar(FrameworkElement? pElement)
	{
		AppBar? parentAppBar = null;
		var spCurrent = pElement;

		while (spCurrent != null)
		{
			DependencyObject spNextAsDO;
			AppBar? spAppBar = spCurrent as AppBar;

			if (spAppBar != null)
			{
				parentAppBar = spAppBar;
				break;
			}

			spNextAsDO = spCurrent.Parent;
			spCurrent = spNextAsDO as FrameworkElement;
		}

		return parentAppBar;
	}

	// Helper which depending on FlowDirection will either directly copy placementMode to adjustedPlacementMode (LTR)
	// or will reverse Left/Right if order is RTL.
	private static MajorPlacementMode GetEffectivePlacementMode(
		MajorPlacementMode placementMode,
		FlowDirection flowDirection)
	{
		var pAdjustedPlacementMode = placementMode;

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

		return pAdjustedPlacementMode;
	}

	// Fails if placementMode is not a valid value.
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
				throw new InvalidOperationException("Invalid flyout placement mode");
		}
	}

	// Helper for getting minimum size of FrameworkElement.
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Size GetMinimumSize(FrameworkElement frameworkElement)
	{
		var minWidth = frameworkElement.MinWidth;
		var minHeight = frameworkElement.MinHeight;
		return new Size(minWidth, minHeight);
	}

	// Helper for getting maximum size of FrameworkElement.
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Size GetMaximumSize(FrameworkElement frameworkElement)
	{
		var maxWidth = frameworkElement.MaxWidth;
		var maxHeight = frameworkElement.MaxHeight;
		return new Size(maxWidth, maxHeight);
	}

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

		MUX_ASSERT(anchorSize >= 0.0);
		MUX_ASSERT(controlSize >= 0.0);

		if ((highLimit - lowLimit) > controlSize &&
			anchorSize >= 0.0 &&
			controlSize >= 0.0)
		{
			pControlPos = 0;

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
				MUX_ASSERT(false, "Unsupported PreferredJustification");
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

		MUX_ASSERT(anchorSize >= 0.0);
		MUX_ASSERT(controlSize >= 0.0);

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

		pControlPos = new(0.0, 0.0);

		MUX_ASSERT(placementMode != MajorPlacementMode.Full, "PlacementMode should not be Full when we come here.");

		if (IsPlacementModeVertical(placementMode))
		{
			if (TestAgainstLimitsAndPlace(
				placementTarget.Y,
				placementTarget.Height,
				(placementMode == MajorPlacementMode.Bottom) ? true : false,
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
				(placementMode == MajorPlacementMode.Right) ? true : false,
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

		pControlPos = new(controlXPos, controlYPos);

		return placementFits;
	}

	// Try to place a control according to fallback order.  If none of the fallbacks produces result which does not
	// violate placement rules, use the first one specified.
	// Return true if control can fit entirely without adjusting its size.
	private static bool PerformPlacementWithFallback(
		Rect placementTarget,
		Size controlSize,
		Rect containerRect,
		MajorPlacementMode[] orderedPlacements, // TODO:MZ: Removed PlacementCount param in favor of array.Length
		PreferredJustification justification,
		out Point pControlPos,
		out MajorPlacementMode pEffectiveMode)
	{
		var placementCount = orderedPlacements.Length;

		Point firstChoicePos = default;

		MUX_ASSERT(placementCount > 0);

		pControlPos = new(0.0, 0.0);
		pEffectiveMode = MajorPlacementMode.Top;

		// Try the initial placement and retain the coordinate regardless of the outcome.
		if (TryPlacement(placementTarget, controlSize, containerRect, orderedPlacements[0], justification, out firstChoicePos))
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
				availableSpace = DoubleUtil.Min(containerRect.Height, availableSpace);
				break;

			case MajorPlacementMode.Bottom:
				availableSpace = (containerRect.Y + containerRect.Height) - (placementTargetBounds.Y + placementTargetBounds.Height);
				availableSpace = DoubleUtil.Min(containerRect.Height, availableSpace);
				break;

			case MajorPlacementMode.Left:
				availableSpace = placementTargetBounds.X - containerRect.X;
				availableSpace = DoubleUtil.Min(containerRect.Width, availableSpace);
				break;

			case MajorPlacementMode.Right:
				availableSpace = (containerRect.X + containerRect.Width) - (placementTargetBounds.X + placementTargetBounds.Width);
				availableSpace = DoubleUtil.Min(containerRect.Width, availableSpace);
				break;

			default:
				MUX_ASSERT(false, "Unsupported FlyoutPlacementMode");
				break;
		}

		availableSpace = DoubleUtil.Max(0.0, availableSpace);

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

		MUX_ASSERT(pPlacement != MajorPlacementMode.Full, "PlacementMode should not be Full when we come here.");

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

		MUX_ASSERT(pPlacement != MajorPlacementMode.Full, "PlacementMode should not be Full when we come here.");

		// For primary axis, if exceeding available size on side specified by pPlacement
		// find side with more space and adjust position and size to be next to target.
		// For the secondary axis, if exceeding container size, set it to fill all available space.

		if (IsPlacementModeVertical(pPlacement))
		{
			if (pControlSize.Height > availableSpace)
			{
				MajorPlacementMode moreSpacePlacement = pPlacement;
				double moreAvailableSpace = 0.0;

				SelectSideWithMoreSpace(
					ref moreSpacePlacement,
					placementTargetBounds,
					containerRect,
					out moreAvailableSpace);

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
				MajorPlacementMode moreSpacePlacement = pPlacement;
				double moreAvailableSpace = 0.0;

				SelectSideWithMoreSpace(
					ref moreSpacePlacement,
					placementTargetBounds,
					containerRect,
					out moreAvailableSpace);

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

	public FlyoutBase()
	{
		m_allowPlacementFallbacks = false;
		m_cachedInputPaneHeight = 0.0;
		m_presenterResized = false;
		m_isTargetPositionSet = false;
		m_isPositionedAtPoint = false;
		m_usePickerFlyoutTheme = false;
		m_allowPresenterResizing = true;
		m_isFlyoutPresenterRequestedThemeOverridden = false;
		m_isLightDismissOverlayEnabled = true;
		//DBG_FLYOUT_TRACE(L">>> FlyoutBase::FlyoutBase()");

		m_majorPlacementMode = GetMajorPlacementFromPlacement(g_defaultPlacementMode);
	}

	~FlyoutBase() // TODO:MZ: Move to Unloaded most likely
	{
		//DBG_FLYOUT_TRACE(">>> FlyoutBase::~FlyoutBase()");

		m_epPresenterSizeChangedHandler.Disposable = null;
		m_epPresenterLoadedHandler.Disposable = null;
		m_epPresenterUnloadedHandler.Disposable = null;
		m_epPopupLostFocusHandler.Disposable = null;
		m_epPlacementTargetUnloadedHandler.Disposable = null;

		if (DXamlCore.Current is { } core)
		{
			core.SetTextControlFlyout(this, null);
		}
	}

	private protected override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == LightDismissOverlayModeProperty)
		{
			// The overlay is configured whenever we open the popup,
			// so if this property changes while it's open, then we
			// need to re-configure it.
			if (m_tpPopup is not null)
			{
				var isOpen = m_tpPopup.IsOpen;
				if (isOpen)
				{
					ConfigurePopupOverlay();
				}
			}
		}
		else if (args.Property == OverlayInputPassThroughElementProperty)
		{
			m_ownsOverlayInputPassThroughElement = false;
		}
		else if (args.Property == ShowModeProperty)
		{
			var showMode = ShowMode;
			UpdateStateToShowMode(showMode);
		}
		else if (args.Property == ShouldConstrainToRootBoundsProperty)
		{
			if (m_tpPopup is not null)
			{
				bool shouldConstrainToRootBounds = ShouldConstrainToRootBounds;
				m_tpPopup.ShouldConstrainToRootBounds = shouldConstrainToRootBounds;
			}
		}
	}

	/// <summary>
	/// When overridden in a derived class, initializes a control to show the flyout content as appropriate for the derived control.
	/// </summary>
	/// <remarks>This method has no base class implementation and must be overridden in a derived class.</remarks>
	/// <returns>The control that displays the content of the flyout.</returns>
	protected virtual Control CreatePresenter()
	{
		//TODO:MZ: Call to CreatePresenterProtected?
		return null;
	}

	// Associates this flyout with the correct XamlRoot for its context, or returns an error
	// if the context is ambiguous.
	private void EnsureAssociatedXamlRoot(DependencyObject? placementTarget)
	{
		DependencyObject placementTargetDO;
		if (placementTarget is not null)
		{
			placementTarget.QueryInterface(IID_PPV_ARGS(&placementTargetDO));
		}

		var visualTree = VisualTree.GetUniqueVisualTree(
			this,
			placementTargetDO == null ? null : placementTargetDO,
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
		//DBG_FLYOUT_TRACE(L">>> FlyoutBase::ShowAtImpl()");

		var placementTargetAsDO = placementTarget as DependencyObject;

		EnsureAssociatedXamlRoot(placementTargetAsDO);
		ShowAt(placementTargetAsDO, null);
	}

	/// <summary>
	/// Shows the flyout placed in relation to the specified element using the specified options.
	/// </summary>
	/// <param name="placementTarget">The element to use as the flyout's placement target.</param>
	/// <param name="showOptions">The options to use when showing the flyout.</param>
	public void ShowAt(
		DependencyObject? placementTarget,
		FlyoutShowOptions? showOptions)
	{
		//DBG_FLYOUT_TRACE(L">>> FlyoutBase::ShowAtWithOptionsImpl()");

		bool shouldConstrainToRootBounds = ShouldConstrainToRootBounds;

		using var scopeGuard = Disposable.Create(() =>
		{
			m_openingWindowedInProgress = false;
		});

		// MenuFlyouts are always windowed, whereas other flyouts are windowed if ShouldConstrainToRootBounds is false.
		m_openingWindowedInProgress = !shouldConstrainToRootBounds || this is MenuFlyout;

		Point? position = null;
		Rect? exclusionRect = null;
		FlyoutShowMode showMode = FlyoutShowMode.Auto;
		FlyoutPlacementMode placement = FlyoutPlacementMode.Auto;

		m_hasPlacementOverride = false;

		if (showOptions is not null)
		{
			position = showOptions.Position;
			exclusionRect = showOptions.ExclusionRect;
			showMode = showOptions.ShowMode;
			placement = showOptions.Placement;
		}

		var placementTargetUIElement = placementTarget as UIElement;
		if (placementTargetUIElement == null && position == null)
		{
			throw new ArgumentException("ShowOptions were empty.");
		}

		EnsureAssociatedXamlRoot(placementTarget);
		var xamlRoot = XamlRoot;

		// The call to EnsureAssociatedXamlRoot guarantees that if it succeeds we'll have a valid xamlRoot pointer.
		MUX_ASSERT(xamlRoot != null);

		GeneralTransform? transformToRoot = null;

		// If we're going to need to transform to root, let's create the transform now.
		if (placementTargetUIElement is not null && (position is not null || exclusionRect is not null))
		{
			transformToRoot = placementTargetUIElement.TransformToVisual(null);
		}

		if (position is not null)
		{
			m_isPositionedAtPoint = true;

			var positionValue = position.Value;

			if (transformToRoot is not null)
			{
				positionValue = transformToRoot.TransformPoint(positionValue);
			}

			EnsurePopupAndPresenter();

			Rect contentRect = new();

			if (IsWindowedPopup())
			{
				DXamlCore.Current.CalculateAvailableMonitorRect(m_tpPopup.Cast<Popup>(), positionValue, &contentRect));
			}
			else
			{
				var contentSize = xamlRoot.Size;

				contentRect.X = 0;
				contentRect.Y = 0;
				contentRect.Width = contentSize.Width;
				contentRect.Height = contentSize.Height;
			}

			if (DoubleUtil.IsNaN(positionValue.X) || DoubleUtil.IsNaN(positionValue.Y))
			{
				throw new ArgumentException("Position was invalid");
			}
			else
			{
				// Adjust the target position such that it is within the contentRect boundaries.
				positionValue.X = MathEx.Clamp(positionValue.X, contentRect.X, contentRect.X + contentRect.Width);
				positionValue.Y = MathEx.Clamp(positionValue.Y, contentRect.Y, contentRect.Y + contentRect.Height);

				// Set the target position to show the flyout
				SetTargetPosition(positionValue);
			}
		}
		else
		{
			m_isPositionedAtPoint = false;
		}

		if (exclusionRect is not null)
		{
			Rect exclusionRectValue = exclusionRect.Value;

			if (transformToRoot is not null)
			{
				exclusionRectValue = transformToRoot.TransformBounds(exclusionRectValue);
			}

			m_exclusionRect = exclusionRectValue;
		}
		else
		{
			m_exclusionRect = RectUtil.CreateEmptyRect();
		}

		if (showMode != FlyoutShowMode.Auto)
		{
			UpdateStateToShowMode(showMode);
		}
		else
		{
			UpdateStateToShowMode(FlyoutShowMode.Standard);
		}

		if (placement != FlyoutPlacementMode.Auto)
		{
			m_hasPlacementOverride = true;
			m_placementOverride = placement;
		}

		FrameworkElement? visualRootAsFE;

		// Show at the target element, if target is null use the frame as target
		if (placementTargetUIElement is not null)
		{
			visualRootAsFE = placementTargetUIElement;
		}
		else
		{
			UIElement rootElement = xamlRoot.Content;
			visualRootAsFE = rootElement;
		}

		bool openDelayed = false;
		HRESULT hr = ShowAtCore(visualRootAsFE.Get(), openDelayed);
		BOOLEAN isOpen = FALSE;

		if (SUCCEEDED(hr))
		{
			// Set popup flow direction must be called after ShowAt
			// to ensure the placement target in the flyout is initialized.
			hr = ForwardPopupFlowDirection();

			// Check if the popup successfully opened unless the opening was intentionally
			// delayed for instance when a submenu closes and another one opens.
			if (!openDelayed && SUCCEEDED(hr))
			{
				hr = get_IsOpenImpl(&isOpen);
			}
		}

		if (FAILED(hr) || (!openDelayed && !isOpen))
		{
			// Since the popup actually did not open, reset the m_hasPlacementOverride flag so that
			// it has the correct value at the next opening attempt.
			m_hasPlacementOverride = false;
		}
	}

	private void ShowAtCore(FrameworkElement pPlacementTarget, out bool openDelayed)
	{
		//DBG_FLYOUT_TRACE(L">>> FlyoutBase::ShowAtCore()");

		openDelayed = false;

		auto oldPlacementTarget = m_tpPlacementTarget.Get();

		bool shouldOpen = false;
		IFC_RETURN(ValidateAndSetParameters(pPlacementTarget, &shouldOpen));

		// If we shouldn't open, we'll early out here.
		if (!shouldOpen)
		{
			return S_OK;
		}

		EnsureAssociatedXamlRoot(pPlacementTarget);

		// Determine whether this flyout is opening as a child flyout.
		FlyoutBase parentFlyout = FindParentFlyoutFromElement(m_tpPlacementTarget);
		if (parentFlyout is not null)
		{
			// Allocate the metadata object for the parent's instance to track
			// the child flyouts.
			if (parentFlyout.m_childFlyoutMetadata is null)
			{
				parentFlyout.m_childFlyoutMetadata = new FlyoutMetadata();
			}

			// By setting the parent reference, we're marking ourselves as being a child flyout.
			m_tpParentFlyout = parentFlyout;
		}

		// Determine whether there is already an open flyout tracked by the appropriate
		// flyout metadata, and stage this instance until it closes rather than opening
		// immediately.
		var shouldOpen = CheckAndHandleOpenFlyout();

		if (shouldOpen)
		{
			Open();

			// If our placement target has changed, then we'll be reopening the flyout and placement will happen on presenter load.
			// Otherwise, we should perform placement now, as that step may not happen.
			if (oldPlacementTarget == m_tpPlacementTarget)
			{
				Size presenterSize = GetPresenterSize();

				// RS3: Adjust the placement based on the screen size. Skip the PreparePopupTheme call
				// to avoid setting m_tpThemeTransition for compatibility with RS2.
				PerformPlacement(presenterSize, 0.0, false /*preparePopupTheme*/));
			}
		} 
		else
		{
			openDelayed = true;
		}
	}

	/// <summary>
	/// Closes the flyout.
	/// </summary>
	public void Hide()
	{
		//DBG_FLYOUT_TRACE(L">>> FlyoutBase::HideImpl()");

		// Provide an opportunity for the app to cancel hiding the flyout.
		OnClosing(out var cancel);
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
			if (childFlyout is not null)
			{
				childFlyout.Hide();
			}
		}

		RemoveRootVisualPointerMovedHandler();

		if (m_wrRootVisual is not null)
		{
			WeakReferencePool.ReturnWeakReference(this, m_wrRootVisual);
			m_wrRootVisual = null;
		}

		if (m_tpPopup is not null)
		{
			// Since popup is light-dismiss all closing logic is in unloaded event handler.  Here, we only hide the popup.
			m_tpPopup.IsOpen = false;
		}
	}

	// This will be called from either ShowAtImpl if there were no staged Flyouts or from OnPresenterUnloaded when
	// previous FlyoutBase was closed.  Parameters should be validated and cached prior to calling this method.
	//TODO:MZ: Maybe public Open();
	private void Open()
	{
		DBG_FLYOUT_TRACE(L">>> FlyoutBase::Open()");

		// Placement target should be validated and set.
		MUX_ASSERT(m_tpPlacementTarget != null);

		if (m_tpPlacementTarget.GetPublicRootVisual() is { } publicRootVisualCore)
		{
			//TODO:MZ: Verify
			m_wrRootVisual = WeakReferencePool.RentWeakReference(this, publicRootVisual);
		}

		var flyoutMetadata = GetFlyoutMetadata();

		m_isPositionedForDateTimePicker = false;

#if FLYOUT_DEBUG
		{
			flyoutMetadata.GetOpenFlyout(out var openFlyout, out var openFlyoutPlacementTarget);

			MUX_ASSERT(openFlyout is null);
			MUX_ASSERT(openFlyoutPlacementTarget is null);
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

		FrameworkElement presenterAsFE = m_tpPresenter;
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
			presenterAsFE.Width = DoubleUtil.NaN;
			presenterAsFE.Height = DoubleUtil.NaN;
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

		CoreImports.Popup_SetShouldTakeFocus(m_tpPopup, m_shouldTakeFocus);
		m_tpPopup.Opacity = 1.0;

		FlyoutPlacementMode placementMode = GetEffectivePlacement();
		m_majorPlacementMode = GetMajorPlacementFromPlacement(placementMode);
		m_preferredJustification = GetJustificationFromPlacementMode(placementMode);

		flyoutMetadata.SetOpenFlyout(this, m_tpPlacementTarget);

		if (VisualTree* visualTree = GetHandle()->GetVisualTree())
		{
			// Currently we can't set Popup.XamlRoot here because that API doesn't allow the Popup to move between
			// two different XamlRoots/VisualTrees, and we want to support that here.  Instead, we use SetVisualTree
			// which doesn't enforce this.  This is safe as long as Popup isn't active.
			CPopup* corePopup = static_cast<CPopup*>(m_tpPopup.Cast<DirectUI::Popup>()->GetHandle());
			ASSERT(!corePopup->IsActive());
			corePopup->SetVisualTree(visualTree);
		}

		m_tpPopup.IsOpen = true;

		// Note: This is the call that propagates the SystemBackdrop object set on either this FlyoutBase or its
		// MenuFlyoutPresenter to the Popup. A convenient place to do this is in ForwardTargetPropertiesToPresenter, but we
		// see cases where the MenuFlyoutPresenter's SystemBackdrop property is null until it enters the tree via the
		// Popup::Open call above. So trying to propagate it before opening the popup actually finds no SystemBackdrop, and
		// the popup is left with a transparent background. Do the propagation after the popup opens instead. Windowed
		// popups support having a backdrop set after the popup is open.
		ForwardSystemBackdropToPopup();
	}

	private void EnsurePopupAndPresenter()
	{
		if (m_tpPopup is null)
		{
			Popup spPopup = new Popup();

			// Callback to derived class to create and initialize presenter instance.
			var spPresenter = CreatePresenter();

			var spPresenterAsFlyoutPresenter = spPresenter as FlyoutPresenter;

			if (spPresenterAsFlyoutPresenter is not null)
			{
				spPresenterAsFlyoutPresenter.Flyout = this;
			}

			PreparePresenter(spPresenter);

			// Hookup presenter to popup.
			var spPresenterAsUI = spPresenter;
			spPopup.Child = spPresenterAsUI;

			bool shouldConstrainToRootBounds = ShouldConstrainToRootBounds;
			spPopup.ShouldConstrainToRootBounds = shouldConstrainToRootBounds;
			spPopup.AssociatedFlyout = this;

			UIElement spPopupAsUI = spPopup;

			spPopupAsUI.LostFocus += OnPopupLostFocus;
			m_epPopupLostFocusHandler.Disposable = Disposable.Create(() => spPopupAsUI.LostFocus -= OnPopupLostFocus);

			m_tpPresenter = spPresenter;

			// Set IsDialog property to True for popup if inherited class is Flyout
			if (this is Flyout)
			{
				AutomationProperties.SetIsDialog(spPopup, true);
			}

			ctl::ComPtr<IDependencyObject> spPresenterAsDO;
			IFC_RETURN(spPresenter.As(&spPresenterAsDO));

			if (spPresenterAsDO.Cast<DependencyObject>()->GetHandle()->IsPropertyDefault(MetadataAPI::GetDependencyPropertyByIndex(KnownPropertyIndex::AutomationProperties_AutomationId)) &&
				!GetHandle()->IsPropertyDefault(MetadataAPI::GetDependencyPropertyByIndex(KnownPropertyIndex::AutomationProperties_AutomationId)))
			{
				wrl_wrappers::HString automationId;
				IFC_RETURN(DirectUI::AutomationProperties::GetAutomationIdStatic(this, automationId.ReleaseAndGetAddressOf()));
				IFC_RETURN(DirectUI::AutomationProperties::SetAutomationIdStatic(spPresenterAsDO.Get(), automationId.Get()));
			}

			if (spPresenterAsDO.Cast<DependencyObject>()->GetHandle()->IsPropertyDefault(MetadataAPI::GetDependencyPropertyByIndex(KnownPropertyIndex::AutomationProperties_Name)) &&
				!GetHandle()->IsPropertyDefault(MetadataAPI::GetDependencyPropertyByIndex(KnownPropertyIndex::AutomationProperties_Name)))
			{
				wrl_wrappers::HString automationName;
				IFC_RETURN(DirectUI::AutomationProperties::GetNameStatic(this, automationName.ReleaseAndGetAddressOf()));
				IFC_RETURN(DirectUI::AutomationProperties::SetNameStatic(spPresenterAsDO.Get(), automationName.Get()));
			}

			m_tpPopup = spPopup;
		}

		MUX_ASSERT(m_tpPresenter is not null);
		MUX_ASSERT(m_tpPopup is not null);
	}

	private void SetPopupLightDismissBehavior()
	{
		m_tpPopup.IsLightDismissEnabled = m_isLightDismissOverlayEnabled;

		if (m_shouldOverlayPassThroughAllInput)
		{
			DependencyObject overlayInputPassThroughElement = OverlayInputPassThroughElement;

			if (overlayInputPassThroughElement is null)
			{
				UIElement? rootVisual = m_wrRootVisual;

				if (rootVisual is not null)
				{
					DependencyObject rootVisualAsDO = rootVisual;

					OverlayInputPassThroughElement = rootVisualAsDO;
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
		MUX_ASSERT(m_tpPresenter is not null);
		MUX_ASSERT(m_tpPlacementTarget is not null);

		IFC_RETURN(m_tpPresenter.As(&spPresenterAsFE));

		IFC_RETURN(m_tpPlacementTarget->get_DataContext(&spDataContext));
		IFC_RETURN(spPresenterAsFE->put_DataContext(spDataContext.Get()));

		IFC_RETURN(m_tpPlacementTarget->get_FlowDirection(&flowDirection));
		IFC_RETURN(spPresenterAsFE->put_FlowDirection(flowDirection));

		IFC_RETURN(m_tpPlacementTarget->get_Language(strLanguage.GetAddressOf()));
		IFC_RETURN(spPresenterAsFE->put_Language(strLanguage.Get()));

		IFC_RETURN(m_tpPlacementTarget.Cast<FrameworkElement>()->get_IsTextScaleFactorEnabledInternal(&isTextScaleFactorEnabled));
		IFC_RETURN(spPresenterAsFE.Cast<FrameworkElement>()->put_IsTextScaleFactorEnabledInternal(isTextScaleFactorEnabled));

		IFC_RETURN(get_AllowFocusOnInteraction(&allowFocusOnInteraction));

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
		if (allowFocusOnInteraction
			&& !GetHandle()->OfTypeByIndex<KnownTypeIndex::MenuFlyout>())
		{
			bool isSubMenu = false;

			if (auto thisAsIMenu = ctl::query_interface_cast<IMenu>(this))
			{
				ctl::ComPtr<IMenu> parentMenu;
				IFC_RETURN(thisAsIMenu->get_ParentMenu(&parentMenu));

				isSubMenu = static_cast<bool>(parentMenu);
			}

			if (!isSubMenu)
			{
				IFC_RETURN(m_tpPlacementTarget.Cast<FrameworkElement>()->get_AllowFocusOnInteraction(&allowFocusOnInteraction));
			}
		}

		IFC_RETURN(spPresenterAsFE.Cast<FrameworkElement>()->put_AllowFocusOnInteraction(allowFocusOnInteraction))

		IFC_RETURN(get_AllowFocusWhenDisabled(&allowFocusWhenDisabled));

		//First check to see if FlyoutBase has the property set to true. If not, then pull from the placement target instead
		if (!allowFocusWhenDisabled)
		{
			IFC_RETURN(m_tpPlacementTarget.Cast<FrameworkElement>()->get_AllowFocusWhenDisabled(&allowFocusWhenDisabled));
		}

		IFC_RETURN(spPresenterAsFE.Cast<FrameworkElement>()->put_AllowFocusWhenDisabled(allowFocusWhenDisabled))

		// Flyout matches the theme of its placement target.
		{
			DependencyProperty requestedThemeProperty = FrameworkElement.RequestedThemeProperty;

			// We'll only override the requested theme on the presenter if its value hasn't been explicitly set.
			// Otherwise, we'll abide by its existing value.
			if (m_tpPresenter.IsPropertyDefault(requestedThemeProperty) ||
				m_isFlyoutPresenterRequestedThemeOverridden)
			{
				xaml::ElementTheme currentFlyoutPresenterTheme;
				xaml::ElementTheme requestedTheme = xaml::ElementTheme_Default;
				ctl::ComPtr<IDependencyObject> spCurrent;
				ctl::ComPtr<IDependencyObject> spParent;
				ctl::ComPtr<IFrameworkElement> spCurrentAsFE;

				IFC_RETURN(spPresenterAsFE->get_RequestedTheme(&currentFlyoutPresenterTheme));

				// Walk up the tree from the placement target until we find an element with a RequestedTheme.
				IFC_RETURN(m_tpPlacementTarget.As(&spCurrent));
				while (spCurrent)
				{
					IFC_RETURN(spCurrent.CopyTo(spCurrentAsFE.ReleaseAndGetAddressOf()));

					IFC_RETURN(spCurrentAsFE->get_RequestedTheme(&requestedTheme));

					if (requestedTheme != xaml::ElementTheme_Default)
					{
						break;
					}

					IFC_RETURN(VisualTreeHelper::GetParentStatic(spCurrent.Get(), spParent.ReleaseAndGetAddressOf()));
					if (spParent.AsOrNull<DirectUI::PopupRoot>())
					{
						// If the target is in a Popup and the Popup is in the Visual Tree, we want to inherrit the theme
						// from that Popup's parent. Otherwise we will get the App's theme, which might not be what
						// is expected.
						IFC_RETURN(spCurrentAsFE->get_Parent(&spParent));
					}

					spCurrent = spParent;
				}

				if (requestedTheme != currentFlyoutPresenterTheme)
				{
					IFC_RETURN(spPresenterAsFE->put_RequestedTheme(requestedTheme));
					m_isFlyoutPresenterRequestedThemeOverridden = true;
				}

				if (WinAppSdk::Containment::IsChangeEnabled<WINAPPSDK_CHANGEID_46253034>())
				{
					// Also set the popup's theme. If there is a SystemBackdrop on the menu, it'll be watching the theme on the
					// popup itself rather than the presenter set as the popup's child.
					IFC_RETURN(m_tpPopup.Cast<DirectUI::PopupGenerated>()->put_RequestedTheme(requestedTheme));
				}
			}
		}
	}

	_Check_return_ HRESULT ForwardSystemBackdropToPopup()
	{
		// Propagate the SystemBackdrop on the FlyoutBasePresenter to the underlying Popup. The FlyoutBase.SystemBackdrop property
		// is just a convenient place to store the SystemBackdrop object for flyouts. Popup is the place that actually
		// implements IXP's ICompositionSupportsSystemBackdrop interface to interop with IXP's SystemBackdropControllers and
		// to kick off calls to SystemBackdrop.OnTargetConnected/OnTargetDisconnected.
		ctl::ComPtr<ISystemBackdrop> flyoutSystemBackdrop = GetSystemBackdrop();
		ctl::ComPtr<ISystemBackdrop> popupSystemBackdrop;
		IFC_RETURN(m_tpPopup.Cast<DirectUI::PopupGenerated>()->get_SystemBackdrop(&popupSystemBackdrop));
		if (flyoutSystemBackdrop.Get() != popupSystemBackdrop.Get())
		{
			IFC_RETURN(m_tpPopup.Cast<DirectUI::PopupGenerated>()->put_SystemBackdrop(flyoutSystemBackdrop.Get()));
		}

		return S_OK;
	}

	ctl::ComPtr<ISystemBackdrop> GetSystemBackdrop()
	{
		//
		// There are multiple places that have a SystemBackdrop property:
		//
		//   1. Popups
		//
		//      Popups are the place that actually implement IXP's ICompositionSupportsSystemBackdrop interface to interop
		//      with IXP's SystemBackdropControllers and calls SystemBackdrop.OnTargetConnected/OnTargetDisconnected.
		//
		//   2. FlyoutBase
		//
		//      FlyoutBase.SystemBackdrop is a convenient place to store the SystemBackdrop object for flyouts, and is meant
		//      to be used in a code-behind. However, FlyoutBase can't be styled, which means we can't set the
		//      SystemBackdrop property from generic.xaml. For that, we need...
		//
		//   3. Flyout presenters (e.g. MenuFlyoutPresenter)
		//
		//      The presenter inside the flyout is the control that can actually be styled, so it has a SystemBackdrop
		//      property as well.
		//
		// The FlyoutBase.SystemBackdrop property (set via a code-behind) takes precedence over the Flyout presenter's
		// SystemBackdrop property (set via generic.xaml or via a style). Ultimately we take the SystemBackdrop set on
		// either the flyout or the presenter and set it on the underlying Popup.
		//
		ctl::ComPtr<ISystemBackdrop> systemBackdrop;
		ctl::ComPtr<MenuFlyoutPresenter> menuFlyoutPresenter;

		IFCFAILFAST(get_SystemBackdrop(&systemBackdrop));
		if (!systemBackdrop)
		{
			if (SUCCEEDED(m_tpPresenter.As(&menuFlyoutPresenter)))
			{
				IFCFAILFAST(menuFlyoutPresenter->get_SystemBackdrop(&systemBackdrop));
			}
		}
		return systemBackdrop;
	}

	private void ValidateAndSetParameters(FrameworkElement placementTarget, out bool shouldOpen)
	{
		var placementMode = GetEffectivePlacement();
		m_preferredJustification = GetJustificationFromPlacementMode(placementMode);
		var majorplacementMode = GetMajorPlacementFromPlacement(placementMode);
		ValidateFlyoutPlacementMode(placementMode);

		if (placementTarget is null)
		{
			throw new ArgumentNullException(nameof(placementTarget));
		}

		m_majorPlacementMode = majorplacementMode;
		SetPlacementTarget(placementTarget);

		// Find out placement target bounds when it's guaranteed to be in visual tree in case if it goes away
		// (as would be the case in flyout invoked by element from another flyout).
		CalculatePlacementTargetBoundsPrivate(
			  m_majorPlacementMode,
			  placementTarget,
			  out m_placementTargetBounds,
			  out m_allowPlacementFallbacks);

		ContentRoot contentRoot = VisualTree.GetContentRootForElement(placementTarget);

		m_inputDeviceTypeUsedToOpen = contentRoot.InputManager.LastInputDeviceType;
		// If the placement target isn't in the live tree, then there's nothing to anchor ourselves on.
		// In that case, we'll silently not open the flyout.
		*shouldOpen = spPlacementTarget.Cast<FrameworkElement>()->IsInLiveTree();
	}

	private bool CheckAndHandleOpenFlyout()
	{
		var shouldOpen = true;

		FlyoutMetadata flyoutMetadata = GetFlyoutMetadata();

		flyoutMetadata.GetOpenFlyout(out var openFlyout, out var openFlyoutPlacementTarget));

		if (openFlyout is not null)
		{
			shouldOpen = false;

			bool isSameFlyoutAsOpen = openFlyout == this;
			bool isSameTargetAsOpen = openFlyoutPlacementTarget == m_tpPlacementTarget;

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
				flyoutMetadata.GetStagedFlyout(out var stagedFlyout, out var stagedFlyoutPlacementTarget));

				if (stagedFlyout is not null)
				{
					// There is an open AND staged FlyoutBase.  Since we honor the latest request,
					// update staged FlyoutBase to this.
					flyoutMetadata.SetStagedFlyout(this, m_tpPlacementTarget);

					// Clear the staged flyout's parent reference to no longer mark it as a child flyout.
					stagedFlyout.m_tpParentFlyout.Clear();
				}
				else
				{
					// Either this is a different FlyoutBase or it is the same, but placement target is
					// different AND there is no outstanding request.  Hide previous instance and
					// queue this one to be shown.
					Control presenter = openFlyout.GetPresenter();

					openFlyout.Hide();
					flyoutMetadata.SetStagedFlyout(this, m_tpPlacementTarget);
				}
			}
		}

		m_wasTargetPositionSet = m_isTargetPositionSet;
		m_lastTargetPoint = m_targetPoint;

		return shouldOpen;
	}

	private void PerformPlacement(
		Size presenterSize,
		double bottomAppBarVerticalCorrection = 0.0,
		bool preparePopupTheme = true)
	{
		Rect availableWindowRect = new();
		Rect presenterRect = new();
		MajorPlacementMode effectivePlacementMode = GetMajorPlacementFromPlacement(g_defaultPlacementMode);
		Control? pPresenterAsControl = null;
		Popup pPopupNoRef = m_tpPopup;
		bool isPlacementPropertyLocal = false;

		MUX_ASSERT(m_tpPopup != null);
		MUX_ASSERT(m_tpPresenter != null);

		if (m_tpPlacementTarget is null)
		{
			// Placement target has been reset, which means request can be ignored.
			return;
		}

		pPresenterAsControl = m_tpPresenter;

		// Get available area to display Flyout, excluding space occupied by AppBars and IHM.
		CalculateAvailableWindowRect(
			m_tpPopup,
			m_tpPlacementTarget,
			m_isTargetPositionSet,
			m_targetPoint,
			m_majorPlacementMode == MajorPlacementMode.Full,
			out availableWindowRect);

		// If placement target is in live tree, get its coordinates.  If not, reuse ones cached when FlyoutBase
		// ShowAt was first called (support for scenario where flyout is invoked from element in another flyout).

		if (m_tpPlacementTarget.IsInLiveTree()) //TODO:MZ: Find alternative
		{
			CalculatePlacementTargetBoundsPrivate(
				&m_majorPlacementMode,
				m_tpPlacementTarget.Get(),
				&m_placementTargetBounds,
				&m_allowPlacementFallbacks,
				bottomAppBarVerticalCorrection);
		}

		var placementTargetFlowDirection = m_tpPlacementTarget.FlowDirection;

		// Reverse meaning of left and right for RTL.
		// Popup's coordinates (0, 0) are always in upper-left hand corner.
		effectivePlacementMode = GetEffectivePlacementMode(
			m_majorPlacementMode,
			placementTargetFlowDirection);

		IsPropertyLocal(
			MetadataAPI.GetDependencyPropertyByIndex(KnownPropertyIndex.FlyoutBase_Placement),
			&isPlacementPropertyLocal));

		if (!isPlacementPropertyLocal)
		{
			AutoAdjustPlacement(&effectivePlacementMode));
		}

		{
			var minPresenterSize = GetMinimumSize(pPresenterAsControl);
			var maxPresenterSize = GetMaximumSize(pPresenterAsControl);

			CalculatePlacementPrivate(
				&effectivePlacementMode,
				m_preferredJustification,
				m_allowPlacementFallbacks,
				m_allowPresenterResizing,
				m_placementTargetBounds,
				presenterSize,
				minPresenterSize,
				maxPresenterSize,
				availableWindowRect,
				IsWindowedPopup(),
				out presenterRect);
		}

		// Position popup.
		if (m_isTargetPositionSet)
		{
			double showAtVerticalOffset;
			showAtVerticalOffset = m_tpPopup.VerticalOffset;
			presenterRect = UpdateTargetPosition(availableWindowRect, presenterSize);

			// If this flyout has a target position set, the placement is only used
			// to determine the direction of the MenuFlyout transition which can animate
			// either from Top or Bottom.
			effectivePlacementMode =
				(showAtVerticalOffset > static_cast<double>(presenterRect.Y)) ?
				MajorPlacementMode.Top :
				MajorPlacementMode.Bottom;
		}
		else
		{
			if (placementTargetFlowDirection == FlowDirection.LeftToRight)
			{
				m_tpPopup.HorizontalOffset = presenterRect.X;
			}
			else
			{
				m_tpPopup.HorizontalOffset = presenterRect.X + presenterRect.Width;
			}

			m_tpPopup.VerticalOffset = presenterRect.Y;
		}



		if (presenterSize.Width != presenterRect.Width ||
			presenterSize.Height != presenterRect.Height)
		{
			pPresenterAsControl.Width = presenterRect.Width;
			pPresenterAsControl.Height = presenterRect.Height;
			m_presenterResized = true;
		}

		// We don't want our popup to grab the focus since we will be focusing the flyout ourselves.
		CoreImports.Popup_SetShouldTakeFocus(pPopupNoRef, false);

		if (preparePopupTheme)
		{
			// Note: transitions already respect flow direction, so we undo the flow-direction change to
			// effective placement before setting up the transition.
			effectivePlacementMode = GetEffectivePlacementMode(
				effectivePlacementMode,
				placementTargetFlowDirection);

			PreparePopupTheme(
				pPopupNoRef,
				effectivePlacementMode,
				m_tpPlacementTarget);
		}

		static_cast<CFlyoutBase*>(GetHandle()).OnPlacementUpdated(effectivePlacementMode));
	}

	private void PreparePresenter(Control presenter)
	{
		MUX_ASSERT(presenter is not null);

		presenter.SizeChanged += OnPresenterSizeChanged;
		m_epPresenterSizeChangedHandler.Disposable = Disposable.Create(() => presenter.SizeChanged -= OnPresenterSizeChanged);
		presenter.Loaded += OnPresenterLoaded;
		m_epPresenterLoadedHandler.Disposable = Disposable.Create(() => presenter.Loaded -= OnPresenterLoaded);
		presenter.Unloaded += OnPresenterUnloaded;
		m_epPresenterUnloadedHandler.Disposable = Disposable.Create(() => presenter.Unloaded -= OnPresenterUnloaded);
	}

	private void PreparePopupTheme(
			Popup pPopup,
			MajorPlacementMode placementMode,
			FrameworkElement pPlacementTarget)
	{
		bool areOpenCloseAnimationsEnabled = AreOpenCloseAnimationsEnabled;

		if (m_tpThemeTransition is null && areOpenCloseAnimationsEnabled)
		{
			Transition spTransition;
			TransitionCollection spTransitionCollection = new TransitionCollection();

			if (m_usePickerFlyoutTheme)
			{
				var spPickerFlyoutThemeTransition = new PickerFlyoutThemeTransition();
				spTransition = spPickerFlyoutThemeTransition;
			}
			else
			{
				var spPopupThemeTransition = new PopupThemeTransition();
				spTransition = spPopupThemeTransition;

				FrameworkElement overlayElement = pPopup.OverlayElement;
				spPopupThemeTransition.SetOverlayElement(overlayElement);

				SetTransitionParameters(spTransition, placementMode);
			}

			spTransitionCollection.Add(spTransition);

			// Allow LTEs to target Popup.Child when Popup is windowed
			// ThemeTransition can't be applied to child of a windowed popup, targeting grandchild here.
			if (IsWindowedPopup())
			{
				UIElement popupChild = pPopup.Child;
				if (popupChild is not null)
				{
					DependencyObject popupGrandChildAsDO = VisualTreeHelper.GetChild(popupChild, 0);
					if (popupGrandChildAsDO is UIElement popupGrandChildAsUIE)
					{
						popupGrandChildAsUIE.Transitions = spTransitionCollection;
						popupGrandChildAsUIE.InvalidateMeasure();
					}
				}
			}
			else
			{
				pPopup.ChildTransitions = spTransitionCollection;
			}

			m_tpThemeTransition = spTransition;
		}
	}

	private void SetTransitionParameters(
		Transition pTransition,
		MajorPlacementMode placementMode)
	{
		MUX_ASSERT(pTransition is not null);

		var spTransition = pTransition;

		var transitionAsPopupThemeTransition = spTransition as PopupThemeTransition;

		if (transitionAsPopupThemeTransition is not null)
		{
			switch (placementMode)
			{
				case MajorPlacementMode.Full:
					transitionAsPopupThemeTransition.FromHorizontalOffset = 0.0;
					transitionAsPopupThemeTransition.FromVerticalOffset = 0.0;
					break;

				case MajorPlacementMode.Top:
					transitionAsPopupThemeTransition.FromHorizontalOffset = 0.0;
					transitionAsPopupThemeTransition.FromVerticalOffset = g_entranceThemeOffset;
					break;

				case MajorPlacementMode.Bottom:
					transitionAsPopupThemeTransition.FromHorizontalOffset = 0.0;
					transitionAsPopupThemeTransition.FromVerticalOffset = g_entranceThemeOffset;
					break;

				case MajorPlacementMode.Left:
					transitionAsPopupThemeTransition.FromHorizontalOffset = g_entranceThemeOffset;
					transitionAsPopupThemeTransition.FromVerticalOffset = 0.0;
					break;

				case MajorPlacementMode.Right:
					transitionAsPopupThemeTransition.FromHorizontalOffset = -g_entranceThemeOffset;
					transitionAsPopupThemeTransition.FromVerticalOffset = 0.0;
					break;

				default:
					MUX_ASSERT(false, "Unsupported MajorPlacementMode");
					throw new InvalidOperationException("Unexpected MajorPlacementMode");
			}
		}
	}

	private void RaiseOpenedEvent()
	{
		DBG_FLYOUT_TRACE(L">>> FlyoutBase::RaiseOpenedEvent()");

		Opened?.Invoke(this, EventArgs.Empty); //TODO:MZ:EventArgs.Empty?
	}

	private void OnClosing(out bool cancel)
	{
		DBG_FLYOUT_TRACE(L">>> FlyoutBase::OnClosing()");

		cancel = false;

		FlyoutBaseClosingEventArgs eventArgs = new FlyoutBaseClosingEventArgs();

		Closing?.Invoke(this, eventArgs);

		bool cancelRequested = eventArgs.Cancel;

		cancel = cancelRequested;

		if (!cancelRequested)
		{
			ElementSoundPlayerService.RequestInteractionSoundForElementStatic(ElementSoundKind.Hide, this);
		}
	}

	private void OnClosed()
	{
		DBG_FLYOUT_TRACE(L">>> FlyoutBase::OnClosed()");

		m_isTargetPositionSet = false;
		InputDevicePrefersPrimaryCommands = false;

		// Now that the flyout has closed, reset its m_hasPlacementOverride flag so that
		// it will be valid the next time it opens again.
		m_hasPlacementOverride = false;

		// Also reset the m_isLightDismissOverlayEnabled flag to its default TRUE value
		// so that the light dismiss overlay is recreated as needed when reopening.
		m_isLightDismissOverlayEnabled = TRUE;

		RaiseClosedEvent();
	}


	private void RaiseClosedEvent()
	{
		DBG_FLYOUT_TRACE(L">>> FlyoutBase::RaiseClosedEvent()");

		Closed?.Invoke(this, EventArgs.Empty); //TODO:MZ:EventArgs.Empty?
	}

	private void OnOpening()
	{
		DBG_FLYOUT_TRACE(L">>> FlyoutBase::OnOpening()");

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

		ElementSoundPlayerService.RequestInteractionSoundForElementStatic(ElementSoundKind.Show, this);

		RaiseOpeningEvent();
	}


	private void RaiseOpeningEvent()
	{
		DBG_FLYOUT_TRACE(L">>> FlyoutBase::RaiseOpeningEvent()");

		m_openingCanceled = false;

		Opening?.Invoke(this, EventArgs.Empty); //TODO:MZ:EventArgs.Empty?
	}

	private void OnPresenterSizeChanged(
		object pSender,
		SizeChangedEventArgs pArgs)
	{
		DBG_FLYOUT_TRACE(L">>> FlyoutBase::OnPresenterSizeChanged()");

		// Perform placement when size of presenter is known.
		var presenterSize = pArgs.NewSize;

		if (!m_presenterResized)
		{
			PerformPlacement(presenterSize);
		}
	}

	// Use WM_CHANGEUISTATE/WM_QUERYUISTATE messages to determine if we should show focus indicators (based on the
	// most recently-used input device of the system).  Note some top level windows (e.g., WPF) don't seem to propagate
	// the WM_CHANGEUISTATE/WM_UPDATEUISTATE messages like DefWindowProc does, which blocks this from working on arbitrary
	// child windows.  For MenuFlyout, the Popup window HWND works well here because it has no parent window that could
	// interfere with the messages.
	bool AreSystemFocusIndicatorsVisible(_In_ HWND wnd)
	{
		::SendMessage(wnd, WM_CHANGEUISTATE, MAKEWPARAM(UIS_INITIALIZE, 0), 0);
		auto uiState = ::SendMessage(wnd, WM_QUERYUISTATE, 0, 0);

		// Windows has different flags to hide accelerators (UISF_HIDEACCEL) and focus indicators (UISF_HIDEFOCUS),
		// but in XAML we don't track this distinction, so we just show the focus visuals if either flag is set.
		if ((uiState & (UISF_HIDEFOCUS | UISF_HIDEACCEL)) == 0)
		{
			return true;
		}
		return false;
	}

	private void OnPresenterLoaded(
		object pSender,
		RoutedEventArgs pArgs)
	{
		DBG_FLYOUT_TRACE(L">>> FlyoutBase::OnPresenterLoaded()");

		bool allowFocusOnInteraction = AllowFocusOnInteraction;
		DependencyObject target = this;

		// First check to see if FlyoutBase has the property set to false. If not, then pull from the presenter instead,
		// if we have one - in rare occasions we may not, if OnPresenterLoaded() is called immediately after OnPresenterUnloaded()
		// without any call to ShowAt(), and we don't want to crash in such a circumstance.
		if (allowFocusOnInteraction && m_tpPlacementTarget is not null)
		{
			target = m_tpPlacementTarget;
		}

		if (m_shouldTakeFocus)
		{
			// Propagate focus to the first focusable child.
			DirectUI::FocusState focusState = CFocusManager::GetFocusStateFromInputDeviceType(m_inputDeviceTypeUsedToOpen);

			CContentRoot* contentRoot = VisualTree::GetContentRootForElement(target);
			CInputManager& inputManager = contentRoot->GetInputManager();

			// In some islands scenarios (e.g., Win+X menu) the app calls ShowAt in response to input that XAML is not aware of (because
			// it's happening outside the island)  Since XAML won't know then what the last input device _really_ was, we update XAML's
			// notion of last input device with what the system has here, and open the Flyout with the appropriate FocusState.
			if (m_tpPopup && contentRoot->GetType() == CContentRoot::XamlIsland)
			{
				CPopup* corePopup {static_cast<CPopup*>(m_tpPopup.Cast<Popup>()->GetHandle())};
				HWND popupWnd = corePopup->GetWindowHandle();
				if (popupWnd)
				{
					const bool shouldShowKeyboardIndicators = AreSystemFocusIndicatorsVisible(popupWnd);
					if (shouldShowKeyboardIndicators && focusState != DirectUI::FocusState::Keyboard)
					{
						inputManager.SetLastInputDeviceType(DirectUI::InputDeviceType::Keyboard);
						focusState = CFocusManager::GetFocusStateFromInputDeviceType(inputManager.GetLastInputDeviceType());

					}
					else if (!shouldShowKeyboardIndicators && focusState == DirectUI::FocusState::Keyboard)
					{
						inputManager.SetLastInputDeviceType(DirectUI::InputDeviceType::None);
						focusState = CFocusManager::GetFocusStateFromInputDeviceType(inputManager.GetLastInputDeviceType());
					}
				}
			}

			// If the user is using a UIA client such as Narrator, we want to treat things as if the device used to open
			// the flyout was a keyboard. Otherwise, AllowFocusOnInteraction being false might result in focus not moving into the
			// flyout for a Narrator user.
			if (inputManager.GetWasUIAFocusSetSinceLastInput())
			{
				focusState = DirectUI::FocusState::Keyboard;
			}

			// Note: Normally we check whether the target has AllowFocusOnInteraction set before having the flyout take
			// focus. The exception is MenuFlyout, which is allowed to take focus regardless of what its target's
			// AllowFocusOnInteraction value is. We do this so the user can immediately navigate between menu items with the
			// arrow keys after opening it. This covers cases like MenuFlyouts opening from AppBarButtons, where the target
			// AppBarButton will have AllowFocusOnInteraction="False" in the default template.
			if (Focus::FocusSelection::ShouldUpdateFocus(target, focusState)
				|| (allowFocusOnInteraction && GetHandle()->OfTypeByIndex<KnownTypeIndex::MenuFlyout>()))
			{
				BOOLEAN childFocused = FALSE;
				IFC_RETURN(m_tpPresenter.AsOrNull<xaml::IUIElement>()->Focus(
					static_cast<xaml::FocusState>(focusState),
					&childFocused));

				if (!childFocused)
				{
					// If we failed to focus the flyout, focus the popup itself.
					// If the focused child or the flyout itself has AllowFocusOnInteraction set to false, do not try and set focus
					CPopup *PopupAsNative = static_cast<CPopup*>((m_tpPopup.Cast<Popup>()->GetHandle()));
					IFC_RETURN(PopupAsNative->SetFocus(focusState));
				}
			}
		}

		RaiseOpenedEvent();
	}

	// Raise closed event and open staged Flyout in case one exists.
	private void OnPresenterUnloaded(object pSender, RoutedEventArgs pArgs)
	{
		DBG_FLYOUT_TRACE(L">>> FlyoutBase::OnPresenterUnloaded()");

		bool wasTargetPositionSet = m_isTargetPositionSet;
		Point targetPoint = new(m_targetPoint.X, m_targetPoint.Y);

		OnClosed();

		FlyoutMetadata flyoutMetadata = GetFlyoutMetadata();

		flyoutMetadata.SetOpenFlyout(null, null);

		flyoutMetadata.GetStagedFlyout(out var stagedFlyout, out var stagedFlyoutPlacementTarget);

		// If our presenter was unloaded as part of tearing down the island, then don't try to reopen the staged flyout.
		VisualTree* stagedFlyoutVisualTree = stagedFlyout ? stagedFlyout.Cast<FlyoutBase>()->GetHandle()->GetVisualTree() : nullptr;

		if (stagedFlyout && stagedFlyoutVisualTree)
		{
			// If not reopening, clear the reference to placement target.  If it is reopening, it will be cleared later.
			// This state can be reached if open flyout is re-opened with a different placement target.
			if (stagedFlyout == this)
			{
				SetPlacementTarget(null);

				// Clear our parent reference to no longer mark this flyout as a child flyout.
				m_tpParentFlyout.Clear();
			}
			else
			{
				// If we're reopening a flyout, preserve target position
				MUX_ASSERT(targetPoint.X == m_targetPoint.X && targetPoint.Y == m_targetPoint.Y);
				m_isTargetPositionSet = wasTargetPositionSet;
			}

			flyoutMetadata.SetStagedFlyout(null, null);
			stagedFlyout.Open();
		}
		else
		{
			SetPlacementTarget(null);

			// Clear our parent reference to no longer mark this flyout as a child flyout.
			m_tpParentFlyout.Clear();
		}
	}

	private void OnPopupLostFocus(object pSender, RoutedEventArgs pArgs)
	{
		DBG_FLYOUT_TRACE(L">>> FlyoutBase::OnPopupLostFocus()");

		bool isOpen = false;

		if (m_tpPopup is not null)
		{
			isOpen = m_tpPopup.IsOpen;
		}

		if (isOpen)
		{
			DependencyObject focusedElement = GetFocusedElement();

			// We only want to close when we lose focus in the case where the newly focused element
			// does not reside in any other flyout - otherwise, this will conflict with
			// the functionality of child flyouts.
			// Note that tap-and-hold can cause the root scroll viewer to take focus temporarily,
			// which we don't want to respond to, so we'll ignore that.
			if (focusedElement && (!m_isLightDismissOverlayEnabled || !focusedElement.AsOrNull<RootScrollViewer>()))
			{
				DependencyObject currentObject = focusedElement;
				UIElement currentElement = currentObject;

				// If the focused element is a text element (which is not a UI element),
				// then we need to get its containing element.
				if (currentElement is null)
				{
					TextElement currentTextElement = currentObject as TextElement;

					if (currentTextElement)
					{
						currentElement = currentTextElement.GetContainingFrameworkElement();
					}
				}

				Popup popupAncestor = null;
				bool popupRootExists = false;

				while (currentElement is not null)
				{
					if (currentElement is PopupRoot)
					{
						popupRootExists = true;
						currentElement = currentElement.GetLogicalParentNoRef();
						continue;
					}

					if (currentElement is Popup elementCast)
					{
						popupAncestor = elementCast;
						break;
					}

					currentElement = currentElement.GetUIElementAdjustedParentInternal(false);
				}

				// In the case of XamlIslands, it may be the case that we can't retrieve a popup object.
				// In that case, we'll assume that it was in a popup if we found the popup root.
				// We can't tell if it was a light-dismiss popup in that case, but we'll err on the side
				// of not hiding in that circumstance.
				if (!popupRootExists && (popupAncestor is null || !popupAncestor.IsSelfOrAncestorLightDismiss()))
				{
					Hide();
				}
			}
		}
	}

	// Hides the flyout in TransientWithDismissOnPointerMoveAway mode when the pointer moves more than 80 pixels away.
	private void OnRootVisualPointerMoved(
		object pSender,
		PointerRoutedEventArgs pArgs)
	{
#if FLYOUT_DEBUG
		MUX_ASSERT(m_tpPopup is not null);
		bool isOpen = false = m_tpPopup.IsOpen;
		MUX_ASSERT(isOpen);
#endif

		// The flyout is hidden when the pointer is moved more than 80 pixels
		// away from its bounds. This is the square of that threshold distance.
		const double hidingThreshold = 6400;

		var pointerPoint = pArgs.GetCurrentPoint(null);

		Point pointerPosition = pointerPoint.Position;

		Size presenterSize = GetPresenterSize();

		GeneralTransform transformToRoot = m_tpPresenter.TransformToVisual(null);

		Rect presenterBounds = new(0, 0, presenterSize.Width, presenterSize.Height);
		Rect rootPresenterBounds = transformToRoot.TransformBounds(presenterBounds);

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
	private void CalculatePlacementPrivate(
		MajorPlacementMode pMajorPlacement,
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
		pControlRect = new();
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
			Size controlSizeWithMargins = new();
			Size minControlSizeWithMargins = new();
			var placementOrder = new MajorPlacementMode[4];
			Point controlPos = new();

			pControlRect.X = 0.0;
			pControlRect.Y = 0.0;
			pControlRect.Width = controlSize.Width;
			pControlRect.Height = controlSize.Height;

			controlSizeWithMargins.Width = controlSize.Width;
			controlSizeWithMargins.Height = controlSize.Height;

			minControlSizeWithMargins.Width = minControlSize.Width;
			minControlSizeWithMargins.Height = minControlSize.Height;

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
					MUX_ASSERT(false, "Unsupported MajorPlacementMode");
					throw new InvalidOperationException("Unsupported MajorPlacementMode");
					break;
			}

			if (!PerformPlacementWithFallback(
					placementTargetBounds,
					controlSizeWithMargins,
					containerRect,
					placementOrder,
					(allowFallbacks) ? placementOrder.Length : 1,
					justification,
					out controlPos,
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
				pControlRect.Width = DoubleUtil.Max(0.0, controlSizeWithMargins.Width);
				pControlRect.Height = DoubleUtil.Max(0.0, controlSizeWithMargins.Height);
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
					MUX_ASSERT(false, "Unsupported MajorPlacementMode");
					throw new InvalidOperationException("Unsupported MajorPlacementMode");
					break;
			}

			// If the flyout is windowed, then a xy-position less than 0 is acceptable - that means we're displaying above or to the left of the window.
			if (!isWindowed)
			{
				pControlRect->X = static_cast<FLOAT>(DoubleUtil::Max(0.0, pControlRect->X));
				pControlRect->Y = static_cast<FLOAT>(DoubleUtil::Max(0.0, pControlRect->Y));
			}
		}
	}

	// Calculate bounding rectangle of the placement target taking into consideration whether target is on AppBar.
	private void CalculatePlacementTargetBoundsPrivate(
		MajorPlacementMode pPlacement,
		FrameworkElement placementTarget,
		out Rect pPlacementTargetBounds,
		out bool pAllowFallbacks,
		double bottomAppBarVerticalCorrection = 0.0)
	{
		AppBarMode appBarMode = AppBarMode.Floating;
		bool isTopOrBottomAppBar = false;

		pAllowFallbacks = false;
		pPlacementTargetBounds = new();

		var placementTargetWidth = placementTarget.ActualWidth;
		var placementTargetHeight = placementTarget.ActualHeight;

		pPlacementTargetBounds.Width = placementTargetWidth;
		pPlacementTargetBounds.Height = placementTargetHeight;

		var appBar = FindParentAppBar(placementTarget);

		if (appBar is not null)
		{
			appBarMode = appBar.GetMode();
			isTopOrBottomAppBar = (appBarMode == AppBarMode.Top || appBarMode == AppBarMode.Bottom);
		}

		if (!isTopOrBottomAppBar || (pPlacement == MajorPlacementMode.Full))
		{
			// Non-AppBar case and non-Top/BottomAppBar and FullPlacementMode, transform target to root screen coordinates.
			GeneralTransform spTargetToRootTransform = placementTarget.TransformToVisual(null);
			pPlacementTargetBounds = spTargetToRootTransform.TransformBounds(pPlacementTargetBounds);
			pAllowFallbacks = true;
		}
		else
		{
			// For Top/Bottom AppBars, expand target bounds to AppBar size and transform to root screen coordinates.
			GeneralTransform spTargetToAppBarTransform;
			GeneralTransform spAppBarToRootTransform;
			Rect placementTargetBoundsInAppBar = new();
			double appBarHeight = 0.0;

			spTargetToAppBarTransform = placementTarget.TransformToVisual(appBar);
			placementTargetBoundsInAppBar = spTargetToAppBarTransform.TransformBounds(pPlacementTargetBounds);

			appBarHeight = appBar!.ActualHeight;
			placementTargetBoundsInAppBar.Height = appBarHeight;

			spAppBarToRootTransform = appBar.TransformToVisual(null);
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
				MUX_ASSERT(false, "Unsupported AppBarMode");
				throw new InvalidOperationException("Unsupported AppBarMode");
			}
		}
	}

	private void CloseOpenFlyout(FlyoutBase? parentFlyout)
	{
		DBG_FLYOUT_TRACE(L">>> FlyoutBase::CloseOpenFlyout()");

		FlyoutMetadata flyoutMetadata;

		if (parentFlyout is null)
		{
			flyoutMetadata = DXamlCore.Current.FlyoutMetadata;
		}
		else
		{
			flyoutMetadata = parentFlyout.m_childFlyoutMetadata;
		}

		if (flyoutMetadata is not null)
		{
			// Clear the staged flyout before we hide the open flyout to make sure we don't
			// end up just opening a new flyout.
			flyoutMetadata.SetStagedFlyout(null, null);

			flyoutMetadata.GetOpenFlyout(out var openFlyout, out _);

			if (openFlyout is not null)
			{
				openFlyout.Hide();
			}
		}
	}

	private static void HideFlyout(FlyoutBase flyout)
	{
		DBG_FLYOUT_TRACE(L">>> FlyoutBase::HideFlyout()");

		if (flyout is not null)
		{
			flyout.Hide();
		}
	}

	/* static */
	private static Rect CalculateAvailableWindowRect(
		Popup popup,
		FrameworkElement? placementTarget,
		bool hasTargetPosition,
		Point targetPosition,
		bool isFull)
	{
		Rect pAvailableRect = new();
		DXamlCore dxamlCore = DXamlCore.Current;

		// If the popup is windowed, then the available window rect is the monitor that it will appear in.
		// The exception is if we're opening in full mode, in which case we want it to overlay the content area,
		// not the entire monitor area.
		if (!isFull && popup.IsWindowed())
		{
			Point popupPosition = new();

			if (hasTargetPosition)
			{
				popupPosition = targetPosition;
			}
			else
			{
				MUX_ASSERT(placementTarget is not null);

				var actualWidth = placementTarget!.ActualWidth;
				var actualHeight = placementTarget.ActualHeight;

				Point centerOfTarget = new(actualWidth / 2.0, actualHeight / 2.0);

				GeneralTransform transformToRoot = placementTarget.TransformToVisual(null);
				centerOfTarget = transformToRoot.TransformPoint(centerOfTarget);

				popupPosition = centerOfTarget;
			}

			pAvailableRect = DXamlCore.Current.CalculateAvailableMonitorRect(popup, popupPosition);
		}
		else
		{
			bool isTopAppBarOpen = false;
			bool isTopAppBarSticky = false;
			XFLOAT topAppBarWidth = 0.0f;
			XFLOAT topAppBarHeight = 0.0f;
			bool isBottomAppBarOpen = false;
			bool isBottomAppBarSticky = false;
			XFLOAT bottomAppBarWidth = 0.0f;
			XFLOAT bottomAppBarHeight = 0.0f;

			pAvailableRect->X = 0.0;
			pAvailableRect->Y = 0.0;
			pAvailableRect->Width = 0.0;
			pAvailableRect->Height = 0.0;

			wf::Rect windowBounds;
			IFC_RETURN(dxamlCore->GetContentBoundsForElement(popup->GetHandle(), &windowBounds));

			// Apply the visible bounds for calculating the available bounds area
			// that excludes the system tray and soft steering wheel navigation bar.
			IFC_RETURN(dxamlCore->GetVisibleContentBoundsForElement(true /*ignoreIHM*/, false /*inDesktopCoordinates*/, popup->GetHandle(), pAvailableRect));

			if (const auto xamlRoot = XamlRoot::GetImplementationForElementStatic(popup))
			{
				ctl::ComPtr<IApplicationBarService> applicationBarService;
				IFC_RETURN(xamlRoot->GetApplicationBarService(applicationBarService));
				if (applicationBarService)
				{
					IFC_RETURN(applicationBarService->GetAppBarStatus(&isTopAppBarOpen, &isTopAppBarSticky,
								&topAppBarWidth, &topAppBarHeight, &isBottomAppBarOpen, &isBottomAppBarSticky,
								&bottomAppBarWidth, &bottomAppBarHeight));
				}
			}

			if (isTopAppBarOpen)
			{
				double clampedTopAppBarHeight = DoubleUtil.Min(topAppBarHeight, pAvailableRect.Height);
				pAvailableRect.Y = clampedTopAppBarHeight;
				pAvailableRect.Height = pAvailableRect.Height - clampedTopAppBarHeight;
			}

			if (isBottomAppBarOpen)
			{
				pAvailableRect.Height = pAvailableRect.Height - bottomAppBarHeight;
			}

			var inputPaneOccludeRectInDips = DXamlCore.Current.GetInputPaneOccludeRect(popup);

			var intersectionRect = pAvailableRect;

			RectUtil.Intersect(intersectionRect, inputPaneOccludeRectInDips);

			if (intersectionRect.Height > 0)
			{
				pAvailableRect.Height = pAvailableRect.Height - intersectionRect.Height;
			}

			pAvailableRect.Height = DoubleUtil.Max(0.0, pAvailableRect.Height);
		}

		return pAvailableRect;
	}

	internal void SetPresenterStyle(Control presenter, Style style)
	{
		MUX_ASSERT(presenter is not null);

		if (style is not null)
		{
			presenter!.Style = style;
		}
		else
		{
			presenter!.ClearValue(FrameworkElement.StyleProperty);
		}
	}

	// React to the IHM (touch keyboard) showing or hiding.
	// Flyout should always be shown above the IHM.
	private void NotifyInputPaneStateChange(
		InputPaneState inputPaneState,
		Rect inputPaneBounds)
	{
		double bottomAppBarVerticalCorrection = 0.0;
		var zoomScale = RootScale.GetRasterizationScaleForElement(this);

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
				m_tpPresenter.Width = DoubleUtil.NaN;
				m_tpPresenter.Height = DoubleUtil.NaN;
				m_presenterResized = false;
			}
		}

		PerformPlacement(presenterSize, bottomAppBarVerticalCorrection);
	}

	// Helper method to set/clear m_tpPlacementTarget and maintain associated state and event handlers.
	private void SetPlacementTarget(FrameworkElement? pPlacementTarget)
	{
		// Clear the old placement target if it exists.
		if (m_tpPlacementTarget is not null)
		{
			m_epPlacementTargetUnloadedHandler.Disposable = null;
			m_tpPlacementTarget = null;
			Target = null;
		}

		if (pPlacementTarget is not null)
		{
			Target = pPlacementTarget;
			m_tpPlacementTarget = pPlacementTarget;

			var placement = GetEffectivePlacement();

			// Hook up an event handler that will dismiss the flyout if the placement target gets unloaded.
			void PlacementTargetUnloadedHandler(object sender, RoutedEventArgs args)
			{
				Hide();
			}
			m_tpPlacementTarget.Unloaded += PlacementTargetUnloadedHandler;
			m_epPlacementTargetUnloadedHandler.Disposable = Disposable.Create(() => m_tpPlacementTarget.Unloaded -= PlacementTargetUnloadedHandler);

		}
	}

	/* static */ _Check_return_ HRESULT FlyoutBase::GetPlacementTargetNoRef(
		_In_ CFlyoutBase* flyout,
		_Outptr_ CFrameworkElement** placementTarget)
	{
		ctl::ComPtr<DependencyObject> flyoutPeer;
		IFC_RETURN(DXamlCore::GetCurrent()->GetPeer(flyout, &flyoutPeer));
		ASSERT(ctl::is<IFlyoutBase>(flyoutPeer));

		*placementTarget = static_cast<CFrameworkElement*>(flyoutPeer.Cast<FlyoutBase>()->m_tpPlacementTarget.Cast<FrameworkElement>()->GetHandle());
		return S_OK;
	}

	// Private interface Implementation
	internal bool UsePickerFlyoutTheme
	{
		get => m_usePickerFlyoutTheme;
		set => m_usePickerFlyoutTheme = value;
	}

	private void PlaceFlyoutForDateTimePickerImpl(Point point)
	{
		PickerFlyoutThemeTransition spTransitionAsPickerFlyoutThemeTransition;

		m_isPositionedForDateTimePicker = true;

		ForwardPopupFlowDirection();
		SetTargetPosition(point);
		ApplyTargetPosition();
		var presenterSize = GetPresenterSize();
		PerformPlacement(presenterSize);

		spTransitionAsPickerFlyoutThemeTransition = m_tpThemeTransition as PickerFlyoutThemeTransition;

		if (spTransitionAsPickerFlyoutThemeTransition is not null)
		{
			double popupOffset = 0.0;
			Point centerOfTarget = new(0, 0);

			popupOffset = m_tpPopup.VerticalOffset;

			if (m_tpPlacementTarget is not null)
			{
				var spTransformFromTargetToWindow = m_tpPlacementTarget.TransformToVisual(null);
				centerOfTarget = spTransformFromTargetToWindow.TransformPoint(centerOfTarget);

				var actualHeight = m_tpPlacementTarget.ActualHeight;
				centerOfTarget.Y += actualHeight / 2.0;
			}

			double center = popupOffset + presenterSize.Height / 2;
			double offsetFromCenter = centerOfTarget.Y - center;
			spTransitionAsPickerFlyoutThemeTransition.OpenedLength = presenterSize.Height;
			spTransitionAsPickerFlyoutThemeTransition.OffsetFromCenter = offsetFromCenter;
		}
	}

	// Hooks up the root visual's PointerMoved event so the flyout can be automatically
	// hidden in TransientWithDismissOnPointerMoveAway mode.
	private void AddRootVisualPointerMovedHandler()
	{
		if (m_rootVisualPointerMovedToken.Disposable is null)
		{
			UIElement rootVisual = m_wrRootVisual;

			rootVisual.PointerMoved += OnRootVisualPointerMoved;
			m_rootVisualPointerMovedToken.Disposable = Disposable.Create(
				() => rootVisual.PointerMoved -= OnRootVisualPointerMoved);
		}
	}

	// Unhooks the root visual's PointerMoved event.
	private void RemoveRootVisualPointerMovedHandler()
	{
		if (m_rootVisualPointerMovedToken.Disposable is not null)
		{
			ctl::ComPtr<IUIElement> rootVisual;
        	IFC_RETURN(m_wrRootVisual.As(&rootVisual));

			if (rootVisual)
			{
				IFC_RETURN(rootVisual->remove_PointerMoved(m_rootVisualPointerMovedToken));
			}
		}
	}

	//------------------------------------------------------------------------
	// FlyoutBaseFactory
	//------------------------------------------------------------------------

	/// <summary>
	/// Shows the flyout associated with the specified element, if any.
	/// </summary>
	/// <param name="flyoutOwner">The element for which to show the associated flyout.</param>
	/// <exception cref="InvalidOperationException">Thrown when flyout does not have an associated flyout.</exception>
	public static ShowAttachedFlyout(FrameworkElement flyoutOwner)
	{
		var flyoutBase = GetAttachedFlyout(flyoutOwner);

		if (flyoutBase is not null)
		{
			flyoutBase.ShowAt(flyoutOwner);
		}
		else
		{
			//TODO:MZ: Check if UWP throws here
			throw new InvalidOperationException("No attached flyout on this element");
		}
	}

	private void ForwardPopupFlowDirection()
	{
		if (m_tpPopup is not null)
		{
			var spPopupAsFE = m_tpPopup;
			// Propagate the target flow direction to the Popup
			var flowDirection = m_tpPlacementTarget.FlowDirection;
			spPopupAsFE.FlowDirection = flowDirection;
		}
	}

	private void SetTargetPosition(Point targetPoint)
	{
		m_isTargetPositionSet = true;
		m_targetPoint = targetPoint;
	}

	private void ApplyTargetPosition()
	{
		if (m_isTargetPositionSet && m_tpPopup is not null)
		{
			m_tpPopup.HorizontalOffset = m_targetPoint.X;
			m_tpPopup.VerticalOffset = m_targetPoint.Y;
		}
	}

	private void SetIsWindowedPopup()
	{
		EnsurePopupAndPresenter();

		// Set popup to be windowed, if we can, in order to support rendering the popup out of the XAML window.
		if (Popup.DoesPlatformSupportWindowedPopup(DXamlCore.Current))
		{
			// Set popup to be windowed, if we can, in order to support rendering the popup out of the XAML window.
			if (m_tpPopup != null && !m_tpPopup.IsWindowed())
			{
				Popup popup = m_tpPopup;

				if (!popup.WasEverOpened())
				{
					popup.SetIsWindowed();
					MUX_ASSERT(popup.IsWindowed());
				}
			}
		}
	}

	private bool IsWindowedPopup()
	{
		bool areWindowedPopupsSupported = CPopup::DoesPlatformSupportWindowedPopup(DXamlCore::GetCurrent()->GetHandle());
		bool isPopupWindowed = m_tpPopup ? static_cast<CPopup*>(m_tpPopup.Cast<Popup>()->GetHandle())->IsWindowed() : false;

		return areWindowedPopupsSupported && (isPopupWindowed || m_openingWindowedInProgress);
	}

	//TODO:MZ: Original implementation checks the system accessibility settings
	private bool IsRightHandedHandedness => true;

	private Rect UpdateTargetPosition(
		Rect availableWindowRect,
		Size presenterSize)
	{
		double horizontalOffset = 0.0;
		double verticalOffset = 0.0;
		FrameworkElement spPopupAsFE;
		FlowDirection flowDirection = FlowDirection.LeftToRight;
		FlowDirection targetFlowDirection = FlowDirection.LeftToRight;
		bool isMenuFlyout = this is MenuFlyout;
		bool preferTopPlacement = false;

		MUX_ASSERT(m_tpPopup is not null);
		MUX_ASSERT(m_isTargetPositionSet);

		horizontalOffset = m_targetPoint.X;
		verticalOffset = m_targetPoint.Y;

		FlyoutPlacementMode placementMode = GetEffectivePlacement();

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

		FlyoutBase::MajorPlacementMode majorPlacementMode = preferTopPlacement
			? FlyoutBase::MajorPlacementMode::Top
			: GetMajorPlacementFromPlacement(placementMode);

		IFC_RETURN(m_tpPopup.As(&spPopupAsFE));
		IFC_RETURN(spPopupAsFE->get_FlowDirection(&flowDirection));
		if (m_isPositionedAtPoint)
		{
			IFC_RETURN(m_tpPlacementTarget->get_FlowDirection(&targetFlowDirection));
			ASSERT(flowDirection == targetFlowDirection);
		}

		GetEffectivePlacementMode(
			majorPlacementMode,
			flowDirection,
			&majorPlacementMode);

		FlyoutBase::PreferredJustification justification{};
		
		if (!WinAppSdk::Containment::IsChangeEnabled<WINAPPSDK_CHANGEID_46832968>())
		{
			justification = GetJustificationFromPlacementMode(placementMode);
		}

		FlyoutBase::MajorPlacementMode originalMajorPlacementMode = majorPlacementMode;
		FlyoutBase::PreferredJustification originalJustification{};
		
		if (!WinAppSdk::Containment::IsChangeEnabled<WINAPPSDK_CHANGEID_46832968>())
		{
			originalJustification = justification;
		}

		// If the desired placement of the flyout is inside the exclusion area, we'll shift it in the direction of the placement direction
		// so that it no longer is inside that area.
		auto accountForExclusionRect = [&]()
		{
			if (!RectUtil::AreDisjoint(m_exclusionRect, { static_cast<float>(horizontalOffset), static_cast<float>(verticalOffset), presenterSize.Width, presenterSize.Height }))
			{
				switch (majorPlacementMode)
				{
				case FlyoutBase::MajorPlacementMode::Top:
					verticalOffset = m_exclusionRect.Y - presenterSize.Height;
					break;
				case FlyoutBase::MajorPlacementMode::Bottom:
					verticalOffset = m_exclusionRect.Y + m_exclusionRect.Height;
					break;
				case FlyoutBase::MajorPlacementMode::Left:
					horizontalOffset = m_exclusionRect.X - presenterSize.Width;
					break;
				case FlyoutBase::MajorPlacementMode::Right:
					horizontalOffset = m_exclusionRect.X + m_exclusionRect.Width;
					break;
				}

				// (0, 0) is at the right side in RTL rather than the left side,
				// so we'll add on the presenter width in that case to the offset.
				if ((majorPlacementMode == FlyoutBase::MajorPlacementMode::Left ||
					majorPlacementMode == FlyoutBase::MajorPlacementMode::Right) &&
					flowDirection == xaml::FlowDirection_RightToLeft)
				{
					horizontalOffset += presenterSize.Width;
				}
			}
		};

		accountForExclusionRect();

		bool isRTL = (flowDirection == FlowDirection.RightToLeft);
		bool shiftLeftForRightHandedness = useHandednessPlacement && (IsRightHandedHandedness != isRTL);
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
		var maxWidth = m_tpPresenter.MaxWidth;
		var maxHeight = m_tpPresenter.MaxHeight;

		wf::Point targetPoint = { static_cast<FLOAT>(horizontalOffset), static_cast<FLOAT>(verticalOffset) };
		wf::Rect availableRect = {};

		const bool useMonitorBounds = IsWindowedPopup();

		if (useMonitorBounds)
		{
			// Calculate the available monitor bounds to set the target position within the monitor bounds
			IFC_RETURN(DXamlCore::GetCurrent()->CalculateAvailableMonitorRect(m_tpPopup.Cast<Popup>(), targetPoint, &availableRect));
		}
		else
		{
			availableRect = availableWindowRect;
		}

		// Set the max width and height with the available bounds
		IFC_RETURN(m_tpPresenter.Cast<Control>()->put_MaxWidth(
			DoubleUtil::IsNaN(maxWidth) ? availableRect.Width : DoubleUtil::Min(maxWidth, availableRect.Width)));
		IFC_RETURN(m_tpPresenter.Cast<Control>()->put_MaxHeight(
			DoubleUtil::IsNaN(maxHeight) ? availableRect.Height : DoubleUtil::Min(maxHeight, availableRect.Height)));

		// Adjust the target position if the current target is out of bounds
		if (flowDirection == xaml::FlowDirection_LeftToRight)
		{
			if (targetPoint.X + presenterSize.Width > (availableRect.X + availableRect.Width))
			{
				if (useMonitorBounds)
				{
					// Update the target horizontal position if the target is out of the available bounds.
					// If the presenter width is greater than the current target left point from the screen,
					// the menu target left position is set to the begin of the screen position.
					horizontalOffset -= DoubleUtil::Min(
						presenterSize.Width,
						DoubleUtil::Max(0, targetPoint.X - availableRect.X));
				}
				else
				{
					if (m_isPositionedAtPoint)
					{
						// Update the target horizontal position if the target is out of the available rect
						horizontalOffset -= DoubleUtil::Min(presenterSize.Width, horizontalOffset);
					}
					else
					{
						// Used for date and time picker flyouts
						horizontalOffset = availableRect.X + availableRect.Width - presenterSize.Width;
						horizontalOffset = DoubleUtil::Max(availableRect.X, horizontalOffset);
					}
				}

				// If we were positioned on the right, we're now positioned on the left.
				if (majorPlacementMode == FlyoutBase::MajorPlacementMode::Right)
				{
					majorPlacementMode = FlyoutBase::MajorPlacementMode::Left;
				}

				if (!WinAppSdk::Containment::IsChangeEnabled<WINAPPSDK_CHANGEID_46832968>())
				{
					// If we were justified left, we're now justified right.
					if (justification == FlyoutBase::PreferredJustification::Left)
					{
						justification = FlyoutBase::PreferredJustification::Right;
					}
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
					horizontalOffset += DoubleUtil::Min(
						presenterSize.Width,
						DoubleUtil::Max(0, availableRect.Width - targetPoint.X + availableRect.X));
				}
				else
				{
					// Adjust the target position if the current target is out of the Xaml window bounds
					if (m_isPositionedAtPoint)
					{
						// Update the target horizontal position if the target is out of the available rect
						horizontalOffset += DoubleUtil::Min(presenterSize.Width, (availableRect.Width + availableRect.X - horizontalOffset));
					}
					else
					{
						// Used for date and time picker flyouts
						horizontalOffset = presenterSize.Width + availableRect.X;
						horizontalOffset = DoubleUtil::Min(availableRect.Width + availableRect.X, horizontalOffset);
					}
				}

				// If we were positioned on the left, we're now positioned on the right.
				if (majorPlacementMode == FlyoutBase::MajorPlacementMode::Left)
				{
					majorPlacementMode = FlyoutBase::MajorPlacementMode::Right;
				}
				
				if (!WinAppSdk::Containment::IsChangeEnabled<WINAPPSDK_CHANGEID_46832968>())
				{
					// If we were justified right, we're now justified left.
					if (justification == FlyoutBase::PreferredJustification::Right)
					{
						justification = FlyoutBase::PreferredJustification::Left;
					}
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
				if (majorPlacementMode == FlyoutBase::MajorPlacementMode::Left)
				{
					majorPlacementMode = FlyoutBase::MajorPlacementMode::Right;
				}

				if (!WinAppSdk::Containment::IsChangeEnabled<WINAPPSDK_CHANGEID_46832968>())
				{
					// If we were justified right, we're now justified left.
					if (justification == FlyoutBase::PreferredJustification::Right)
					{
						justification = FlyoutBase::PreferredJustification::Left;
					}
				}
			}
			else if (isRTL && targetPoint.X + presenterSize.Width >= availableRect.Width)
			{
				horizontalOffset -= presenterSize.Width;
				targetPoint.X -= presenterSize.Width;

				// If we were positioned on the right, we're now positioned on the left.
				if (majorPlacementMode == FlyoutBase::MajorPlacementMode::Right)
				{
					majorPlacementMode = FlyoutBase::MajorPlacementMode::Left;
				}

				if (!WinAppSdk::Containment::IsChangeEnabled<WINAPPSDK_CHANGEID_46832968>())
				{
					// If we were justified left, we're now justified right.
					if (justification == FlyoutBase::PreferredJustification::Left)
					{
						justification = FlyoutBase::PreferredJustification::Right;
					}
				}
			}
		}

		// If opening up would cause the flyout to get clipped, we fall back to opening down
		if (preferTopPlacement && targetPoint.Y < availableRect.Y)
		{
			verticalOffset += presenterSize.Height;
			targetPoint.Y += presenterSize.Height;

			// If we were positioned on the top, we're now positioned on the bottom.
			if (majorPlacementMode == FlyoutBase::MajorPlacementMode::Top)
			{
				majorPlacementMode = FlyoutBase::MajorPlacementMode::Bottom;
			}

			if (!WinAppSdk::Containment::IsChangeEnabled<WINAPPSDK_CHANGEID_46832968>())
			{
				// If we were justified bottom, we're now justified top.
				if (justification == FlyoutBase::PreferredJustification::Bottom)
				{
					justification = FlyoutBase::PreferredJustification::Top;
				}
			}
		}

		if (targetPoint.Y + presenterSize.Height > availableRect.Y + availableRect.Height)
		{
			if (useMonitorBounds)
			{
				// Update the target vertical position if the target is out of the available monitor.
				// If the presenter height is greater than the current target top point from the screen,
				// the menu target top position is set to the begin of the screen position.
				if (verticalOffset > availableRect.Y)
				{
					verticalOffset = verticalOffset - DoubleUtil::Min(
						presenterSize.Height,
						DoubleUtil::Max(0, targetPoint.Y - availableRect.Y));
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
					verticalOffset -= DoubleUtil::Min(presenterSize.Height, verticalOffset);
				}
				else
				{
					verticalOffset = availableRect.Y + availableRect.Height - presenterSize.Height;
				}
			}

			// If we were positioned on the bottom, we're now positioned on the top.
			if (majorPlacementMode == FlyoutBase::MajorPlacementMode::Bottom)
			{
				majorPlacementMode = FlyoutBase::MajorPlacementMode::Top;
			}

			if (!WinAppSdk::Containment::IsChangeEnabled<WINAPPSDK_CHANGEID_46832968>())
			{
				// If we were justified top, we're now justified bottom.
				if (justification == FlyoutBase::PreferredJustification::Top)
				{
					justification = FlyoutBase::PreferredJustification::Bottom;
				}
			}
		}

		if (!useMonitorBounds)
		{
			verticalOffset = DoubleUtil::Max(availableRect.Y, verticalOffset);
		}

		// The above accounting may have shifted our position, so we'll account for the exclusion rect again.
		accountForExclusionRect();

		if (WinAppSdk::Containment::IsChangeEnabled<WINAPPSDK_CHANGEID_46832968>())
		{
			// Accounting for the exclusion rect is important, but keeping the popup fully in view is more important, if possible.
			// If we are still not completely in view at this point, we'll fall back to our original positioning and then
			// nudge the popup into full view, as long as it will fit on screen.
			const bool presenterIsOffScreenHorizontally = isRTL ?
				(horizontalOffset - presenterSize.Width < availableRect.X ||
					horizontalOffset > availableRect.X + availableRect.Width) :
				(horizontalOffset < availableRect.X ||
					horizontalOffset + presenterSize.Width > availableRect.X + availableRect.Width);

			const bool presenterIsOffScreenVertically =
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
		}
		else
		{
			// Accounting for the exclusion rect is important, but keeping the popup fully in view is more important, if possible.
			// If we switched sides but are still not completely in view at this point, then we'll give up, revert back to the
			// original placement mode and justification, and put the popup as far in that direction as possible while still remaining in view.
			if (originalMajorPlacementMode != majorPlacementMode ||
				originalJustification != justification)
			{
				if (presenterSize.Width <= availableRect.Width &&
					(horizontalOffset < availableRect.X ||
						horizontalOffset + presenterSize.Width > availableRect.X + availableRect.Width))
				{
					if (originalMajorPlacementMode == FlyoutBase::MajorPlacementMode::Left ||
						originalJustification == FlyoutBase::PreferredJustification::Right)
					{
						horizontalOffset = availableRect.X;
					}
					else
					{
						horizontalOffset = availableRect.X + availableRect.Width - presenterSize.Width;
					}
				}

				if (presenterSize.Height <= availableRect.Height &&
					(verticalOffset < availableRect.Y ||
						verticalOffset + presenterSize.Height > availableRect.Y + availableRect.Height))
				{
					if (originalMajorPlacementMode == FlyoutBase::MajorPlacementMode::Top ||
						originalJustification == FlyoutBase::PreferredJustification::Bottom)
					{
						verticalOffset = availableRect.Y;
					}
					else
					{
						verticalOffset = availableRect.Y + availableRect.Height - presenterSize.Height;
					}
				}
			}
		}

		IFC_RETURN(m_tpPopup->put_HorizontalOffset(horizontalOffset));
		IFC_RETURN(m_tpPopup->put_VerticalOffset(verticalOffset));

		double leftMostEdge = (flowDirection == FlowDirection.LeftToRight) ? horizontalOffset : horizontalOffset - presenterSize.Width;

		var presenterRect = new Rect();
		presenterRect.X = leftMostEdge;
		presenterRect.Y = verticalOffset;
		presenterRect.Width = presenterSize.Width;
		presenterRect.Height = presenterSize.Height;
		return presenterRect;
	}

	private FlyoutMetadata GetFlyoutMetadata()
	{
		if (m_tpParentFlyout is not null)
		{
			return m_tpParentFlyout.m_childFlyoutMetadata;
		}
		else
		{
			return DXamlCore.Current.FlyoutMetadata;
		}
	}

	private static bool GetIsAncestorOfTraverseThroughPopups(
		UIElement? pParentElem,
		UIElement? pMaybeDescendant)
	{
		if (pParentElem == null)
		{
			return false;
		}

		while ((pMaybeDescendant != null) && (pMaybeDescendant != pParentElem))
		{
			pMaybeDescendant = pMaybeDescendant.GetUIElementAdjustedParentInternal(false);
		}

		return pParentElem == pMaybeDescendant;
	}

	private FlyoutBase FindParentFlyoutFromElement(FrameworkElement element)
	{
		FlyoutBase? parentFlyout = null;

		// If the element is a CommandBar element, then we'll find the parent CommandBar first -
		// this element might be in the CommandBar's popup and not be a visual child of the flyout
		// that the CommandBar is a child of.
		FrameworkElement elementLocal = element;
		ICommandBarElement elementAsCommandBarElement = elementLocal as ICommandBarElement;

		if (elementAsCommandBarElement is not null)
		{
			var parentCommandBar = CommandBar.FindParentCommandBarForElement(elementAsCommandBarElement);

			if (parentCommandBar is not null)
			{
				elementLocal = parentCommandBar;
			}
		}

		var visualTree = VisualTree.GetForElement(elementLocal);

		var popups = VisualTreeHelper.GetOpenPopups(visualTree);

		int numPopups = popups.Count;

		// Iterate over the open popups, and for the ones that are associated with a flyout
		// check to see if the given element is a descendent of that flyout's presenter.
		for (int i = 0; i < numPopups; ++i)
		{
			Popup popup = popups[i];

			FlyoutBase flyout = popup.AssociatedFlyout;
			if (flyout is not null)
			{
				// If the flyout is open, it should have a presenter.
				MUX_ASSERT(flyout.m_tpPresenter is not null);

				var pUIElemPopup = popup;
				var pUIElemLocal = elementLocal;
				const bool isAncestorOf = GetIsAncestorOfTraverseThroughPopups(pUIElemPopup, pUIElemLocal);

				if (isAncestorOf || popup == elementLocal)
				{
					parentFlyout = flyout;
					break;
				}
			}
		}
	}


	// Callback for ShowAt() from core layer
	private static void ShowAtStatic(
		FlyoutBase flyout,
		FrameworkElement target)
	{
		MUX_ASSERT(flyout is not null);
		MUX_ASSERT(target is not null);

		flyout.ShowAt(target);
	}

	// Callback for ShowAt() from core layer
	private static void ShowAtStatic(
		FlyoutBase flyout,
		FrameworkElement target,
		Point point,
		Rect exclusionRect,
		FlyoutShowMode flyoutShowMode)
	{
		MUX_ASSERT(flyout is not null);
		MUX_ASSERT(target is not null);

		var showOptions = new FlyoutShowOptions();
		showOptions.Position = point;
		showOptions.ExclusionRect = exclusionRect;
		showOptions.ShowMode = flyoutShowMode;

		flyout!.ShowAtWithOptions(target, showOptions);
	}

	//TODO:MZ: Probably property
	private bool IsLightDismissOverlayEnabled
	{
		get => m_isLightDismissOverlayEnabled;
		set => m_isLightDismissOverlayEnabled = value;
	}

	/// <summary>
	/// Gets a value that indicates whether the flyout is shown within the bounds of the XAML root.
	/// </summary>
	public bool IsConstrainedToRootBounds
	{
		get
		{
			EnsurePopupAndPresenter();
			return m_tpPopup.IsConstrainedToRootBounds;
		}
	}

	private static void OnClosingStatic(FlyoutBase obj, out bool cancel)
	{
		cancel = false;
		if (obj is not null)
		{
			obj.OnClosing(out cancel);
		}
	}

	private void ConfigurePopupOverlay()
	{
		MUX_ASSERT(m_tpPopup is not null);

		bool isOverlayVisible = LightDismissOverlayHelper.ResolveIsOverlayVisibleForControl(this);

		bool isLightDismissEnabled = m_tpPopup.IsLightDismissEnabled;

		// Some modes of flyout configure their popup as not light-dismissible;
		// for those cases don't allow the overlay to be visible.
		isOverlayVisible &= isLightDismissEnabled;

		if (isOverlayVisible)
		{
			m_tpPopup.LightDismissOverlayMode = LightDismissOverlayMode.On;

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
				DependencyObject templatedParent;
				if (m_tpPlacementTarget is not null)
				{
					templatedParent = m_tpPlacementTarget.TemplatedParent;
				}

				if (templatedParent is not null && templatedParent is CalendarDatePicker)
				{
					themeBrush = "CalendarDatePickerLightDismissOverlayBackground";
				}
				else
				{
					themeBrush = "FlyoutLightDismissOverlayBackground";
				}
			}

			// Hook up our brush to the popup's overlay using theme resources.
			m_tpPopup.SetOverlayThemeBrush(themeBrush);
		}
		else
		{
			m_tpPopup.LightDismissOverlayMode(LightDismissOverlayMode.Off);
		}
	}

	/// <summary>
	/// Attempts to invoke a keyboard shortcut (accelerator).
	/// </summary>
	public void TryInvokeKeyboardAccelerator(ProcessKeyboardAcceleratorEventArgs args)
	{
		//TODO:MZ: Call to protected?
	}

	/// <summary>
	/// Called just before a keyboard shortcut (accelerator) is processed in your app. Invoked whenever
	/// application code or internal processes call ProcessKeyboardAccelerators. Override this method
	/// to influence the default accelerator handling.
	/// </summary>
	/// <param name="args">The ProcessKeyboardAcceleratorEventArgs.</param>
	protected virtual void OnProcessKeyboardAccelerators(ProcessKeyboardAcceleratorEventArgs args)
	{
	}

	private Size GetPresenterSize()
	{
		var actualWidth = m_tpPresenter.ActualWidth;
		var actualHeight = m_tpPresenter.ActualHeight;

		var size = new Size(actualWidth, actualHeight);
		return size;
	}

	private void UpdateStateToShowMode(FlyoutShowMode showMode)
	{
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
				MUX_ASSERT(false, "Unsupported FlyoutShowMode");
				break;
		}

		if (m_tpPopup is not null)
		{
			bool isOpen = m_tpPopup.IsOpen;
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

	private FlyoutPlacementMode GetEffectivePlacement()
	{
		if (m_hasPlacementOverride)
		{
			return m_placementOverride;
		}
		else
		{
			return Placement;
		}
	}

	_Check_return_ HRESULT FlyoutBase::IsOpen(_In_ CFlyoutBase* flyoutBase, _Out_ bool& isOpen)
	{
		DBG_FLYOUT_TRACE(L">>> FlyoutBase::IsOpen()");

		ASSERT(flyoutBase);

		ctl::ComPtr<DependencyObject> flyoutDO;
		IFC_RETURN(DXamlCore::GetCurrent()->GetPeer(flyoutBase, &flyoutDO));

		BOOLEAN isOpenImpl = false;
		IFC_RETURN(flyoutDO.Cast<FlyoutBase>()->get_IsOpenImpl(&isOpenImpl));

		isOpen = !!isOpenImpl;
		return S_OK;
	}

	private bool IsOpenImpl()
	{
		if (m_tpPopup is not null)
		{
			return m_tpPopup.IsOpen;
		}

		return false;
	}

	/// <summary>
	/// Gets or sets the XamlRoot in which this flyout is being viewed.
	/// </summary>
	public XamlRoot? XamlRoot
	{
		get => Windows.UI.Xaml.XamlRoot.GetForElement(this);
		set
		{
			var xamlRoot = Windows.UI.Xaml.XamlRoot.GetForElement(this).Get();
			if (pValue == xamlRoot)
			{
				return S_OK;
			}

			if (xamlRoot != null)
			{
				DirectUI.ErrorHelper.OriginateErrorUsingResourceID(E_UNEXPECTED, ERROR_CANNOT_SET_XAMLROOT_WHEN_NOT_NULL));
			}

			XamlRoot.SetForElementStatic(this, pValue);
		}
	}

	//// When it was initially added, the Target property had no dependency property backing it.
	//// In a later version, we added a dependency property, which requires us to separate the
	//// associated dependency property from the property itself in code-gen.  This, in turn,
	//// requires us to manually define the getter for the dependency property.
	//_Check_return_ HRESULT FlyoutBaseFactory.get_TargetPropertyImpl(_Outptr_result_maybenull_ IDependencyProperty** ppValue)
	//{
	//	RRETURN(MetadataAPI.GetIDependencyProperty(KnownPropertyIndex.FlyoutBase_Target, ppValue));
	//}
}
