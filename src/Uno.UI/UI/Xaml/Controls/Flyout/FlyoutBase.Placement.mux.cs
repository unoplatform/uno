// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference dxaml/xcp/dxaml/lib/FlyoutBase_partial.cpp, commit b9fa30e549de859f15fb84f153cc6136b981d10a

#nullable enable

using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class FlyoutBase
{
	// Return true if placement is along vertical axis, false otherwise.
	private static bool IsPlacementModeVertical(MajorPlacementMode placementMode)
	{
		// We are safe even if placementMode is Full. because the case for placementMode is Full has already been put in another if branch in function PerformPlacement.
		// if necessary, we can add another function : IsPlacementModeHorizontal
		return placementMode == MajorPlacementMode.Top || placementMode == MajorPlacementMode.Bottom;
	}

	// Calculates space between container boundary and placement target for a given placement mode.
	private static double CalculateAvailableSpace(
		MajorPlacementMode placement,
		Rect placementTargetBounds,
		Rect containerRect)
	{
		var availableSpace = 0.0;

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
		}

		availableSpace = Math.Max(0.0, availableSpace);

		return availableSpace;
	}

	// Update placement mode and calculate available space for the best placement mode
	// along the same axis as specified by placement.
	private static void SelectSideWithMoreSpace(
		ref MajorPlacementMode placement,
		Rect placementTargetBounds,
		Rect containerRect,
		out double availableSpace)
	{
		availableSpace = 0.0;

		if (IsPlacementModeVertical(placement))
		{
			var availableSpaceTop = CalculateAvailableSpace(MajorPlacementMode.Top, placementTargetBounds, containerRect);
			var availableSpaceBottom = CalculateAvailableSpace(MajorPlacementMode.Bottom, placementTargetBounds, containerRect);

			if (availableSpaceTop > availableSpaceBottom)
			{
				placement = MajorPlacementMode.Top;
				availableSpace = availableSpaceTop;
			}
			else if (availableSpaceTop < availableSpaceBottom)
			{
				placement = MajorPlacementMode.Bottom;
				availableSpace = availableSpaceBottom;
			}
			else
			{
				availableSpace = availableSpaceTop;
			}
		}
		else
		{
			var availableSpaceLeft = CalculateAvailableSpace(MajorPlacementMode.Left, placementTargetBounds, containerRect);
			var availableSpaceRight = CalculateAvailableSpace(MajorPlacementMode.Right, placementTargetBounds, containerRect);

			if (availableSpaceLeft > availableSpaceRight)
			{
				placement = MajorPlacementMode.Left;
				availableSpace = availableSpaceLeft;
			}
			else if (availableSpaceLeft < availableSpaceRight)
			{
				placement = MajorPlacementMode.Right;
				availableSpace = availableSpaceRight;
			}
			else
			{
				availableSpace = availableSpaceLeft;
			}
		}
	}

	// Size and position control to fill available space constrained by container rectangle.
	// Uno: the margin to the target is applied here, where WinUI applies FlyoutMargin once the placement is calculated.
	internal static void ResizeToFit(
		ref MajorPlacementMode placement,
		Rect placementTargetBounds,
		Rect containerRect,
		Size minControlSize,
		double targetMargin,
		ref Point controlPos,
		ref Size controlSize)
	{
		var availableSpace = CalculateAvailableSpace(placement, placementTargetBounds, containerRect);

		// For primary axis, if exceeding available size on side specified by placement
		// find side with more space and adjust position and size to be next to target.
		// For the secondary axis, if exceeding container size, set it to fill all available space.

		if (IsPlacementModeVertical(placement))
		{
			if (controlSize.Height > availableSpace)
			{
				var moreSpacePlacement = placement;
				SelectSideWithMoreSpace(ref moreSpacePlacement, placementTargetBounds, containerRect, out var moreAvailableSpace);

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
				if (placement == MajorPlacementMode.Top)
				{
					controlPos.Y = placementTargetBounds.Y - targetMargin - controlSize.Height;
				}
				else
				{
					controlPos.Y = placementTargetBounds.Y + placementTargetBounds.Height + targetMargin;
				}
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
				var moreSpacePlacement = placement;
				SelectSideWithMoreSpace(ref moreSpacePlacement, placementTargetBounds, containerRect, out var moreAvailableSpace);

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
				if (placement == MajorPlacementMode.Left)
				{
					controlPos.X = placementTargetBounds.X - targetMargin - controlSize.Width;
				}
				else
				{
					controlPos.X = placementTargetBounds.X + placementTargetBounds.Width + targetMargin;
				}
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
		else if ((controlPos.X + controlSize.Width) > (containerRect.X + containerRect.Width))
		{
			controlPos.X = (containerRect.X + containerRect.Width) - controlSize.Width;
		}

		if (controlPos.Y < containerRect.Y)
		{
			controlPos.Y = containerRect.Y;
		}
		else if ((controlPos.Y + controlSize.Height) > (containerRect.Y + containerRect.Height))
		{
			controlPos.Y = (containerRect.Y + containerRect.Height) - controlSize.Height;
		}
	}
}
