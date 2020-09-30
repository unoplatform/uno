// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ProximityStrategy.h, ProximityStrategy.cpp

#nullable enable

using Windows.Foundation;
using Windows.UI.Xaml.Input;
using static Uno.UI.Xaml.Input.XYFocusAlgorithmHelper;

namespace Uno.UI.Xaml.Input
{
	internal static class ProximityStrategy
	{
		internal static double GetScore(
			FocusNavigationDirection direction,
			Rect bounds,
			Rect candidateBounds,
			double maxDistance,
			bool considerSecondaryAxis)
		{
			double score = 0;

			double primaryAxisDistance = CalculatePrimaryAxisDistance(direction, bounds, candidateBounds);
			double secondaryAxisDistance = CalculateSecondaryAxisDistance(direction, bounds, candidateBounds);

			if (primaryAxisDistance >= 0)
			{
				// We do not want to use the secondary axis if the candidate is within the shadow of the element
				(double first, double second) potential;
				(double first, double second) reference;

				if (IsLeft(direction) || IsRight(direction))
				{
					reference.first = bounds.Top;
					reference.second = bounds.Bottom;

					potential.first = candidateBounds.Top;
					potential.second = candidateBounds.Bottom;
				}
				else
				{
					reference.first = bounds.Left;
					reference.second = bounds.Right;

					potential.first = candidateBounds.Left;
					potential.second = candidateBounds.Right;
				}

				if (considerSecondaryAxis == false || CalculatePercentInShadow(reference, potential) != 0)
				{
					secondaryAxisDistance = 0;
				}

				score = maxDistance - (primaryAxisDistance + secondaryAxisDistance);
			}

			return score;
		}
	}
}
