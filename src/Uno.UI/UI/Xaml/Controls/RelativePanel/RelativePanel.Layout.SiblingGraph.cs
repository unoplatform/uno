using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Uno.Extensions;
using Windows.UI.Xaml.Data;
using Uno.UI.Common;
using Windows.Foundation;
using Uno;

#if XAMARIN_ANDROID
using Android.Views;
#elif XAMARIN_IOS
using View = UIKit.UIView;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	partial class RelativePanel
	{
		private class SiblingGraph
		{
			private List<Sibling> _entryPoints = new List<Sibling>();
			private List<Sibling> _nodes = new List<Sibling>();

			/// <summary>
			/// Adds a node to the Sibling Graph.
			/// </summary>
			/// <param name="child">The FrameworkElement to add to the graph</param>
			/// <param name="dependencies">The dependencies this FrameworkElement has</param>
			internal void AddNode(IFrameworkElement child, Dependency[] dependencies)
			{
				var node = new Sibling(child);

				// For each dependency of this node, create the node and create inter-dependencies for the siblings
				foreach (var dependency in dependencies)
				{
					// Find the sibling in the currently existing nodes
					var sibling = _nodes
						.Where(n => n.Element.Equals(dependency.Element))
						.FirstOrDefault();

					// If the sibling is found, set the bi-directional relationship between the siblings
					if (sibling != null)
					{
						node.Dependencies.Add(new SiblingDependency(sibling, dependency.Type));
						sibling.Dependencies.Add(new SiblingDependency(node, GetOppositeDependency(dependency.Type), true));
					}
				}

				// If the node has no dependencies, it is considered to be a graph entry point
				if (dependencies.Length == 0)
				{
					_entryPoints.Add(node);
				}

				_nodes.Add(node);
			}

			/// <summary>
			/// Gets all the FrameworkElements in the graph, starting from the entry points of the graph and then navigating through the graph
			/// until all branches have been explored, then goes to the next entry point and so on.
			/// </summary>
			internal IEnumerable<IEnumerable<IFrameworkElement>> GetClusters(bool isHorizontallyInfinite, bool isVerticallyInfinite)
			{
				var remaining = new List<Sibling>(_nodes);
				var remainingEntryPoints = new List<Sibling>(_entryPoints);
				var clusters = new List<IEnumerable<IFrameworkElement>>();

				// Traverse the graph until there are no remaining children
				while (remaining.Count != 0)
				{
					if (remainingEntryPoints.Count != 0)
					{
						var entryPoint = remainingEntryPoints[0];

						if (remaining.Contains(entryPoint))
						{
							var cluster = new List<Sibling>();

							remaining.Remove(entryPoint);
							cluster.Add(entryPoint);

							var enumerator = entryPoint.GetSiblings().GetEnumerator();

							while (enumerator.MoveNext())
							{
								var current = remaining.FirstOrDefault(n => n.Element.Equals(enumerator.Current));

								if (current != null)
								{
									remaining.Remove(current);
									cluster.Add(current);
								}
							}

							// Make sure dependencies are prepared for ordering
							CleanupDependencies(cluster, isHorizontallyInfinite, isVerticallyInfinite);

							// Set cluster elements, start of with the ones that are top-left, then aligned to any panel boundary and move to the others
							var orderedCluster = OrderClusterForLayouting(cluster);
							clusters.Add(orderedCluster);
						}

						remainingEntryPoints.Remove(entryPoint);
					}
					else
					{
						// This shouldn't happen but was added to prevent endless loops.  If this happens, an items somehow was considered not to be
						// part of an existing graph or an entry point.  It should be considered a bug.
						throw new InvalidOperationException("A RelativePanel child was left out of the Sibling Graph.");
					}
				}

				return clusters;
			}

			/// <summary>
			/// Makes sure dependencies in the cluster are reversed if they are based on unknown boundaries
			/// </summary>
			private void CleanupDependencies(List<Sibling> cluster, bool isHorizontallyInfinite, bool isVerticallyInfinite)
			{
				if (!isHorizontallyInfinite && !isVerticallyInfinite)
				{
					return;
				}

				foreach (var sibling in cluster)
				{
					if (isHorizontallyInfinite && RelativePanel.GetAlignRightWithPanel(sibling.Element))
					{
						ReverseDependencies(sibling, true);
					}

					if (isVerticallyInfinite && RelativePanel.GetAlignBottomWithPanel(sibling.Element))
					{
						ReverseDependencies(sibling, false);
					}
				}
			}

			/// <summary>
			/// Reverses which dependencies are inferred or not in a certain orientation
			/// </summary>
			private void ReverseDependencies(Sibling sibling, bool isHorizontal)
			{
				foreach (SiblingDependency dependency in sibling.Dependencies)
				{
					if (!dependency.IsInferred)
					{
						continue;
					}

					if (isHorizontal && (dependency.Type == DependencyType.RightOf || dependency.Type == DependencyType.AlignLeftWith))
					{
						dependency.IsInferred = false;
						var firstSibling = dependency.Sibling.Dependencies
							.First(sd => sd.Sibling.Equals(sibling) && sd.Type.Equals(GetOppositeDependency(dependency.Type)));

						if (firstSibling != null)
						{
							firstSibling.IsInferred = true;
						}

						ReverseDependencies(dependency.Sibling, isHorizontal);
					}
					else if (!isHorizontal && (dependency.Type == DependencyType.Below || dependency.Type == DependencyType.AlignTopWith))
					{
						dependency.IsInferred = false;
						var firstSibling = dependency.Sibling.Dependencies
							.First(sd => sd.Sibling.Equals(sibling) && sd.Type.Equals(GetOppositeDependency(dependency.Type)));

						if (firstSibling != null)
						{
							firstSibling.IsInferred = true;
						}

						ReverseDependencies(dependency.Sibling, isHorizontal);
					}
				}
			}

			/// <summary>
			/// Prioritizes elements for layouting.  First treat the entrypoints, starting from top-left then around.
			/// Then select the ones with dependencies that are already laid out.
			/// </summary>
			/// <param name="cluster"></param>
			/// <returns></returns>
			private IFrameworkElement[] OrderClusterForLayouting(List<Sibling> cluster)
			{
				// Give weight to panel alignments.  Weight 3 for top or left so that a left-aligned only
				// element (3) has precedence over a bottom-right aligned element (1+1)
				var orderedByAlignment = new List<Sibling>(cluster
					.OrderByDescending(s =>
						(IsAlignLeftWithPanel(s.Element) ? 3 : 0) +
						(IsAlignTopWithPanel(s.Element) ? 3 : 0) +
						(RelativePanel.GetAlignRightWithPanel(s.Element) ? 1 : 0) +
						(RelativePanel.GetAlignBottomWithPanel(s.Element) ? 1 : 0)
					)
				);

				var ordered = new List<Sibling>();

				while (orderedByAlignment.Count != 0)
				{
					// Take the first item in the previous list that has no direct dependency that have not already been selected
					var next = orderedByAlignment.First(s => s.Dependencies
						.Where(sd => !sd.IsInferred)
						.Select(sd => sd.Sibling)
						.Except(ordered)
						.Empty()
					);

					orderedByAlignment.Remove(next);
					ordered.Add(next);
				}

				return ordered
					.SelectToArray(s => s.Element);
			}

			/// <summary>
			/// Gets the Sibling structure associated to a particular FrameworkElement
			/// </summary>
			internal Sibling GetSibling(IFrameworkElement element)
			{
				return _nodes.FirstOrDefault(s => s.Element.Equals(element));
			}

			/// <summary>
			/// Measures all the siblings in the graph and returns the minimal required size to display all siblings
			/// </summary>
			/// <param name="availableSize">The available size for the siblings to be displayed in</param>
			/// <param name="measureChild">The method to call in order to measure one child</param>
			/// <returns></returns>
			internal Size MeasureSiblings(Size availableSize, Thickness padding, Func<View, Size, Size> measureChild)
			{
				var clusters = GetClusters(double.IsPositiveInfinity(availableSize.Width), double.IsPositiveInfinity(availableSize.Height));
				var finalSize = new Size();

				var measureChildCached = CacheMeasureFunc(measureChild);

				foreach (var cluster in clusters)
				{
					var siblingLayoutInfos = new Dictionary<IFrameworkElement, SiblingLayoutInfo>(Uno.UI.Helpers.IFrameworkElementReferenceEqualityComparer.Default);

					foreach (var child in cluster)
					{
						PrepareAndMeasureChild(child, availableSize, padding, measureChildCached, siblingLayoutInfos);
					}

					foreach (var child in cluster)
					{
						// Update the final size of the panel with the new layout info
						finalSize = ExecuteOnSiblingLayoutInfoIfAvailable(child, siblingLayoutInfos, sli => UpdateFinalMeasuredSize(sli.Area, finalSize, availableSize, padding));
					}
				}

				return finalSize;
			}

			/// <summary>
			/// Prepares a child for a measuring pass and sets up this child's sibling layout info and measures it when required
			/// </summary>
			/// <param name="child">The child to prepare for measure</param>
			/// <param name="availableSize">The available size in the panel</param>
			/// <param name="padding">The panel's padding</param>
			/// <param name="measureChild">The method to call when we need to measure the child</param>
			/// <param name="siblingLayoutInfos">The set of SiblingLayoutInfos from the child's siblings</param>
			/// <param name="updateCallerStack">The stack of all siblings that caused a remeasure of their dependencies, null at first pass</param>
			private void PrepareAndMeasureChild(
				IFrameworkElement child,
				Size availableSize,
				Thickness padding,
				Func<View, Size, Size> measureChild,
				Dictionary<IFrameworkElement, SiblingLayoutInfo> siblingLayoutInfos,
				List<IFrameworkElement> updateCallerStack = null)
			{
				bool needsCorrections = false;

				// Get the available area for the children, based on the currently laid out siblings and on the available size in the panel
				var areaForChild = GetAvailableAreaForChild(child, availableSize, siblingLayoutInfos, padding);

				var horizontalMargin = child.Margin.Left + child.Margin.Right;
				var verticalMargin = child.Margin.Top + child.Margin.Bottom;

				// If the area is smaller than the requiredSize, make sure it has enough space and raise a flag to correct errors afterward
				if ((!double.IsNaN(child.Width) && areaForChild.Width < (child.Width + horizontalMargin)) ||
					areaForChild.Width < (child.MinWidth + horizontalMargin) ||
					(!double.IsNaN(child.Height) && areaForChild.Height < (child.Height + verticalMargin)) ||
					areaForChild.Height < (child.MinHeight + verticalMargin))
				{
					needsCorrections = true;

					areaForChild.Width = Math.Max(Math.Max(double.IsNaN(child.Width) ? 0 : child.Width, child.MinWidth) + horizontalMargin, areaForChild.Width);
					areaForChild.Height = Math.Max(Math.Max(double.IsNaN(child.Height) ? 0 : child.Height, child.MinHeight) + verticalMargin, areaForChild.Height);
				}

				// Measure the child with the available size
				var childSize = measureChild(child as View, areaForChild.Size);

				// Get the area (including location) of the child based on the measured size and the layout information of other children in order
				var childArea = ExecuteOnSiblingLayoutInfoIfAvailable(
					child,
					siblingLayoutInfos,
					sli => ComputeChildArea(areaForChild, childSize, sli)
				);

				var wasSet = ExecuteOnSiblingLayoutInfoIfAvailable(child, siblingLayoutInfos, sli => sli.IsAreaSet);

				// Update the Layout info with the newly found size
				var changed = ExecuteOnSiblingLayoutInfoIfAvailable(child, siblingLayoutInfos, sli =>
				{
					if (sli.Area.Equals(childArea))
					{
						return false;
					}

					sli.Area = childArea;

					return true;
				});

				// If needed, correct the siblings based on the newlyfound forced size
				if ((needsCorrections && changed) || (wasSet && changed))
				{
					var callerStack = updateCallerStack ?? new List<IFrameworkElement>();
					callerStack.Add(child);

					UpdateSiblingsBasedOnSize(
						child,
						availableSize,
						padding,
						measureChild,
						siblingLayoutInfos,
						callerStack
					);
				}
			}

			/// <summary>
			/// Re-measures the siblings that are affected by a change in size of a dependent sibling
			/// </summary>
			/// <param name="child">The child for which the size changed</param>
			/// <param name="availableSize">The panel's available size</param>
			/// <param name="padding">The panel's padding</param>
			/// <param name="measureChild">The method to call when we need to measure a child</param>
			/// <param name="siblingLayoutInfos">The set of sibling layout information of the currently laid out siblings</param>
			/// <param name="updateCallerStack">The stack of all IFrameworkElements that caused the chain of updates</param>
			private void UpdateSiblingsBasedOnSize(
				IFrameworkElement child,
				Size availableSize,
				Thickness padding,
				Func<View, Size, Size> measureChild,
				Dictionary<IFrameworkElement, SiblingLayoutInfo> siblingLayoutInfos,
				List<IFrameworkElement> updateCallerStack
			)
			{
				var sibling = GetSibling(child);

				foreach (var dependency in sibling.Dependencies.Select(d => d.Sibling.Element).Distinct())
				{
					// If element has already been laid out
					if (siblingLayoutInfos.ContainsKey(dependency) && !updateCallerStack.Contains(dependency))
					{
						// Re-measure the child based on the new info
						PrepareAndMeasureChild(dependency, availableSize, padding, measureChild, siblingLayoutInfos, updateCallerStack);
					}
				}
			}

			/// <summary>
			/// Arranges all the siblings in the graph and lays them out in the available space, relative to their parent
			/// </summary>
			/// <param name="finalSize">The size that is available for the layout</param>
			/// <param name="arrangeChild">The method to call in order to arrange one specific child</param>
			internal void ArrangeSiblings(Size finalSize, Thickness padding, Func<View, Size> getDesiredChildSize, Action<View, Rect> arrangeChild)
			{
				var clusters = GetClusters(double.IsPositiveInfinity(finalSize.Width), double.IsPositiveInfinity(finalSize.Height));

				foreach (var cluster in clusters)
				{
					var siblingLayoutInfos = new Dictionary<IFrameworkElement, SiblingLayoutInfo>(Uno.UI.Helpers.IFrameworkElementReferenceEqualityComparer.Default);

					foreach (var child in cluster)
					{
						PrepareChildForArrange(child, finalSize, padding, getDesiredChildSize, siblingLayoutInfos);
					}

					foreach (var child in cluster)
					{
						var sli = siblingLayoutInfos[child];

						if (sli != null)
						{
							arrangeChild(child as View, sli.Area);
						}
					}
				}
			}

			private void PrepareChildForArrange(IFrameworkElement child, Size finalSize, Thickness padding, Func<View, Size> getDesiredChildSize, Dictionary<IFrameworkElement, SiblingLayoutInfo> siblingLayoutInfos)
			{
				// Get the available area for the children, based on the currently laid out siblings and on the available size in the panel
				var areaForChild = GetAvailableAreaForChild(child, finalSize, siblingLayoutInfos, padding);
				var desiredSize = getDesiredChildSize(child as View);
				var childArea = areaForChild;

				// If the child stretches horizontally or vertically, keep the child's location, but update its height or width
				if (!ExecuteOnSiblingLayoutInfoIfAvailable(child, siblingLayoutInfos, sli => sli.StretchesHorizontally))
				{
					childArea = AlterRectangleSize(childArea, Math.Min(childArea.Width, desiredSize.Width), childArea.Height);
				}

				if (!ExecuteOnSiblingLayoutInfoIfAvailable(child, siblingLayoutInfos, sli => sli.StretchesVertically))
				{
					childArea = AlterRectangleSize(childArea, childArea.Width, Math.Min(childArea.Height, desiredSize.Height));
				}

				// Get the area of the child based on the measured size and the layout information of other children in order (correct position based on information)
				childArea = ExecuteOnSiblingLayoutInfoIfAvailable(child, siblingLayoutInfos, sli => ComputeChildArea(areaForChild, childArea.Size, sli));

				// Update the Layout info with the newly found size
				var areaChanged = ExecuteOnSiblingLayoutInfoIfAvailable(child, siblingLayoutInfos, sli =>
				{
					if (!sli.Area.Equals(childArea))
					{
						sli.Area = childArea;
						return true;
					}

					return false;
				});

				// Retropropagate the changes to the siblings that are bound to this child
				if (areaChanged)
				{
					UpdateSiblingsBasedOnArea(child, finalSize, padding, getDesiredChildSize, siblingLayoutInfos);
				}
			}

			/// <summary>
			/// Gets the available area for the children, based on the currently laid out siblings and on the available size in the panel
			/// </summary>
			/// <param name="child">The child for which we want to find out the available area</param>
			/// <param name="availableSize">The panel's available size</param>
			/// <param name="siblingLayoutInfos">The information about the currently laid out siblings</param>
			/// <returns></returns>
			private Rect GetAvailableAreaForChild(IFrameworkElement child, Size availableSize, Dictionary<IFrameworkElement, SiblingLayoutInfo> siblingLayoutInfos, Thickness graphPadding)
			{
				// Compute each boundaries that can determined for the current children based in the known information
				var leftBound = ComputeChildLeftBound(child, availableSize, siblingLayoutInfos, graphPadding);
				var topBound = ComputeChildTopBound(child, availableSize, siblingLayoutInfos, graphPadding);
				var rightBound = ComputeChildRightBound(child, availableSize, siblingLayoutInfos, leftBound, graphPadding);
				var bottomBound = ComputeChildBottomBound(child, availableSize, siblingLayoutInfos, topBound, graphPadding);

				// Make final adjustments to the available area for the child
				return new Rect(leftBound, topBound, rightBound - leftBound, bottomBound - topBound);
			}

			/// <summary>
			/// Checks if the given FrameworkElement has any dependencies that would provide a left boundary using the existing layout info
			/// </summary>
			private double ComputeChildLeftBound(
				IFrameworkElement child,
				Size availableSize,
				Dictionary<IFrameworkElement, SiblingLayoutInfo> siblingLayoutInfos,
				Thickness graphPadding,
				bool useInferred = false
			)
			{
				var rightOf = GetAvailableDependencies(GetSibling(child), DependencyType.RightOf, useInferred, siblingLayoutInfos);
				if (rightOf.Length != 0)
				{
					ExecuteOnSiblingLayoutInfoIfAvailable(child, siblingLayoutInfos, sli => sli.IsLeftBound = !useInferred);
					return rightOf.Max(d => siblingLayoutInfos[d.Sibling.Element].Area.Right);
				}

				var leftAlign = GetAvailableDependencies(GetSibling(child), DependencyType.AlignLeftWith, useInferred, siblingLayoutInfos);
				if (leftAlign.Length != 0)
				{
					ExecuteOnSiblingLayoutInfoIfAvailable(child, siblingLayoutInfos, sli => sli.IsLeftBound = !useInferred);
					return leftAlign.Max(d => siblingLayoutInfos[d.Sibling.Element].Area.Left);
				}

				var centerAlign = GetAvailableDependencies(GetSibling(child), DependencyType.AlignHorizontalCenterWith, useInferred, siblingLayoutInfos);
				if (centerAlign.Length != 0)
				{
					var center = centerAlign.Average(d =>
					{
						var sibling = siblingLayoutInfos[d.Sibling.Element];
						return (sibling.Area.Left + sibling.Area.Right) / 2;
					});
					ExecuteOnSiblingLayoutInfoIfAvailable(child, siblingLayoutInfos, sli => sli.Center = new Point(center, sli.Center.Y));
				}

				if (IsAlignLeftWithPanel(child))
				{
					ExecuteOnSiblingLayoutInfoIfAvailable(child, siblingLayoutInfos, sli => sli.IsLeftBound = true);
				}

				if (GetAlignHorizontalCenterWithPanel(child))
				{
					var spacing = child.Margin.Left + child.Margin.Right;
					var size = double.IsNaN(child.Width) ?
							Math.Max(
								Math.Min(
									availableSize.Width - graphPadding.Left - graphPadding.Right,
									child.MaxWidth + spacing
								),
								child.MinWidth + spacing
							) :
							child.Width + spacing;

					return (availableSize.Width - spacing - size) / 2;
				}

				return useInferred ? graphPadding.Left : ComputeChildLeftBound(child, availableSize, siblingLayoutInfos, graphPadding, true);
			}

			/// <summary>
			/// Returns true if the child is left aligned with Panel or has no horizontal alignment instructions
			/// </summary>
			private bool IsAlignLeftWithPanel(IFrameworkElement child)
			{
				return RelativePanel.GetAlignLeftWithPanel(child) ||
					!(
						RelativePanel.GetAlignRightWithPanel(child) ||
						RelativePanel.GetAlignHorizontalCenterWithPanel(child) ||
						RelativePanel.GetAlignLeftWith(child) != null ||
						RelativePanel.GetAlignRightWith(child) != null ||
						RelativePanel.GetRightOf(child) != null ||
						RelativePanel.GetLeftOf(child) != null
					);
			}

			/// <summary>
			/// Checks if the given FrameworkElement has any dependencies that would provide a top boundary using the existing layout info
			/// </summary>
			private double ComputeChildTopBound(
				IFrameworkElement child,
				Size availableSize,
				Dictionary<IFrameworkElement, SiblingLayoutInfo> siblingLayoutInfos,
				Thickness graphPadding,
				bool useInferred = false
			)
			{
				var below = GetAvailableDependencies(GetSibling(child), DependencyType.Below, useInferred, siblingLayoutInfos);
				if (below.Length != 0)
				{
					ExecuteOnSiblingLayoutInfoIfAvailable(child, siblingLayoutInfos, sli => sli.IsTopBound = !useInferred);
					return below.Max(d => siblingLayoutInfos[d.Sibling.Element].Area.Bottom);
				}

				var topAlign = GetAvailableDependencies(GetSibling(child), DependencyType.AlignTopWith, useInferred, siblingLayoutInfos);
				if (topAlign.Length != 0)
				{
					ExecuteOnSiblingLayoutInfoIfAvailable(child, siblingLayoutInfos, sli => sli.IsTopBound = !useInferred);
					return topAlign.Max(d => siblingLayoutInfos[d.Sibling.Element].Area.Top);
				}

				var centerAlign = GetAvailableDependencies(GetSibling(child), DependencyType.AlignVerticalCenterWith, useInferred, siblingLayoutInfos);
				if (centerAlign.Length != 0)
				{
					var center = centerAlign.Average(d =>
					{
						var sibling = siblingLayoutInfos[d.Sibling.Element];
						return (sibling.Area.Top + sibling.Area.Bottom) / 2;
					});
					ExecuteOnSiblingLayoutInfoIfAvailable(child, siblingLayoutInfos, sli => sli.Center = new Point(sli.Center.X, center));
				}

				if (IsAlignTopWithPanel(child))
				{
					ExecuteOnSiblingLayoutInfoIfAvailable(child, siblingLayoutInfos, sli => sli.IsTopBound = true);
				}

				if (GetAlignVerticalCenterWithPanel(child))
				{
					var spacing = child.Margin.Top + child.Margin.Bottom;
					var size = double.IsNaN(child.Height) ?
							Math.Max(
								Math.Min(
									availableSize.Height - graphPadding.Top - graphPadding.Bottom,
									child.MaxHeight + spacing
								),
								child.MinHeight + spacing
							) :
							child.Height + spacing;

					return (availableSize.Height - spacing - size) / 2;
				}

				return useInferred ? graphPadding.Top : ComputeChildTopBound(child, availableSize, siblingLayoutInfos, graphPadding, true);
			}

			/// <summary>
			/// Returns true if the child is top aligned with Panel or has no vertical alignment instructions
			/// </summary>
			private bool IsAlignTopWithPanel(IFrameworkElement child)
			{
				return RelativePanel.GetAlignTopWithPanel(child) ||
					!(
						RelativePanel.GetAlignBottomWithPanel(child) ||
						RelativePanel.GetAlignVerticalCenterWithPanel(child) ||
						RelativePanel.GetAlignTopWith(child) != null ||
						RelativePanel.GetAlignBottomWith(child) != null ||
						RelativePanel.GetAbove(child) != null ||
						RelativePanel.GetBelow(child) != null
					);
			}

			/// <summary>
			/// Checks if the given FrameworkElement has any dependencies that would provide a right boundary using the existing layout info
			/// </summary>
			private double ComputeChildRightBound(
				IFrameworkElement child,
				Size availableSize,
				Dictionary<IFrameworkElement, SiblingLayoutInfo> siblingLayoutInfos,
				double childLeft,
				Thickness graphPadding,
				bool useInferred = false
			)
			{
				var leftOf = GetAvailableDependencies(GetSibling(child), DependencyType.LeftOf, useInferred, siblingLayoutInfos);
				if (leftOf.Length != 0)
				{
					ExecuteOnSiblingLayoutInfoIfAvailable(child, siblingLayoutInfos, sli => sli.IsRightBound = !useInferred);
					return leftOf.Min(d => siblingLayoutInfos[d.Sibling.Element].Area.Left);
				}

				var rightAlign = GetAvailableDependencies(GetSibling(child), DependencyType.AlignRightWith, useInferred, siblingLayoutInfos);
				if (rightAlign.Length != 0)
				{
					ExecuteOnSiblingLayoutInfoIfAvailable(child, siblingLayoutInfos, sli => sli.IsRightBound = !useInferred);
					return rightAlign.Min(d => siblingLayoutInfos[d.Sibling.Element].Area.Right);
				}

				if (RelativePanel.GetAlignRightWithPanel(child))
				{
					ExecuteOnSiblingLayoutInfoIfAvailable(child, siblingLayoutInfos, sli => sli.IsRightBound = true);
					return availableSize.Width - graphPadding.Right;
				}

				if (!useInferred)
				{
					return ComputeChildRightBound(child, availableSize, siblingLayoutInfos, childLeft, graphPadding, true);
				}

				// If there is no dependency, base yourself off of the available width, margins and Width/Min/Max properties
				var spacing = childLeft + child.Margin.Left + child.Margin.Right;
				return double.IsNaN(child.Width) ?
							Math.Max(
								Math.Min(
									availableSize.Width - graphPadding.Right,
									child.MaxWidth + spacing
								),
								child.MinWidth + spacing
							) :
							child.Width + spacing;
			}

			/// <summary>
			/// Checks if the given FrameworkElement has any dependencies that would provide a bottom boundary using the existing layout info
			/// </summary>
			private double ComputeChildBottomBound(
				IFrameworkElement child,
				Size availableSize,
				Dictionary<IFrameworkElement, SiblingLayoutInfo> siblingLayoutInfos,
				double childTop,
				Thickness graphPadding,
				bool useInferred = false
			)
			{
				var above = GetAvailableDependencies(GetSibling(child), DependencyType.Above, useInferred, siblingLayoutInfos);
				if (above.Length != 0)
				{
					ExecuteOnSiblingLayoutInfoIfAvailable(child, siblingLayoutInfos, sli => sli.IsBottomBound = !useInferred);
					return above.Min(d => siblingLayoutInfos[d.Sibling.Element].Area.Top);
				}

				var bottomAlign = GetAvailableDependencies(GetSibling(child), DependencyType.AlignBottomWith, useInferred, siblingLayoutInfos);
				if (bottomAlign.Length != 0)
				{
					ExecuteOnSiblingLayoutInfoIfAvailable(child, siblingLayoutInfos, sli => sli.IsBottomBound = !useInferred);
					return bottomAlign.Min(d => siblingLayoutInfos[d.Sibling.Element].Area.Bottom);
				}

				if (RelativePanel.GetAlignBottomWithPanel(child))
				{
					ExecuteOnSiblingLayoutInfoIfAvailable(child, siblingLayoutInfos, sli => sli.IsBottomBound = true);
					return availableSize.Height - graphPadding.Bottom;
				}

				if (!useInferred)
				{
					ComputeChildBottomBound(child, availableSize, siblingLayoutInfos, childTop, graphPadding, true);
				}

				// If there is no dependency, base yourself off of the available height, margins and Height/Min/Max properties
				var spacing = childTop + child.Margin.Top + child.Margin.Bottom;
				return double.IsNaN(child.Height) ?
							Math.Max(
								Math.Min(
									availableSize.Height - graphPadding.Bottom,
									child.MaxHeight + spacing
								),
								child.MinHeight + spacing
							) :
							child.Height + spacing;

			}

			/// <summary>
			/// Gets already laid out FrameworkElements that have a given dependency type in relation to this sibling and where indeference is appropriate
			/// </summary>
			private SiblingDependency[] GetAvailableDependencies(Sibling element, DependencyType dependencyType, bool useInferred, IDictionary<IFrameworkElement, SiblingLayoutInfo> availableSiblings)
			{
				return element.Dependencies
					.Where(d => (d.Type == dependencyType) && (d.IsInferred == useInferred) && (availableSiblings.ContainsKey(d.Sibling.Element)))
					.ToArray();
			}

			/// <summary>
			/// Get the area (including location) of a child based on the measured size and the layout information of other children in order
			/// </summary>
			/// <param name="availableArea">The child's available area</param>
			/// <param name="childSize">The measured child size</param>
			/// <param name="siblingLayoutInfo">The information about the previously laid out children</param>
			private Rect ComputeChildArea(Rect availableArea, Size childSize, SiblingLayoutInfo siblingLayoutInfo)
			{
				var location = availableArea.Location;

				if (siblingLayoutInfo.IsRightBound && !siblingLayoutInfo.IsLeftBound && !double.IsPositiveInfinity(availableArea.Right))
				{
					location.X = availableArea.Right - childSize.Width;
				}
				else if (!double.IsNaN(siblingLayoutInfo.Center.X))
				{
					location.X = siblingLayoutInfo.Center.X - (childSize.Width / 2);
				}

				if (siblingLayoutInfo.IsBottomBound && !siblingLayoutInfo.IsTopBound && !double.IsPositiveInfinity(availableArea.Bottom))
				{
					location.Y = availableArea.Bottom - childSize.Height;
				}
				else if (!double.IsNaN(siblingLayoutInfo.Center.Y))
				{
					location.Y = siblingLayoutInfo.Center.Y - (childSize.Height / 2);
				}

				return new Rect(location, childSize);
			}

			/// <summary>
			/// Update the final size of the panel with the new layout info
			/// </summary>
			/// <param name="childArea">The area of the newly measured child</param>
			/// <param name="currentFinalMeasuredSize">The current size of the panel, based on the previous children</param>
			/// <param name="availableSize">The available size for the panel</param>
			private Size UpdateFinalMeasuredSize(Rect childArea, Size currentFinalMeasuredSize, Size availableSize, Thickness graphPadding)
			{
				return new Size(
					double.IsPositiveInfinity(childArea.Right) ?
						Math.Min(Math.Max(childArea.Left + childArea.Width + graphPadding.Right, currentFinalMeasuredSize.Width), availableSize.Width) :
						Math.Min(Math.Max(childArea.Right + graphPadding.Right, currentFinalMeasuredSize.Width), availableSize.Width),
					double.IsPositiveInfinity(childArea.Bottom) ?
						Math.Min(Math.Max(childArea.Top + childArea.Height + graphPadding.Bottom, currentFinalMeasuredSize.Height), availableSize.Height) :
						Math.Min(Math.Max(childArea.Bottom + graphPadding.Bottom, currentFinalMeasuredSize.Height), availableSize.Height)
					);
			}

			/// <summary>
			/// After updating the size of a child, find the related siblings and make sure their position is updated accordingly
			/// </summary>
			private void UpdateSiblingsBasedOnArea(IFrameworkElement child, Size finalSize, Thickness padding, Func<View, Size> getDesiredChildSize, Dictionary<IFrameworkElement, SiblingLayoutInfo> siblingLayoutInfos)
			{
				var sibling = GetSibling(child);

				foreach (var dependency in sibling.Dependencies.Select(d => d.Sibling.Element).Distinct())
				{
					// If element has already been laid out
					if (siblingLayoutInfos.ContainsKey(dependency))
					{
						// Re-prepare the child based on the new information
						PrepareChildForArrange(dependency, finalSize, padding, getDesiredChildSize, siblingLayoutInfos);
					}
				}
			}

			/// <summary>
			/// Executes an operation on an already existing SiblingLayoutInfo or create it before executing.
			/// </summary>
			private T ExecuteOnSiblingLayoutInfoIfAvailable<T>(IFrameworkElement child, Dictionary<IFrameworkElement, SiblingLayoutInfo> siblingLayoutInfos, Func<SiblingLayoutInfo, T> operation)
			{
				if (!siblingLayoutInfos.ContainsKey(child))
				{
					siblingLayoutInfos[child] = new SiblingLayoutInfo();
				}

				return operation(siblingLayoutInfos[child]);
			}

			/// <summary>
			/// Gets the Dependency Type for a sibling of a particular dependency (E.G. LeftOf would return RightOf, AlignLeftWith will remain AlignLeftWith)
			/// </summary>
			private DependencyType GetOppositeDependency(DependencyType value)
			{
				switch (value)
				{
					case DependencyType.Above:
						return DependencyType.Below;
					case DependencyType.LeftOf:
						return DependencyType.RightOf;
					case DependencyType.Below:
						return DependencyType.Above;
					case DependencyType.RightOf:
						return DependencyType.LeftOf;
					default:
						return value;
				}
			}

			private static Rect AlterRectangleSize(Rect rectangle, double width, double height)
			{
				return new Rect(rectangle.Location, new Size(width, height));
			}

			private static Func<View, Size, Size> CacheMeasureFunc(Func<View, Size, Size> measureChild)
			{
				Dictionary<View, Dictionary<Size, Size>> values = new Dictionary<View, Dictionary<Size, Size>>(ReferenceEqualityComparer<View>.Default);

				return (view, size) =>
				{
					Dictionary<Size, Size> dictionary;

					if (!values.TryGetValue(view, out dictionary))
					{
						dictionary = values[view] = new Dictionary<Size, Size>();
					}

					Size value;
					var candidates = dictionary
						// Where the availableSize was bigger or equal to the one we have and
						// where the resulting size was smaller or equal to the available size we now have
						.Where(kvp =>
							kvp.Key.Width >= size.Width &&
							kvp.Key.Height >= size.Height &&
							kvp.Value.Width <= size.Width &&
							kvp.Value.Height <= size.Height
						);

					if (candidates.Empty())
					{
						value = dictionary[size] = measureChild(view, size);
					}
					else
					{
						// Select the biggest of the available values to make sure we have enough room
						value = candidates
							.Select(kvp => kvp.Value)
							.OrderByDescending(s => s.Width * s.Height)
							.First();
					}

					return value;
				};
			}
		}

		private class SiblingLayoutInfo
		{
			private Rect _area;

			/// <summary>
			/// Defines if the SiblingLayoutInfo's Area has been set once
			/// </summary>
			public bool IsAreaSet { get; set; }

			/// <summary>
			/// Defines the area in which the sibling is laid out
			/// </summary>
			public Rect Area
			{
				get
				{
					return _area;
				}
				set
				{
					IsAreaSet = true;
					_area = value;
				}
			}

			/// <summary>
			/// Defines if the sibling is bound to the left, either with a sibling or with the panel
			/// </summary>
			public bool IsLeftBound { get; set; }

			/// <summary>
			/// Defines if the sibling is bound to the top, either with a sibling or with the panel
			/// </summary>
			public bool IsTopBound { get; set; }

			/// <summary>
			/// Defines if the sibling is bound to the right, either with a sibling or with the panel
			/// </summary>
			public bool IsRightBound { get; set; }

			/// <summary>
			/// Defines if the sibling is bound to the bottom, either with a sibling or with the panel
			/// </summary>
			public bool IsBottomBound { get; set; }

			/// <summary>
			/// Returns true if the sibling is bound both left and right
			/// </summary>
			public bool StretchesHorizontally
			{
				get
				{
					return this.IsLeftBound && this.IsRightBound;
				}
			}

			/// <summary>
			/// Returns true if the sibling is bound both top and bottom
			/// </summary>
			public bool StretchesVertically
			{
				get
				{
					return this.IsTopBound && this.IsBottomBound;
				}
			}

			/// <summary>
			/// Returns the X and Y pair around which the sibling is being centered.
			/// </summary>
			public Point Center { get; set; } = new Point(double.NaN, double.NaN);
		}

		private class Sibling
		{
			public Sibling(IFrameworkElement element)
			{
				Element = element;
				Dependencies = new List<SiblingDependency>();
			}

			public List<SiblingDependency> Dependencies { get; private set; }

			public IFrameworkElement Element { get; private set; }

			/// <summary>
			/// Returns all the siblings that are related to a particular FrameworkElement, going through each dependency
			/// and then trough the siblings of those dependencies.
			/// </summary>
			/// <param name="except">A list of FrameworkElements to avoid while enumerating.  This enables us to prevent redundancy in the enumeration of the graph.</param>
			public IEnumerable<IFrameworkElement> GetSiblings(IFrameworkElement[] except = null)
			{
				var siblings = Dependencies.SelectToArray(d => d.Sibling);
				var ignored = new List<IFrameworkElement>();

				if (except != null)
				{
					ignored.AddRange(except);
				}

				foreach (var sibling in siblings)
				{
					var element = sibling.Element;

					if (!ignored.Contains(element))
					{
						ignored.Add(element);

						yield return element;

						foreach (var coSibling in sibling.GetSiblings(ignored.ToArray()))
						{
							ignored.Add(coSibling);

							yield return coSibling;
						}
					}
				}

				yield break;
			}
		}

		private class SiblingDependency
		{
			/// <summary>
			/// Constructor
			/// </summary>
			/// <param name="sibling">The sibling on which the element is bound</param>
			/// <param name="type">The type of dependency</param>
			/// <param name="isInferred">True if the dependency is inferred (is the inverse of an actual dependency)</param>
			public SiblingDependency(Sibling sibling, DependencyType type, bool isInferred = false)
			{
				Sibling = sibling;
				Type = type;
				IsInferred = isInferred;
			}

			/// <summary>
			/// The sibling on which the element is bound
			/// </summary>
			public Sibling Sibling { get; set; }

			/// <summary>
			/// The type of dependency for the sibling
			/// </summary>
			public DependencyType Type { get; set; }

			/// <summary>
			/// True if the dependency is inferred (is the inverse of an actual dependency)
			/// </summary>
			public bool IsInferred { get; set; }
		}
	}
}
