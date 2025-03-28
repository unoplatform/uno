// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// AlgorithmHelper.h, AlgorithmHelper.cpp

using System;
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using static Uno.Helpers.MathHelpers;

namespace Uno.UI.Xaml.Input
{
	internal static class XYFocusAlgorithmHelper
	{
		/// <summary>
		/// Calculates the distance between bounds on primary axis based on a given navigation direction.
		/// </summary>
		/// <param name="direction">Navigation direction.</param>
		/// <param name="bounds">Bounds.</param>
		/// <param name="candidateBounds">Candidate bounds.</param>
		/// <returns>Distance.</returns>
		internal static double CalculatePrimaryAxisDistance(
			FocusNavigationDirection direction,
			Rect bounds,
			Rect candidateBounds)
		{
			double primaryAxisDistance = -1;
			bool isOverlapping = DoRectsIntersect(bounds, candidateBounds);

			if (bounds == candidateBounds)
			{
				//We shouldn't be calculating the distance from ourselves
				return -1;
			}

			if (
				IsLeft(direction) &&
				(candidateBounds.Right <= bounds.Left || (isOverlapping && candidateBounds.Left <= bounds.Left)))
			{
				primaryAxisDistance = Math.Abs(bounds.Left - candidateBounds.Right);
			}
			else if (
				IsRight(direction) &&
				(candidateBounds.Left >= bounds.Right || (isOverlapping && candidateBounds.Right >= bounds.Right)))
			{
				primaryAxisDistance = Math.Abs(candidateBounds.Left - bounds.Right);
			}
			else if (
				IsUp(direction) &&
				(candidateBounds.Bottom <= bounds.Top || (isOverlapping && candidateBounds.Top <= bounds.Top)))
			{
				primaryAxisDistance = Math.Abs(bounds.Top - candidateBounds.Bottom);
			}
			else if (
				IsDown(direction) &&
				(candidateBounds.Top >= bounds.Bottom || (isOverlapping && candidateBounds.Bottom >= bounds.Bottom)))
			{
				primaryAxisDistance = Math.Abs(candidateBounds.Top - bounds.Bottom);
			}

			return primaryAxisDistance;
		}

		/// <summary>
		/// Calculates the distance between bounds on secondary axis based on a given navigation direction.
		/// </summary>
		/// <param name="direction">Navigation direction.</param>
		/// <param name="bounds">Bounds.</param>
		/// <param name="candidateBounds">Candidate bounds.</param>
		/// <returns>Distance.</returns>
		internal static double CalculateSecondaryAxisDistance(
			FocusNavigationDirection direction,
			Rect bounds,
			Rect candidateBounds)
		{
			double secondaryAxisDistance;

			if (IsLeft(direction) || IsRight(direction))
			{
				// Calculate secondary axis distance for the case where the element is not in the shadow
				secondaryAxisDistance = candidateBounds.Top < bounds.Top ? Math.Abs(bounds.Top - candidateBounds.Bottom) : Math.Abs(candidateBounds.Top - bounds.Bottom);
			}
			else
			{
				// Calculate secondary axis distance for the case where the element is not in the shadow
				secondaryAxisDistance = candidateBounds.Left < bounds.Left ? Math.Abs(bounds.Left - candidateBounds.Right) : Math.Abs(candidateBounds.Left - bounds.Right);
			}

			return secondaryAxisDistance;
		}

		/// <summary>
		/// Calculates the percentage of the potential element that is in the shadow of the reference element.
		/// </summary>
		/// <param name="referenceManifold">Reference manifold.</param>
		/// <param name="potentialManifold">Potential manifold.</param>
		/// <returns>Percentage.</returns>
		internal static double CalculatePercentInShadow(
			(double first, double second) referenceManifold,
			(double first, double second) potentialManifold)
		{
			if ((referenceManifold.first > potentialManifold.second) || (referenceManifold.second <= potentialManifold.first))
			{
				// Potential is not in the reference's shadow.
				return 0;
			}

			double shadow = Math.Min(referenceManifold.second, potentialManifold.second) - Math.Max(referenceManifold.first, potentialManifold.first);
			shadow = Math.Abs(shadow);

			double potentialEdgeLength = Math.Abs(potentialManifold.second - potentialManifold.first);
			double referenceEdgeLength = Math.Abs(referenceManifold.second - referenceManifold.first);

			double comparisonEdgeLength = referenceEdgeLength;

			if (comparisonEdgeLength >= potentialEdgeLength)
			{
				comparisonEdgeLength = potentialEdgeLength;
			}

			double percentInShadow = 1;

			if (comparisonEdgeLength != 0)
			{
				percentInShadow = Math.Min(shadow / comparisonEdgeLength, 1.0);
			}

			return percentInShadow;
		}

		/// <summary>
		/// Checks if the navigation direciton is left.
		/// </summary>
		/// <param name="direction">Direction.</param>
		/// <returns>Ture if left.</returns>
		internal static bool IsLeft(FocusNavigationDirection direction) => direction == FocusNavigationDirection.Left;

		/// <summary>
		/// Checks if the navigation direciton is right.
		/// </summary>
		/// <param name="direction">Direction.</param>
		/// <returns>Ture if right.</returns>
		internal static bool IsRight(FocusNavigationDirection direction) => direction == FocusNavigationDirection.Right;

		/// <summary>
		/// Checks if the navigation direciton is up.
		/// </summary>
		/// <param name="direction">Direction.</param>
		/// <returns>Ture if up.</returns>
		internal static bool IsUp(FocusNavigationDirection direction) => direction == FocusNavigationDirection.Up;

		/// <summary>
		/// Checks if the navigation direciton is down.
		/// </summary>
		/// <param name="direction">Direction.</param>
		/// <returns>Ture if down.</returns>
		internal static bool IsDown(FocusNavigationDirection direction) => direction == FocusNavigationDirection.Down;
	}
}
