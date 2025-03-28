// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// h, cpp

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Helpers.WinUI;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Uno.Helpers;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Uno.UI.Xaml.Input
{
	internal class XYFocus
	{
		internal const int InitialCandidateListCapacity = 50;

		internal struct XYFocusParameters
		{
			public DependencyObject? Element { get; set; }

			public Rect Bounds { get; set; }

			public double Score { get; set; }
		}

		internal class XYFocusMaxRootBoundComparer : IComparer<XYFocusParameters>
		{
			private readonly FocusNavigationDirection _direction;

			internal XYFocusMaxRootBoundComparer(FocusNavigationDirection direction)
			{
				_direction = direction;
			}

			public int Compare(XYFocusParameters x, XYFocusParameters y)
			{
				Rect xBounds = x.Bounds;
				Rect yBounds = y.Bounds;

				if (_direction == FocusNavigationDirection.Left)
				{
					return xBounds.Left.CompareTo(yBounds.Left);
				}
				else if (_direction == FocusNavigationDirection.Right)
				{
					return yBounds.Right.CompareTo(xBounds.Right);
				}
				else if (_direction == FocusNavigationDirection.Up)
				{
					return xBounds.Top.CompareTo(yBounds.Top);
				}
				else if (_direction == FocusNavigationDirection.Down)
				{
					return yBounds.Bottom.CompareTo(xBounds.Bottom);
				}

				return 1;
			}
		}

		internal class XYFocusParametersBestFocusableElementComparer : IComparer<XYFocusParameters>
		{
			private readonly FocusNavigationDirection _direction;
			private readonly bool _isRightToLeft;

			internal XYFocusParametersBestFocusableElementComparer(FocusNavigationDirection direction, bool isRightToLeft)
			{
				_direction = direction;
				_isRightToLeft = isRightToLeft;
			}

			public int Compare(XYFocusParameters elementA, XYFocusParameters elementB)
			{
				if (elementA.Score == elementB.Score)
				{
					Rect firstBounds = elementA.Bounds;
					Rect secondBounds = elementB.Bounds;

					// In the case of a tie, we want to choose the element furthest top or left (depending on FocusNavigation and FlowDirection)
					if (_direction == FocusNavigationDirection.Up || _direction == FocusNavigationDirection.Down)
					{
						if (_isRightToLeft)
						{
							return secondBounds.Left.CompareTo(firstBounds.Left);
						}

						return firstBounds.Left.CompareTo(secondBounds.Left);
					}

					else
					{
						return firstBounds.Top.CompareTo(secondBounds.Top);
					}
				}

				return elementB.Score.CompareTo(elementA.Score);
			}
		}

		internal struct Manifolds
		{
			public static Manifolds Default
			{
				get
				{
					var manifolds = new Manifolds();
					manifolds.Reset();
					return manifolds;
				}
			}

			public void Reset()
			{
				Vertical = (-1, -1);
				Horizontal = (-1, -1);
			}

			public (double top, double bottom) Vertical { get; set; }

			public (double left, double right) Horizontal { get; set; }
		}

		private Manifolds _manifolds;
		private XYFocusAlgorithms _heuristic = new XYFocusAlgorithms();
		private HashSet<int> _exploredList = new HashSet<int>();

		internal Manifolds ResetManifolds()
		{
			var oldManifolds = _manifolds;

			_manifolds.Reset();

			return oldManifolds;
		}

		internal void SetManifolds(Manifolds manifolds)
		{
			_manifolds.Vertical = manifolds.Vertical;
			_manifolds.Horizontal = manifolds.Horizontal;
		}

#if false
		private void ClearCache()
		{
			_exploredList.Clear();
		}
#endif

		internal DependencyObject? GetNextFocusableElement(
			FocusNavigationDirection direction,
			DependencyObject? element,
			DependencyObject? engagedControl,
			VisualTree visualTree,
			bool updateManifolds,
			XYFocusOptions xyFocusOptions)
		{
			if (element == null)
			{
				return null;
			}

			int hash = 0;
			if (_exploredList.Count != 0)
			{
				hash = ExploredListHash(direction, element, engagedControl, xyFocusOptions);
				if (_exploredList.Contains(hash))
				{
					CacheHitTrace(direction);
					return null;
				}
			}

			var root = VisualTree.GetRootOrIslandForElement(element);
			bool isRightToLeft = element.IsRightToLeft();

			XYFocusNavigationStrategy mode = XYFocusBubbling.GetStrategy(element, direction, xyFocusOptions.NavigationStrategyOverride);

			Rect rootBounds;
			Rect focusedElementBounds = xyFocusOptions.FocusedElementBounds;

			var nextFocusableElement = XYFocusBubbling.GetDirectionOverride(element, xyFocusOptions.SearchRoot, direction, true /*ignoreFocusability*/);

			if (nextFocusableElement != null)
			{
				return nextFocusableElement;
			}

			DependencyObject? activeScroller = GetActiveScrollerForScrollInput(direction, element);
			bool isProcessingInputForScroll = (activeScroller != null);

			if (xyFocusOptions.FocusHintRectangle != null)
			{
				// Because we have a focus hint rectangle, we should not have the focused element have any role in what elements are chosen as a candidate
				focusedElementBounds = xyFocusOptions.FocusHintRectangle.Value;
				element = null;
			}

			if (engagedControl != null)
			{
				rootBounds = XYFocusTreeWalker.GetBoundsForRanking(engagedControl, xyFocusOptions.IgnoreClipping);
			}
			else if (xyFocusOptions.SearchRoot != null)
			{
				rootBounds = XYFocusTreeWalker.GetBoundsForRanking(xyFocusOptions.SearchRoot, xyFocusOptions.IgnoreClipping);
			}
			else
			{
				rootBounds = XYFocusTreeWalker.GetBoundsForRanking(root, xyFocusOptions.IgnoreClipping);
			}

			var candidateList = GetAllValidFocusableChildren(root, direction, element, engagedControl, xyFocusOptions.SearchRoot, visualTree, activeScroller, xyFocusOptions.IgnoreClipping, xyFocusOptions.ShouldConsiderXYFocusKeyboardNavigation);

			if (candidateList.Count > 0)
			{
				double maxRootBoundsDistance = Math.Max(rootBounds.Right - rootBounds.Left, rootBounds.Bottom - rootBounds.Top);
				maxRootBoundsDistance = Math.Max(maxRootBoundsDistance, GetMaxRootBoundsDistance(candidateList, focusedElementBounds, direction, xyFocusOptions.IgnoreClipping));

				RankElements(candidateList, direction, focusedElementBounds, maxRootBoundsDistance, mode, xyFocusOptions.ExclusionRect, xyFocusOptions.IgnoreClipping, xyFocusOptions.IgnoreCone);

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					foreach (XYFocusParameters it in candidateList)
					{
						this.Log().LogDebug("Candidate: {0} {1},{2} {3},{4} rank {5}",
							it.Element,
							it.Bounds.Left,
							it.Bounds.Top,
							it.Bounds.Right,
							it.Bounds.Bottom,
							it.Score);
					}
				}

				bool ignoreOcclusivity = xyFocusOptions.IgnoreOcclusivity || isProcessingInputForScroll;

				//Choose the best candidate, after testing for occlusivity, if we're currently scrolling, the test has been done already, skip it.
				nextFocusableElement = ChooseBestFocusableElementFromList(candidateList, direction, visualTree, focusedElementBounds, xyFocusOptions.IgnoreClipping, ignoreOcclusivity, isRightToLeft, xyFocusOptions.UpdateManifold && updateManifolds);
				nextFocusableElement = XYFocusBubbling.TryXYFocusBubble(element, nextFocusableElement, xyFocusOptions.SearchRoot, direction);
			}

			// Store the result in the explored list if the candidate is null.
			if (nextFocusableElement == null)
			{
				if (hash == 0)
				{
					hash = ExploredListHash(direction, element, engagedControl, xyFocusOptions);
				}

				_exploredList.Add(hash);
			}

			return nextFocusableElement;
		}

		private DependencyObject? ChooseBestFocusableElementFromList(
			IList<XYFocusParameters> scoreList,
			FocusNavigationDirection direction,
			VisualTree visualTree,
			Rect bounds,
			bool ignoreClipping,
			bool ignoreOcclusivity,
			bool isRightToLeft,
			bool updateManifolds)
		{
			DependencyObject? bestElement = null;
			scoreList = scoreList.OrderBy(param => param, new XYFocusParametersBestFocusableElementComparer(direction, isRightToLeft)).ToList();

			foreach (var param in scoreList)
			{
				if (param.Score <= 0) { break; }

				// When passing in the bounds for OcclusivityTesting, we want to ensure that we are using the non clipped bounds. Therefore, if ignoreClipping is
				// set to true, that means that our cached bounds are invalid for OcclusivityTesting.
				Rect boundsForOccTesting = ignoreClipping ? XYFocusTreeWalker.GetBoundsForRanking(param.Element, false) : param.Bounds;

				// Don't check for occlusivity if we've already covered occlusivity scenarios for scrollable content or have been asked
				// to ignore occlusivity by the caller.
				if (!param.Bounds.IsInfinite && (ignoreOcclusivity || !XYFocusTreeWalker.IsOccluded(param.Element, boundsForOccTesting)))
				{
					bestElement = param.Element;

					if (updateManifolds)
					{
						//Update the manifolds with the newly selected focus

						var hManifold = _manifolds.Horizontal;
						var vManifold = _manifolds.Vertical;
						XYFocusAlgorithms.UpdateManifolds(direction, bounds, param.Bounds, ref hManifold, ref vManifold);
						_manifolds.Horizontal = hManifold;
						_manifolds.Vertical = vManifold;
					}

					break;
				}
			}

			return bestElement;
		}

		internal void UpdateManifolds(
			FocusNavigationDirection direction,
			Rect elementBounds,
			DependencyObject candidate,
			bool ignoreClipping)
		{
			Rect candidateBounds = XYFocusTreeWalker.GetBoundsForRanking(candidate, ignoreClipping);
			var hManifold = _manifolds.Horizontal;
			var vManifold = _manifolds.Vertical;
			XYFocusAlgorithms.UpdateManifolds(direction, elementBounds, candidateBounds, ref hManifold, ref vManifold);
			_manifolds.Horizontal = hManifold;
			_manifolds.Vertical = vManifold;
		}

		private List<XYFocusParameters> GetAllValidFocusableChildren(
			DependencyObject? startRoot,
			FocusNavigationDirection direction,
			DependencyObject? currentElement,
			DependencyObject? engagedControl,
			DependencyObject? searchScope,
			VisualTree visualTree,
			DependencyObject? activeScroller,
			bool ignoreClipping,
			bool shouldConsiderXYFocusKeyboardNavigation)
		{
			DependencyObject? rootForTreeWalk = startRoot;
			var candidateList = new List<XYFocusParameters>(InitialCandidateListCapacity);
			FocusWalkTraceBegin(direction);

			//If asked to scope the search within the given container, honor it without any exceptions
			if (searchScope != null)
			{
				rootForTreeWalk = searchScope;
			}

			if (engagedControl == null)
			{
				candidateList = XYFocusTreeWalker.FindElements(rootForTreeWalk, currentElement, activeScroller, ignoreClipping, shouldConsiderXYFocusKeyboardNavigation);
			}
			else
			{
				//Only run through this when you are an engaged element. Being an engaged element means that you should only
				//look at the children of the engaged element and any children of popups that were opened during engagement
				//TODO: engagement only happens on Popup root and public root, but should happen on all roots
				var popupChildrenDuringEngagement = PopupRoot.GetPopupChildrenOpenedDuringEngagement(engagedControl);
				candidateList = XYFocusTreeWalker.FindElements(engagedControl, currentElement, activeScroller, ignoreClipping, shouldConsiderXYFocusKeyboardNavigation);

				// Iterate though the popups and add their children to the list
				foreach (var popup in popupChildrenDuringEngagement)
				{
					var subCandidateList = XYFocusTreeWalker.FindElements(popup, currentElement, activeScroller, ignoreClipping, shouldConsiderXYFocusKeyboardNavigation);
					candidateList.AddRange(subCandidateList);
				}

				if (currentElement != engagedControl)
				{
					var bounds = XYFocusTreeWalker.GetBoundsForRanking(engagedControl, ignoreClipping);

					var parameters = new XYFocusParameters();
					parameters.Element = engagedControl;
					parameters.Bounds = bounds;
					candidateList.Add(parameters);
				}
			}

			TraceXYFocusWalkEnd();
			return candidateList;
		}

		private void RankElements(
			List<XYFocusParameters> candidateList,
			FocusNavigationDirection direction,
			Rect bounds,
			double maxRootBoundsDistance,
			XYFocusNavigationStrategy mode,
			Rect? exclusionRect,
			bool ignoreClipping,
			bool ignoreCone)
		{
			Rect exclusionBounds = exclusionRect ?? Rect.Empty;

			for (var i = 0; i < candidateList.Count; i++)
			{
				var candidate = candidateList[i];
				Rect candidateBounds = candidate.Bounds;

				if (!(MathHelpers.DoRectsIntersect(exclusionBounds, candidateBounds)
					|| MathHelpers.DoesRectContainRect(exclusionBounds, candidateBounds)))
				{
					if (mode == XYFocusNavigationStrategy.Projection &&
						XYFocusAlgorithms.ShouldCandidateBeConsideredForRanking(bounds, candidateBounds, maxRootBoundsDistance, direction, exclusionBounds, ignoreCone))
					{
						candidate.Score = _heuristic.GetScore(direction, bounds, candidateBounds, _manifolds.Horizontal, _manifolds.Vertical, maxRootBoundsDistance);
					}
					else if (mode == XYFocusNavigationStrategy.NavigationDirectionDistance || mode == XYFocusNavigationStrategy.RectilinearDistance)
					{
						candidate.Score = ProximityStrategy.GetScore(direction, bounds, candidateBounds, maxRootBoundsDistance, mode == XYFocusNavigationStrategy.RectilinearDistance);
					}
				}
				candidateList[i] = candidate;
			}
		}

		private double GetMaxRootBoundsDistance(
			IReadOnlyList<XYFocusParameters> list,
			Rect bounds,
			FocusNavigationDirection direction,
			bool ignoreClipping)
		{
			var max = list
				.OrderByDescending(param => param, new XYFocusMaxRootBoundComparer(direction))
				.FirstOrDefault();

			var maxBounds = max.Bounds;

			if (direction == FocusNavigationDirection.Left)
			{
				return Math.Abs(maxBounds.Right - bounds.Left);
			}
			else if (direction == FocusNavigationDirection.Right)
			{
				return Math.Abs(bounds.Right - maxBounds.Left);
			}
			else if (direction == FocusNavigationDirection.Up)
			{
				return Math.Abs(bounds.Bottom - maxBounds.Top);
			}
			else if (direction == FocusNavigationDirection.Down)
			{
				return Math.Abs(maxBounds.Bottom - bounds.Top);
			}

			return 0;
		}

		private DependencyObject? GetActiveScrollerForScrollInput(
			FocusNavigationDirection direction,
			DependencyObject? focusedElement)
		{
			DependencyObject? parent = null;
			var textElement = focusedElement as TextElement;
			if (textElement != null)
			{
				parent = textElement.GetContainingFrameworkElement();
			}
			else
			{
				parent = focusedElement;
			}

			while (parent != null)
			{
				var element = parent as UIElement;
				if (element != null && element.IsScroller())
				{
					bool isHorizontallyScrollable = false;
					bool isVerticallyScrollable = false;
					FocusProperties.IsScrollable(element, ref isHorizontallyScrollable, ref isVerticallyScrollable);

					bool isHorizontallyScrollableForDirection = IsHorizontalNavigationDirection(direction) && isHorizontallyScrollable;
					bool isVerticallyScrollableForDirection = IsVerticalNavigationDirection(direction) && isVerticallyScrollable;

					MUX_ASSERT(!(isHorizontallyScrollableForDirection && isVerticallyScrollableForDirection));

					if (isHorizontallyScrollableForDirection || isVerticallyScrollableForDirection)
					{
						return element;
					}
				}

				parent = parent.GetParent() as DependencyObject;
			}
			return null;
		}

		private bool IsHorizontalNavigationDirection(FocusNavigationDirection direction)
		{
			return (direction == FocusNavigationDirection.Left || direction == FocusNavigationDirection.Right);
		}

		private bool IsVerticalNavigationDirection(FocusNavigationDirection direction)
		{
			return (direction == FocusNavigationDirection.Up || direction == FocusNavigationDirection.Down);
		}

#if false
		private void SetPrimaryAxisDistanceWeight(int primaryAxisDistanceWeight) => _heuristic.SetPrimaryAxisDistanceWeight(primaryAxisDistanceWeight);

		private void SetSecondaryAxisDistanceWeight(int secondaryAxisDistanceWeight) => _heuristic.SetSecondaryAxisDistanceWeight(secondaryAxisDistanceWeight);

		private void SetPercentInManifoldShadowWeight(int percentInManifoldShadowWeight) => _heuristic.SetPercentInManifoldShadowWeight(percentInManifoldShadowWeight);

		private void SetPercentInShadowWeight(int percentInShadowWeight) => _heuristic.SetPercentInShadowWeight(percentInShadowWeight);
#endif

		private int ExploredListHash(
			FocusNavigationDirection direction,
			DependencyObject? element,
			DependencyObject? engagedControl,
			XYFocusOptions xyFocusOptions)
		{
			int hash = 17;
			hash = hash * 23 + direction.GetHashCode();
			hash = hash * 23 + (element?.GetHashCode() ?? 0);
			hash = hash * 23 + (engagedControl?.GetHashCode() ?? 0);
			hash = hash * 23 + xyFocusOptions.GetHashCode();
			return hash;
		}

		private void FocusWalkTraceBegin(FocusNavigationDirection direction)
		{
			switch (direction)
			{
				case FocusNavigationDirection.Next:
					TraceXYFocusWalkBegin("Next");
					break;
				case FocusNavigationDirection.Previous:
					TraceXYFocusWalkBegin("Previous");
					break;
				case FocusNavigationDirection.Up:
					TraceXYFocusWalkBegin("Up");
					break;
				case FocusNavigationDirection.Down:
					TraceXYFocusWalkBegin("Down");
					break;
				case FocusNavigationDirection.Left:
					TraceXYFocusWalkBegin("Left");
					break;
				case FocusNavigationDirection.Right:
					TraceXYFocusWalkBegin("Right");
					break;
				default:
					TraceXYFocusWalkBegin("Invalid");
					break;
			}
		}

		private void CacheHitTrace(FocusNavigationDirection direction)
		{
			switch (direction)
			{
				case FocusNavigationDirection.Next:
					TraceXYFocusCandidateCacheHit("Next");
					break;
				case FocusNavigationDirection.Previous:
					TraceXYFocusCandidateCacheHit("Previous");
					break;
				case FocusNavigationDirection.Up:
					TraceXYFocusCandidateCacheHit("Up");
					break;
				case FocusNavigationDirection.Down:
					TraceXYFocusCandidateCacheHit("Down");
					break;
				case FocusNavigationDirection.Left:
					TraceXYFocusCandidateCacheHit("Left");
					break;
				case FocusNavigationDirection.Right:
					TraceXYFocusCandidateCacheHit("Right");
					break;
				default:
					TraceXYFocusCandidateCacheHit("Invalid");
					break;
			}
		}

		private void TraceXYFocusWalkBegin(string direction)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"XYFocus walk begin for direction {direction}");
			}
		}

		private void TraceXYFocusCandidateCacheHit(string direction)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"XYFocus candidate cache hit for direction {direction}");
			}
		}

		private void TraceXYFocusWalkEnd()
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"XYFocus walk ended");
			}
		}
	}
}
