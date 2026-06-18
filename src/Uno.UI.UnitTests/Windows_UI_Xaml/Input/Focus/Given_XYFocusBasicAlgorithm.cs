// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// BasicAlgorithmUnitTests.h, BasicAlgorithmUnitTests.cpp

#nullable enable

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Microsoft.UI.Xaml.Input;
using static Uno.UI.Tests.Helpers.MuxVerify;
using static Uno.UI.Xaml.Input.XYFocusAlgorithmHelper;

namespace Uno.UI.Tests.Windows_UI_Xaml.Input.Internal
{
	[TestClass]
	public class Given_XYFocusBasicAlgorithm
	{
		[TestMethod]
		public void DoNotCalculatePrimaryDistanceFromSelf()
		{
			Rect current = new Rect(new Point(0, 0), new Point(100, 100));
			Rect candidate = new Rect(new Point(0, 0), new Point(100, 100));

			double distance = CalculatePrimaryAxisDistance(FocusNavigationDirection.Left, current, candidate);
			Assert.AreEqual(-1, distance);
		}

		/*
		          +---------------+   +---------------+  +----------------+
		          |               |   |               |  |                |
		          |               |   |               |  |                |
		          |               |   |       A       |  |       B        |
		          |               |   |               |  |                |
		          |               |   |               |  |                |
		          +---------------+   +---------------+  +----------------+

		      */

		[TestMethod]
		public void VerifyPrimaryAxisDistance()
		{
			Rect current = new Rect(new Point(0, 0), new Point(100, 100));
			Rect candidateA = new Rect(new Point(125, 0), new Point(225, 100));
			Rect candidateB = new Rect(new Point(235, 0), new Point(335, 100));

			double distanceA = CalculatePrimaryAxisDistance(FocusNavigationDirection.Right, current, candidateA);
			double distanceB = CalculatePrimaryAxisDistance(FocusNavigationDirection.Right, current, candidateB);
			Assert.IsLessThan(distanceB, distanceA);
		}

		/*
                  +-------------+
                  |             |
                  |             |
                  |             |
                  |             |
                  +-------------+
                                             +---------+
                                             |         |
                                             |   A     |
                                             +---------+

                                    +-------+
                                    |       |
                                    |  B    |
                                    +-------+

              */

		[TestMethod]
		public void VerifySecondaryAxisDistance()
		{
			Rect current = new Rect(new Point(0, 0), new Point(100, 100));
			Rect candidateA = new Rect(new Point(150, 125), new Point(225, 225));
			Rect candidateB = new Rect(new Point(110, 175), new Point(220, 225));

			double distanceA = CalculateSecondaryAxisDistance(FocusNavigationDirection.Right, current, candidateA);
			double distanceB = CalculateSecondaryAxisDistance(FocusNavigationDirection.Right, current, candidateB);
			Assert.IsLessThan(distanceB, distanceA);

			// We can have situations where the secondary distance is shorter although the primary distance is longer
			distanceA = CalculatePrimaryAxisDistance(FocusNavigationDirection.Right, current, candidateA);
			distanceB = CalculatePrimaryAxisDistance(FocusNavigationDirection.Right, current, candidateB);
			Assert.IsGreaterThan(distanceB, distanceA);
		}

		/*

              +----------------+
              |                |
              |                |
              |     B          |
              |                |
              |                |
              +----------------+

              +---------------+   +---------------+
              |               |   |               |
              |               |   |               |
              |               |   |       A       |
              |               |   |               |
              |               |   |               |
              +---------------+   +---------------+

              */

		[TestMethod]
		public void VerifyShadow()
		{
			Rect current = new Rect(new Point(0, 100), new Point(100, 200));
			Rect candidateA = new Rect(new Point(125, 100), new Point(225, 200));
			Rect candidateB = new Rect(new Point(0, 0), new Point(100, 100));

			(double first, double second) potentialVertical;
			(double first, double second) referenceVertical;

			(double first, double second) potentialHorizontal;
			(double first, double second) referenceHorizontal;

			referenceVertical.first = current.Left;
			referenceVertical.second = current.Right;

			potentialVertical.first = candidateB.Left;
			potentialVertical.second = candidateB.Right;

			referenceHorizontal.first = current.Top;
			referenceHorizontal.second = current.Bottom;

			potentialHorizontal.first = candidateA.Top;
			potentialHorizontal.second = candidateA.Bottom;

			Assert.IsTrue(CalculatePercentInShadow(potentialVertical, referenceVertical) > 0);
			Assert.IsTrue(CalculatePercentInShadow(referenceVertical, potentialVertical) > 0);
			Assert.IsTrue(CalculatePercentInShadow(referenceHorizontal, potentialHorizontal) > 0);
			Assert.IsTrue(CalculatePercentInShadow(potentialHorizontal, referenceHorizontal) > 0);
		}

		/*
              The scenario that the test are based off
              +---------------------------------------------------------Root---------------------------------------------------------------------------------------------+
              |                                                                                                                                 _ -ve Margins            |
              |                                                                                                                                |                         |
              |                                                                                                                                V                         |
              |                                          +-----------------+              +------------------+              +-----------------++----------------+        |
              |            +-----------------+           |                 |              |                  |              |                 ||                |        |
              |            |       F         |           |                 |              |        C         |              |        G        ||       J        |        |
              |            +-----------------+           |                 |              |                  |              |                 ||                |        |
              |       +----+-----------------++          |                 |              +------------------+              +-----------------++----------------+        |
              |       |                       |          |                 |              +------------------+              +-----------------++----------------+        |
              |       |                       |          |                 |              |                  |              |                 ||                |        |
              |       |           B           |          |       Main      |              |         D        |              |         H       ||       K        |        |
              |       |                       |          |                 |              |                  |              |                 ||                |        |
              |       +----+------------------+          |                 |              +------------------+              +-----------------++----------------+        |
              |            +------------------+          |                 |              +------------------+              +-----------------++----------------+        |
              |            |                  |          |                 |              |                  |              |                 ||                |        |
              |            |       E          |          |                 |              |         A        |              |         I       ||       L        |        |
              |            |                  |          |                 |              |                  |              |                 ||                |        |
              |            +------------------+          +-----------------+              +------------------+              +-----------------++----------------+        |
              |                                                                                                                                                          |
              +----------------------------------------------------------------------------------------------------------------------------------------------------------+
        */

		private readonly Rect _elementMain = new Rect(new Point(120, 20), new Point(240, 260));
		private readonly Rect _elementA = new Rect(new Point(280, 200), new Point(360, 260));
		private readonly Rect _elementB = new Rect(new Point(20, 120), new Point(100, 180));
		private readonly Rect _elementC = new Rect(new Point(280, 20), new Point(360, 80));
		private readonly Rect _elementD = new Rect(new Point(280, 120), new Point(360, 180));

		private readonly Rect _elementE = new Rect(new Point(20, 200), new Point(100, 260));
		private readonly Rect _elementF = new Rect(new Point(20, 20), new Point(100, 80));

		private readonly Rect _elementG = new Rect(new Point(440, 20), new Point(520, 80));
		private readonly Rect _elementH = new Rect(new Point(440, 120), new Point(520, 180));
		private readonly Rect _elementI = new Rect(new Point(440, 200), new Point(520, 260));

		private readonly Rect _elementJ = new Rect(new Point(500, 20), new Point(580, 80));
		private readonly Rect _elementK = new Rect(new Point(500, 120), new Point(580, 180));
		private readonly Rect _elementL = new Rect(new Point(500, 200), new Point(580, 260));

		private Rect[]? _elementArray;

		public Rect[] ElementArray => _elementArray ??= new Rect[]
		{
			_elementF,
			_elementB,
			_elementE,
			_elementMain,
			_elementC,
			_elementD,
			_elementA,
			_elementG,
			_elementH,
			_elementI,
			_elementJ,
			_elementK,
			_elementL
		};

		private List<Rect>? _scenario;

		public List<Rect> Scenario => _scenario ??= new List<Rect>(ElementArray);

		private Rect BestElement(
			List<Rect> rectList,
			Rect bounds,
			FocusNavigationDirection direction,
			XYFocusNavigationStrategy mode,
			(double first, double second) hManifold = default,
			(double first, double second) vManifold = default)
		{
			(Rect rect, double score) bestElement = (new Rect(), 0);

			XYFocusAlgorithms projection = new XYFocusAlgorithms();

			const double distance = 10000;

			foreach (Rect rect in rectList)
			{
				double score = 0;

				if (mode == XYFocusNavigationStrategy.Projection)
				{
					score = projection.GetScore(direction, bounds, rect, hManifold, vManifold, distance);
				}
				else if (mode == XYFocusNavigationStrategy.NavigationDirectionDistance || mode == XYFocusNavigationStrategy.RectilinearDistance)
				{
					score = ProximityStrategy.GetScore(direction, bounds, rect, distance, mode == XYFocusNavigationStrategy.RectilinearDistance);
				}

				if (score > bestElement.score)
				{
					bestElement.rect = rect;
					bestElement.score = score;
				}
			}

			return bestElement.rect;
		}

		[TestMethod]
		public void VerifyLeft()
		{
			Rect bounds = _elementMain;
			FocusNavigationDirection direction = FocusNavigationDirection.Left;

			(double first, double second) vManifold = (-1.0, -1.0);
			(double first, double second) hManifold = (-1.0, -1.0);

			Rect candidate = BestElement(Scenario, bounds, direction, XYFocusNavigationStrategy.Projection, hManifold, vManifold);
			VerifyAreEqual(candidate, _elementF);

			candidate = BestElement(Scenario, bounds, direction, XYFocusNavigationStrategy.NavigationDirectionDistance);
			VerifyAreEqual(candidate, _elementF);

			candidate = BestElement(Scenario, bounds, direction, XYFocusNavigationStrategy.RectilinearDistance);
			VerifyAreEqual(candidate, _elementF);
		}

		[TestMethod]
		public void VerifyRight()
		{
			Rect bounds = _elementE;
			FocusNavigationDirection direction = FocusNavigationDirection.Right;

			(double first, double second) vManifold = (-1.0, -1.0);
			(double first, double second) hManifold = (-1.0, -1.0);

			Rect candidate = BestElement(Scenario, bounds, direction, XYFocusNavigationStrategy.Projection, hManifold, vManifold);
			VerifyAreEqual(candidate, _elementMain);

			candidate = BestElement(Scenario, bounds, direction, XYFocusNavigationStrategy.NavigationDirectionDistance);
			VerifyAreEqual(candidate, _elementMain);

			candidate = BestElement(Scenario, bounds, direction, XYFocusNavigationStrategy.RectilinearDistance);
			VerifyAreEqual(candidate, _elementMain);
		}

		[TestMethod]
		public void VerifyUp()
		{
			Rect bounds = _elementE;
			FocusNavigationDirection direction = FocusNavigationDirection.Up;

			(double first, double second) vManifold = (-1.0, -1.0);
			(double first, double second) hManifold = (-1.0, -1.0);

			Rect candidate = BestElement(Scenario, bounds, direction, XYFocusNavigationStrategy.Projection, hManifold, vManifold);
			VerifyAreEqual(candidate, _elementB);

			candidate = BestElement(Scenario, bounds, direction, XYFocusNavigationStrategy.NavigationDirectionDistance);
			VerifyAreEqual(candidate, _elementB);

			candidate = BestElement(Scenario, bounds, direction, XYFocusNavigationStrategy.RectilinearDistance);
			VerifyAreEqual(candidate, _elementB);
		}

		[TestMethod]
		public void VerifyDown()
		{
			Rect bounds = _elementF;
			FocusNavigationDirection direction = FocusNavigationDirection.Down;

			(double first, double second) vManifold = (-1.0, -1.0);
			(double first, double second) hManifold = (-1.0, -1.0);

			Rect candidate = BestElement(Scenario, bounds, direction, XYFocusNavigationStrategy.Projection, hManifold, vManifold);
			VerifyAreEqual(candidate, _elementB);

			candidate = BestElement(Scenario, bounds, direction, XYFocusNavigationStrategy.NavigationDirectionDistance);
			VerifyAreEqual(candidate, _elementB);

			candidate = BestElement(Scenario, bounds, direction, XYFocusNavigationStrategy.RectilinearDistance);
			VerifyAreEqual(candidate, _elementB);
		}

		[TestMethod]
		public void VerifyNoCandidates()
		{
			Rect bounds = _elementE;
			FocusNavigationDirection direction = FocusNavigationDirection.Down;

			(double first, double second) vManifold = (-1.0, -1.0);
			(double first, double second) hManifold = (-1.0, -1.0);

			Rect candidate = BestElement(Scenario, bounds, direction, XYFocusNavigationStrategy.Projection, hManifold, vManifold);
			VerifyAreEqual(candidate, new Rect());

			candidate = BestElement(Scenario, bounds, direction, XYFocusNavigationStrategy.NavigationDirectionDistance);
			VerifyAreEqual(candidate, new Rect());

			candidate = BestElement(Scenario, bounds, direction, XYFocusNavigationStrategy.RectilinearDistance);
			VerifyAreEqual(candidate, new Rect());
		}

		[TestMethod]
		public void VerifyManifoldAidsInDecision()
		{
			Rect bounds = _elementMain;
			FocusNavigationDirection direction = FocusNavigationDirection.Right;

			(double first, double second) vManifold = (-1.0, -1.0);
			(double first, double second) hManifold = (-1.0, -1.0);

			XYFocusAlgorithms.UpdateManifolds(FocusNavigationDirection.Right, _elementE, _elementMain, ref hManifold, ref vManifold);

			Rect candidate = BestElement(Scenario, bounds, direction, XYFocusNavigationStrategy.Projection, hManifold, vManifold);
			VerifyAreEqual(candidate, _elementA);
		}

		[TestMethod]
		public void ValidateOverlappingElementToRight()
		{
			Rect bounds = _elementG;
			FocusNavigationDirection direction = FocusNavigationDirection.Right;

			(double first, double second) vManifold = (-1.0, -1.0);
			(double first, double second) hManifold = (-1.0, -1.0);

			Rect candidate = BestElement(Scenario, bounds, direction, XYFocusNavigationStrategy.Projection, hManifold, vManifold);
			VerifyAreEqual(candidate, _elementJ);

			candidate = BestElement(Scenario, bounds, direction, XYFocusNavigationStrategy.NavigationDirectionDistance);
			VerifyAreEqual(candidate, _elementJ);

			candidate = BestElement(Scenario, bounds, direction, XYFocusNavigationStrategy.RectilinearDistance);
			VerifyAreEqual(candidate, _elementJ);
		}

		/*
		                                                                        +------------------+
		                                                                        |                  |
		                                                                        |                  |
		                                                                        |       a          |
		                                                                        |                  |
		                                                                        |                  |
		                                                                        +------------------+
		                  +----------------------------------------------------------------------------+
		                  |                                                                            |
		                  |                                                                            |
		                  |         +------------------+                                               |
		                  |         |                  |                                               |
		                  |         |                  |                                               |
		                  |         |                  |                                               |
		                  |         |                  |                                               |
		                  |         |                  |                                               |
		                  |         |                  |                                               |
		                  |         |                  |                                               |
		                  |         |                  |                                               |
		                  |         |      Start       |                                               |
		                  |         |                  |                                               |
		                  |         |                  |                                               |
		                  |         |                  |                                               |
		                  |         |                  |                                               |
		                  |         |                  |                                               |
		                  |         |                  |                                               |
		                  |         |                  |                                               |
		                  |         +------------------+                                               |
		                  |                                                                            |
		                  |                                                                    Root    |
		                  +----------------------------------------------------------------------------+
		*/

		[TestMethod]
		public void ElementWithNegativeBoundsRankedCorrectly()
		{
			Rect start = new Rect(new Point(120, 20), new Point(240, 260));
			Rect a = new Rect(new Point(440, -80), new Point(520, -20));

			FocusNavigationDirection direction = FocusNavigationDirection.Right;

			(double first, double second) vManifold = (-1.0, -1.0);
			(double first, double second) hManifold = (-1.0, -1.0);

			List<Rect> scenario = new List<Rect>();
			scenario.Add(start);
			scenario.Add(a);

			Rect candidate = BestElement(scenario, start, direction, XYFocusNavigationStrategy.Projection, hManifold, vManifold);
			VerifyAreEqual(candidate, a);

			candidate = BestElement(scenario, start, direction, XYFocusNavigationStrategy.NavigationDirectionDistance);
			VerifyAreEqual(candidate, a);

			candidate = BestElement(scenario, start, direction, XYFocusNavigationStrategy.RectilinearDistance);
			VerifyAreEqual(candidate, a);
		}

		/*
																			+------------------+
																			|                  |
																			|                  |
																			|       start      |
																			|                  |
																			|                  |
																			+------------------+
					  +----------------------------------------------------------------------------+
					  |                                                                            |
					  |                                                                            |
					  |         +------------------+                                               |
					  |         |                  |                                               |
					  |         |                  |                                               |
					  |         |                  |                                               |
					  |         |                  |                                               |
					  |         |                  |                                               |
					  |         |                  |                                               |
					  |         |                  |                                               |
					  |         |                  |                                               |
					  |         |       a          |                                               |
					  |         |                  |                                               |
					  |         |                  |                                               |
					  |         |                  |                                               |
					  |         |                  |                                               |
					  |         |                  |                                               |
					  |         |                  |                                               |
					  |         |                  |                                               |
					  |         +------------------+                                               |
					  |                                                                            |
					  |                                                                    Root    |
					  +----------------------------------------------------------------------------+
		*/

		[TestMethod]
		public void FocusedElementWithNegativeBoundsProducesValidCandidatesThatIsOnSreen()
		{
			Rect a = new Rect(new Point(120, 20), new Point(240, 260));
			Rect start = new Rect(new Point(440, -80), new Point(520, -20));

			FocusNavigationDirection direction = FocusNavigationDirection.Left;

			(double first, double second) vManifold = (-1.0, -1.0);
			(double first, double second) hManifold = (-1.0, -1.0);

			List<Rect> scenario = new List<Rect>();
			scenario.Add(start);
			scenario.Add(a);

			Rect candidate = BestElement(scenario, start, direction, XYFocusNavigationStrategy.Projection, hManifold, vManifold);
			VerifyAreEqual(candidate, a);

			candidate = BestElement(scenario, start, direction, XYFocusNavigationStrategy.NavigationDirectionDistance);
			VerifyAreEqual(candidate, a);

			candidate = BestElement(scenario, start, direction, XYFocusNavigationStrategy.RectilinearDistance);
			VerifyAreEqual(candidate, a);
		}

		/*
																					  +------------------+
																					  |                  |
																					  |                  |
																					  |       a          |
																					  |                  |
																					  |                  |
																					  +------------------+



										  +------------------+
										  |                  |
										  |                  |
										  |                  |
										  |                  |
										  |                  |
										  |                  |
										  |                  |
										  |                  |
										  |       start      |
										  |                  |
										  |                  |
										  |                  |
										  |                  |
										  |                  |
										  |                  |
										  |                  |
										  +------------------+
						  +------------------------------------------------------------------------------------------+
						  |                                                                                          |
						  |                                                                                          |
						  |                                                                                          |
						  |                                                                                          |
						  |                                                                                          |
						  |                                                                                          |
						  |                                                                                          |
						  |                                                                                          |
						  |                                                                                          |
						  |                                                                                          |
						  |                                                                                          |
						  |                                                                                          |
						  |                                                                                          |
						  |                                                                                          |
						  |                                                                                          |
						  |                                                                                          |
						  |                                                                                          |
						  |                                                                                          |
						  |                                                                                          |
						  |                                                                                          |
						  |                                                                                  root    |
						  +------------------------------------------------------------------------------------------+

		*/

		[TestMethod]
		public void FocusedElementWithNegativeSelectsCandidateThatIsOffScreen()
		{
			Rect start = new Rect(new Point(120, -140), new Point(240, -10));
			Rect a = new Rect(new Point(280, -140), new Point(360, -80));

			FocusNavigationDirection direction = FocusNavigationDirection.Right;

			(double first, double second) vManifold = (-1.0, -1.0);
			(double first, double second) hManifold = (-1.0, -1.0);

			List<Rect> scenario = new List<Rect>();
			scenario.Add(start);
			scenario.Add(a);

			Rect candidate = BestElement(scenario, start, direction, XYFocusNavigationStrategy.Projection, hManifold, vManifold);
			VerifyAreEqual(candidate, a);

			candidate = BestElement(scenario, start, direction, XYFocusNavigationStrategy.NavigationDirectionDistance);
			VerifyAreEqual(candidate, a);

			candidate = BestElement(scenario, start, direction, XYFocusNavigationStrategy.RectilinearDistance);
			VerifyAreEqual(candidate, a);
		}

		/*
				  +-------------------------------------------------------------------+
				  |                                                                   |
				  |                                                                   |
				  |                                                                   |
				  |                       +------------+                              |
				  |                       |            |                              |
				  |                       |            |                              |
				  |                       |            |                              |
				  |                       |            |                              |
				  |                       |            |                              |                               +--------------+
				  |                       |            |                              |                               |              |
				  |                       |            |                              |                               |              |
				  |                       |            |                              |      Very far apart           |          a   |
				  |                       |            |                              |                               +--------------+
				  |                       |            |                              |
				  |                       |       start|                              |
				  |                       +------------+                              |
				  |                                                                   |
				  |                                                                   |
				  |                                                                   |
				  |                                                                   |
				  |                                                              root |
				  +-------------------------------------------------------------------+
			  */

		[TestMethod]
		public void ElementScrolledExtremleyOutOfViewShouldStillBeSelected()
		{
			Rect start = new Rect(new Point(120, 20), new Point(240, 260));
			Rect a = new Rect(new Point(10000, 120), new Point(10080, 200));

			FocusNavigationDirection direction = FocusNavigationDirection.Right;

			(double first, double second) vManifold = (-1.0, -1.0);
			(double first, double second) hManifold = (-1.0, -1.0);

			List<Rect> scenario = new List<Rect>();
			scenario.Add(start);
			scenario.Add(a);

			Rect candidate = BestElement(scenario, start, direction, XYFocusNavigationStrategy.Projection, hManifold, vManifold);
			VerifyAreEqual(candidate, a);

			candidate = BestElement(scenario, start, direction, XYFocusNavigationStrategy.NavigationDirectionDistance);
			VerifyAreEqual(candidate, a);

			candidate = BestElement(scenario, start, direction, XYFocusNavigationStrategy.RectilinearDistance);
			VerifyAreEqual(candidate, a);
		}
	}
}
