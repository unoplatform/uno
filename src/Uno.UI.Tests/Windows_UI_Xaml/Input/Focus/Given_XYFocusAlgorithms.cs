// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// XYFocusAlgorithmsUnitTests.h, XYFocusAlgorithmsUnitTests.cpp

#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.UI.Xaml.Input;

namespace Uno.UI.Tests.Windows_UI_Xaml.Input.Internal
{
	[TestClass]
	public class Given_XYFocusAlgorithms
	{
		/*

																XXXXXXX
														  XXXXXX
													XXXXXX
		+-----------------------------------+XXXXXX
		|                                   |     
		|                                   |                           +----------------------+
		|                                   |                           |                      |
		|                                   |                           |                      |
		|                                   |                           |                      |
		|                                   |                           |                      |
		|                                   |                           |                      |
		|                                   |                           |                      |
		|                                   |                           +----------------------+
		|                                   |         
		|                                   |                 
		|                                   |
		+-----------------------------------+XXXXXXX
													XXXXXXX
														   XXXXXXX
																  XXXXXXX 

		*/

		[TestMethod]
		public void ElementCompletelyInsideVisionCodeDetected()
		{
			var focusedElement = new Rect(new Point(100, 100), new Point(200, 200));
			var candidateElement = new Rect(new Point(430, 0), new Point(500, 300));

			const double maxDistance = 600;

			FocusNavigationDirection direction = FocusNavigationDirection.Right;

			bool shouldConsider = XYFocusAlgorithms.ShouldCandidateBeConsideredForRanking(
				focusedElement, candidateElement, maxDistance, direction, Rect.Empty);
			Assert.IsTrue(shouldConsider);

			candidateElement = new Rect(new Point(50, 100), new Point(100, 150));
			direction = FocusNavigationDirection.Left;

			shouldConsider = XYFocusAlgorithms.ShouldCandidateBeConsideredForRanking(
				focusedElement, candidateElement, maxDistance, direction, Rect.Empty);
			Assert.IsTrue(shouldConsider);
		}

		/*
										 +------------------+
										 |                  |
										 |                  |
										 |                  |
										 |                  |              XXXXX
										 +------------------+      X XXXXXXX
															  XXXXXXXX
														XXXXXXX
												   XXXXXX
											XXXXXX
									 XXXXXX
		+------------------------+XXX 
		|                        |
		|                        |
		|                        |
		|                        |
		+------------------------+XXXXXXX
										  XXXXXXX
												  XXXXXXX  
														 XXXXXXX
																XXXXX
		*/

		[TestMethod]
		public void ElementCompletelyOutsideVisionCodeDetected()
		{
			var focusedElement = new Rect(new Point(100, 100), new Point(200, 200));
			var candidateElement = new Rect(new Point(0, 0), new Point(50, 50));

			const double maxDistance = 600;

			FocusNavigationDirection direction = FocusNavigationDirection.Right;

			bool shouldConsider = XYFocusAlgorithms.ShouldCandidateBeConsideredForRanking(
				focusedElement, candidateElement, maxDistance, direction, Rect.Empty);
			Assert.IsFalse(shouldConsider);

			candidateElement = new Rect(new Point(0, 500), new Point(50, 550));
			direction = FocusNavigationDirection.Left;

			shouldConsider = XYFocusAlgorithms.ShouldCandidateBeConsideredForRanking(
				focusedElement, candidateElement, maxDistance, direction, Rect.Empty);
			Assert.IsFalse(shouldConsider);
		}

		/*
																   +----------------------+       XX
																   |                      | XXXXXXX
																   |             XXXXXXXXXXXX
																   |       XXXXXXX        |
																   |X XXXXXX              |
																XXX|                      |
														   XXXXX   |                      |
													  XXXXX        |                      |
											 XXXXXXXXX             +----------------------+
			+--------------------------------+
			|                                |
			|                                |
			|                                |
			|                                |
			|                                |
			|                                |
			|                                |
			|                                |
			+--------------------------------+XXXXXXX
													 XXXXXXX
															XXXXXXX  
																   XXXXXXX
																		  XXXXX
			*/

		[TestMethod]
		public void ElementIntersectsVisionCodeDetected()
		{
			var focusedElement = new Rect(new Point(100, 100), new Point(200, 200));
			var candidateElement = new Rect(new Point(300, 30), new Point(350, 80));

			const double maxDistance = 600;

			FocusNavigationDirection direction = FocusNavigationDirection.Right;

			bool shouldConsider = XYFocusAlgorithms.ShouldCandidateBeConsideredForRanking(
				focusedElement, candidateElement, maxDistance, direction, Rect.Empty);
			Assert.IsTrue(shouldConsider);

			candidateElement = new Rect(new Point(50, 50), new Point(80, 80));
			direction = FocusNavigationDirection.Up;

			shouldConsider = XYFocusAlgorithms.ShouldCandidateBeConsideredForRanking(
				focusedElement, candidateElement, maxDistance, direction, Rect.Empty);
			Assert.IsTrue(shouldConsider);
		}

		/*
							XXXXX  
					   XXXXX
				  XXXXX          
		+------+X +-------+
		|      |  |       |
		|      |  |       |
		|      |  |       |
		|      |  +-------+
		|      |
		|      |
		|      |
		|      |
		|      |
		|      |
		|      |
		|      |
		|      |
		|      |
		|      |
		+------+XXXXX
					  XXXXXXX
							  XXXXXXX  
		*/

		[TestMethod]
		public void VerifyCandidateUpAgainstCone()
		{
			var focusedElement = new Rect(new Point(100, 100), new Point(120, 600));
			var candidateElement = new Rect(new Point(120, 100), new Point(150, 150));

			const double maxDistance = 600;

			FocusNavigationDirection direction = FocusNavigationDirection.Right;

			bool shouldConsider = XYFocusAlgorithms.ShouldCandidateBeConsideredForRanking(
				focusedElement, candidateElement, maxDistance, direction, Rect.Empty);
			Assert.IsTrue(shouldConsider);

			candidateElement = new Rect(new Point(50, 100), new Point(100, 150));
			direction = FocusNavigationDirection.Left;

			shouldConsider = XYFocusAlgorithms.ShouldCandidateBeConsideredForRanking(
				focusedElement, candidateElement, maxDistance, direction, Rect.Empty);
			Assert.IsTrue(shouldConsider);
		}

		[TestMethod]
		public void IntersectionDetectedAtEdge()
		{
			var focusedElement = new Rect(new Point(100, 100), new Point(200, 200));

			float left = (float)(focusedElement.Left + 50 * Math.Cos(0));
			float top = (float)(focusedElement.Top + 50 * Math.Sin(0));
			var candidateElement = new Rect(left, top, 350, 180);

			const double maxDistance = 600;

			FocusNavigationDirection direction = FocusNavigationDirection.Right;

			bool shouldConsider = XYFocusAlgorithms.ShouldCandidateBeConsideredForRanking(
				focusedElement, candidateElement, maxDistance, direction, Rect.Empty);

			Assert.IsTrue(shouldConsider);
		}

		[TestMethod]
		public void VerifyExclusionRectsContainer()
		{
			var focusedElement = new Rect(new Point(100, 100), new Point(200, 200));
			var exclusionRect = new Rect(new Point(200, 200), new Point(400, 400));

			var candidateElement = new Rect(new Point(300, 300), new Point(350, 350));

			const double maxDistance = 600;

			FocusNavigationDirection direction = FocusNavigationDirection.Right;

			bool shouldConsider = XYFocusAlgorithms.ShouldCandidateBeConsideredForRanking(
				focusedElement, candidateElement, maxDistance, direction, exclusionRect);

			Assert.IsFalse(shouldConsider);
		}

		[TestMethod]
		public void VerifyExclusionRectsIntersection()
		{
			var focusedElement = new Rect(new Point(100, 100), new Point(200, 200));
			var exclusionRect = new Rect(new Point(200, 200), new Point(400, 400));

			var candidateElement = new Rect(new Point(350, 350), new Point(450, 450));

			const double maxDistance = 600;

			FocusNavigationDirection direction = FocusNavigationDirection.Right;

			bool shouldConsider = XYFocusAlgorithms.ShouldCandidateBeConsideredForRanking(
				focusedElement, candidateElement, maxDistance, direction, exclusionRect);

			Assert.IsFalse(shouldConsider);
		}

		[TestMethod]
		public void VerifyEmptyRectsAreIgnoredAsCandidates()
		{
			var focusedElement = new Rect(new Point(100, 100), new Point(200, 200));

			var candidateElement = new Rect(new Point(100, 10), new Point(100, 10));

			const double maxDistance = 600;

			FocusNavigationDirection direction = FocusNavigationDirection.Up;

			bool shouldConsider = XYFocusAlgorithms.ShouldCandidateBeConsideredForRanking(
				focusedElement, candidateElement, maxDistance, direction, new Rect());

			Assert.IsFalse(shouldConsider);
		}
	}
}
