// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ProximityStrategyUnitTests.h, ProximityStrategyUnitTests.cpp

#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.UI.Xaml.Input;

namespace Uno.UI.Tests.Windows_UI_Xaml.Input.Internal
{
	[TestClass]
	public class Given_XYFocusProximityStrategy
	{
		/*
            +---------+
            |         |
            |         |
            |         |
            |         |
            |         |
            |         |
            +---------+
                                 +------+
                                 |  A   |
                                 +------+
             +-------+
             |       |
             |   B   |
             +-------+
        
        */

		[TestMethod]
		public void VerifyProximityStrategyClosestToAxis()
		{
			Rect focusedElement = new Rect(new Point(100, 100), new Point(200, 200));

			Rect candidateElementA = new Rect(new Point(250, 200), new Point(300, 300));
			Rect candidateElementB = new Rect(new Point(100, 310), new Point(200, 410));
			const double maxDistance = 600;

			FocusNavigationDirection direction = FocusNavigationDirection.Down;

			double scoreA = ProximityStrategy.GetScore(direction, focusedElement, candidateElementA, maxDistance, false);
			double scoreB = ProximityStrategy.GetScore(direction, focusedElement, candidateElementB, maxDistance, false);
			Assert.IsTrue(scoreA > scoreB);
		}

		/*
            +---------+
            |         |
            |         |
            |         |
            |         |
            |         |
            |         |
            +---------+
                                                                                                                         +------+
                                                                                                                         |  A   |
                                                                                                                         +------+
             +-------+
             |       |
             |   B   |
             +-------+
        
        */

		[TestMethod]
		public void VerifyProximityStrategyClosestToAxisWithExtremeDistance()
		{
			Rect focusedElement = new Rect(new Point(100, 100), new Point(200, 200));

			Rect candidateElementA = new Rect(new Point(2000, 200), new Point(2200, 300));
			Rect candidateElementB = new Rect(new Point(100, 315), new Point(200, 415));
			const double maxDistance = 3000;

			FocusNavigationDirection direction = FocusNavigationDirection.Down;

			double scoreA = ProximityStrategy.GetScore(direction, focusedElement, candidateElementA, maxDistance, false);
			double scoreB = ProximityStrategy.GetScore(direction, focusedElement, candidateElementB, maxDistance, false);
			Assert.IsTrue(scoreA > scoreB);
		}

		/*
            +---------+
            |         |
            |         |
            |         |
            |         |
            |         |
            |         |
            +---------+
                                                             +------+
                                                             |  A   |
                                                             +------+
             +-------+
             |       |
             |   B   |
             +-------+
        
        */

		[TestMethod]
		public void VerifyProximityStrategyNearness()
		{
			Rect focusedElement = new Rect(new Point(100, 100), new Point(200, 200));

			Rect candidateElementA = new Rect(new Point(1000, 110), new Point(1200, 160));
			Rect candidateElementB = new Rect(new Point(100, 300), new Point(200, 400));
			const double maxDistance = 600;

			FocusNavigationDirection direction = FocusNavigationDirection.Down;

			double scoreA = ProximityStrategy.GetScore(direction, focusedElement, candidateElementA, maxDistance, true);
			double scoreB = ProximityStrategy.GetScore(direction, focusedElement, candidateElementB, maxDistance, true);
			Assert.IsTrue(scoreB > scoreA);
		}

		/*
                             +------------+
                             |            |
                             |            |
                             |     B      |
                             |            |
                +--------+   |            |
                |        |   +------------+
                |   A    |   +------------+
                |        |   |    C       |
                +--------+   +------------+


        */

		[TestMethod]
		public void VerifyNearnessMeasuresShadow()
		{
			Rect a = new Rect(new Point(0, 100), new Point(50, 200));
			Rect b = new Rect(new Point(75, 50), new Point(125, 140));
			Rect c = new Rect(new Point(75, 150), new Point(125, 200));
			const double maxDistance = 600;

			FocusNavigationDirection direction = FocusNavigationDirection.Down;

			double scoreA = ProximityStrategy.GetScore(direction, a, b, maxDistance, true);
			double scoreB = ProximityStrategy.GetScore(direction, a, c, maxDistance, true);
			Assert.IsTrue(scoreA == scoreB);
		}
	}
}
