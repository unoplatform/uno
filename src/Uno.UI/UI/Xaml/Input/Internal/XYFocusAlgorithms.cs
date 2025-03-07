// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// XYFocusAlgorithms.h, XYFocusAlgorithms.cpp

#nullable enable

using System;
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using static Uno.UI.Xaml.Input.XYFocusAlgorithmHelper;
using static Uno.Helpers.MathHelpers;

namespace Uno.UI.Xaml.Input
{
	internal class XYFocusAlgorithms
	{
		private const double InShadowThreshold = 0.25;
		private const double InShadowThresholdForSecondaryAxis = 0.02;
		private const double ConeAngle = Math.PI / 4;

		private int _primaryAxisDistanceWeight;
		private int _secondaryAxisDistanceWeight;
		private int _percentInManifoldShadowWeight;
		private int _percentInShadowWeight;

		internal XYFocusAlgorithms()
		{
			_primaryAxisDistanceWeight = 15;
			_secondaryAxisDistanceWeight = 1;
			_percentInManifoldShadowWeight = 10000;
			_percentInShadowWeight = 50;
		}

		internal double GetScore(
			FocusNavigationDirection direction,
			Rect bounds,
			Rect candidateBounds,
			(double first, double second) hManifold,
			(double first, double second) vManifold,
			double maxDistance)
		{
			double score = 0;
			double primaryAxisDistance = maxDistance;
			double secondaryAxisDistance = maxDistance;
			double percentInManifoldShadow = 0;
			double percentInShadow = 0;

			(double first, double second) potential;
			(double first, double second) reference;
			(double first, double second) currentManifold;

			if (IsLeft(direction) || IsRight(direction))
			{
				reference.first = bounds.Top;
				reference.second = bounds.Bottom;

				currentManifold = hManifold;

				potential.first = candidateBounds.Top;
				potential.second = candidateBounds.Bottom;
			}
			else
			{
				reference.first = bounds.Left;
				reference.second = bounds.Right;

				currentManifold = vManifold;

				potential.first = candidateBounds.Left;
				potential.second = candidateBounds.Right;
			}

			primaryAxisDistance = CalculatePrimaryAxisDistance(direction, bounds, candidateBounds);
			secondaryAxisDistance = CalculateSecondaryAxisDistance(direction, bounds, candidateBounds);

			if (primaryAxisDistance >= 0)
			{
				percentInShadow = CalculatePercentInShadow(reference, potential);

				if (percentInShadow >= InShadowThresholdForSecondaryAxis)
				{
					percentInManifoldShadow = CalculatePercentInShadow(currentManifold, potential);
					secondaryAxisDistance = maxDistance;
				}

				// The score needs to be a positive number so we make these distances positive numbers
				primaryAxisDistance = maxDistance - primaryAxisDistance;
				secondaryAxisDistance = maxDistance - secondaryAxisDistance;

				if (percentInShadow >= InShadowThreshold)
				{
					percentInShadow = 1;
					primaryAxisDistance = primaryAxisDistance * 2;
				}

				// Potential elements in the shadow get a multiplier to their final score
				score = CalculateScore(percentInShadow, primaryAxisDistance, secondaryAxisDistance, percentInManifoldShadow);
			}

			return score;
		}

		internal static void UpdateManifolds(
			FocusNavigationDirection direction,
			Rect bounds,
			Rect newFocusBounds,
			ref (double first, double second) hManifold,
			ref (double first, double second) vManifold)
		{
			if (vManifold.second < 0)
			{
				vManifold = (bounds.Left, bounds.Right);
			}

			if (hManifold.second < 0)
			{
				hManifold = (bounds.Top, bounds.Bottom);
			}

			if (IsLeft(direction) || IsRight(direction))
			{
				hManifold.first = Math.Max(Math.Max((float)newFocusBounds.Top, (float)bounds.Top), (float)hManifold.first);
				hManifold.second = Math.Min(Math.Min((float)newFocusBounds.Bottom, (float)bounds.Bottom), (float)hManifold.second);

				// It's possible to get into a situation where the newFocusedElement to the right / left has no overlap with the current edge.
				if (hManifold.second <= hManifold.first)
				{
					hManifold.first = newFocusBounds.Top;
					hManifold.second = newFocusBounds.Bottom;
				}

				vManifold.first = newFocusBounds.Left;
				vManifold.second = newFocusBounds.Right;
			}
			else if (IsUp(direction) || IsDown(direction))
			{
				vManifold.first = Math.Max(Math.Max((float)newFocusBounds.Left, (float)bounds.Left), (float)vManifold.first);
				vManifold.second = Math.Min(Math.Min((float)newFocusBounds.Right, (float)bounds.Right), (float)vManifold.second);

				// It's possible to get into a situation where the newFocusedElement to the right / left has no overlap with the current edge.
				if (vManifold.second <= vManifold.first)
				{
					vManifold.first = newFocusBounds.Left;
					vManifold.second = newFocusBounds.Right;
				}

				hManifold.first = newFocusBounds.Top;
				hManifold.second = newFocusBounds.Bottom;
			}
		}

		private double CalculateScore(
				double percentInShadow,
				double primaryAxisDistance,
				double secondaryAxisDistance,
				double percentInManifoldShadow)
		{
			double score = (percentInShadow * _percentInShadowWeight) +
				(primaryAxisDistance * _primaryAxisDistanceWeight) +
				(secondaryAxisDistance * _secondaryAxisDistanceWeight) +
				(percentInManifoldShadow * _percentInManifoldShadowWeight);

			return score;
		}

		internal static bool ShouldCandidateBeConsideredForRanking(
			Rect bounds,
			Rect candidateBounds,
			double maxDistance,
			FocusNavigationDirection direction,
			Rect exclusionRect,
			bool ignoreCone = false)
		{
			//Consider a candidate only if:
			// 1. It doesn't have an empty rect as it's bounds
			// 2. It doesn't contain currently focused element
			// 3. It's bounds don't intersect with the rect we were asked to avoid looking into (Exclusion Rect)
			// 4. It's bounds aren't contained in the rect we were asked to avoid looking into (Exclusion Rect)
			if (IsEmptyRect(candidateBounds) ||
				DoesRectContainRect(candidateBounds, bounds) ||
				DoRectsIntersect(exclusionRect, candidateBounds) ||
				DoesRectContainRect(exclusionRect, candidateBounds))
			{
				return false;
			}

			//We've decided to disable the use of the cone for vertical navigation.
			if (ignoreCone || IsDown(direction) || IsUp(direction)) { return true; }

			Point originTop = Point.Zero;
			Point originBottom = Point.Zero;
			originTop.Y = bounds.Top;
			originBottom.Y = bounds.Bottom;

			var candidateAsPoints = RectToPoints(candidateBounds);

			//We make the maxDistance twice the normal distance to ensure that all the elements are encapsulated inside the cone. This
			//also aids in scenarios where the original max distance is still less than one of the points (due to the angles)
			maxDistance = maxDistance * 2;

			var cone = new Point[4];
			//Note: our y axis is inverted
			if (IsLeft(direction))
			{
				// We want to start the origin one pixel to the left to cover overlapping scenarios where the end of a candidate element 
				// could be overlapping with the origin (before the shift)
				originTop.X = bounds.Left - 1;
				originBottom.X = bounds.Left - 1;

				var sides = new Point[2];

				//We have two angles. Find a point (for each angle) on the line and rotate based on the direction
				const double rotation = Math.PI; //180 degrees
				sides[0].X = (float)(originTop.X + maxDistance * Math.Cos(rotation + ConeAngle));
				sides[0].Y = (float)(originTop.Y + maxDistance * Math.Sin(rotation + ConeAngle));
				sides[1].X = (float)(originBottom.X + maxDistance * Math.Cos(rotation - ConeAngle));
				sides[1].Y = (float)(originBottom.Y + maxDistance * Math.Sin(rotation - ConeAngle));

				// order points in counter clockwise direction
				cone[0] = originTop;
				cone[1] = sides[0];
				cone[2] = sides[1];
				cone[3] = originBottom;
			}
			else if (IsRight(direction))
			{
				// We want to start the origin one pixel to the right to cover overlapping scenarios where the end of a candidate element 
				// could be overlapping with the origin (before the shift)
				originTop.X = bounds.Right + 1;
				originBottom.X = bounds.Right + 1;

				var sides = new Point[2];

				//We have two angles. Find a point (for each angle) on the line and rotate based on the direction
				const double rotation = 0;
				sides[0].X = (float)(originTop.X + maxDistance * Math.Cos(rotation + ConeAngle));
				sides[0].Y = (float)(originTop.Y + maxDistance * Math.Sin(rotation + ConeAngle));
				sides[1].X = (float)(originBottom.X + maxDistance * Math.Cos(rotation - ConeAngle));
				sides[1].Y = (float)(originBottom.Y + maxDistance * Math.Sin(rotation - ConeAngle));

				// order points in counter clockwise direction
				cone[0] = originBottom;
				cone[1] = sides[0];
				cone[2] = sides[1];
				cone[3] = originTop;
			}

			//There are three scenarios we should check that will allow us to know whether we should consider the candidate element.
			//1) The candidate element and the vision cone intersect
			//2) The candidate element is completely inside the vision cone
			//3) The vision cone is completely inside the bounds of the candidate element (unlikely)

			return DoPolygonsIntersect(cone, candidateAsPoints) || IsEntirelyContained(candidateAsPoints, cone)
				|| IsEntirelyContained(cone, candidateAsPoints);
		}

		internal void SetPrimaryAxisDistanceWeight(int primaryAxisDistanceWeight) =>
			_primaryAxisDistanceWeight = primaryAxisDistanceWeight;

		internal void SetSecondaryAxisDistanceWeight(int secondaryAxisDistanceWeight) =>
			_secondaryAxisDistanceWeight = secondaryAxisDistanceWeight;

		internal void SetPercentInManifoldShadowWeight(int percentInManifoldShadowWeight) =>
			_percentInManifoldShadowWeight = percentInManifoldShadowWeight;

		internal void SetPercentInShadowWeight(int percentInShadowWeight) =>
			_percentInShadowWeight = percentInShadowWeight;
	}
}
