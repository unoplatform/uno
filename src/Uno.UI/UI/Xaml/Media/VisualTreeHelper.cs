#nullable disable // Not supported by WinUI yet
//#define TRACE_HIT_TESTING

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Uno.Collections;

using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;
using static Uno.Extensions.Matrix3x2Extensions;
using static Uno.Extensions.EnumerableExtensions;

#if TRACE_HIT_TESTING
using System.Runtime.CompilerServices;
using System.Text;
using Uno.Disposables;
#endif

using _View = Microsoft.UI.Xaml.UIElement;
using _ViewGroup = Microsoft.UI.Xaml.UIElement;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Shapes;

namespace Microsoft.UI.Xaml.Media
{
	public partial class VisualTreeHelper
	{
		[Uno.NotImplemented]
		public static void DisconnectChildrenRecursive(UIElement element)
		{
			throw new NotSupportedException();
		}

		public static IEnumerable<UIElement> FindElementsInHostCoordinates(Point intersectingPoint, UIElement/* ? */ subtree)
			=> FindElementsInHostCoordinatesPointStatic(intersectingPoint, subtree, c_canHitDisabledElementsDefault, c_canHitInvisibleElementsDefault);

		[Uno.NotImplemented]
		public static IEnumerable<UIElement> FindElementsInHostCoordinates(Rect intersectingRect, UIElement/* ? */ subtree)
		{
			throw new NotSupportedException();
		}

		public static IEnumerable<UIElement> FindElementsInHostCoordinates(Point intersectingPoint, UIElement/* ? */ subtree, bool includeAllElements)
			=> FindAllElementsInHostCoordinatesPointStatic(intersectingPoint, subtree, includeAllElements);

		[Uno.NotImplemented]
		public static IEnumerable<UIElement> FindElementsInHostCoordinates(Rect intersectingRect, UIElement/* ? */ subtree, bool includeAllElements)
		{
			throw new NotSupportedException();
		}

		// Both accessors below walk the children by index rather than through LINQ. Enumerating a
		// MaterializableList goes through its Materialized copy -- a fresh List<T> whenever the collection
		// has been touched -- and LINQ additionally boxes the enumerator. These two run for every element
		// on every measure (FrameworkElement.HasTemplateChild) and for every node of every tree walk.
		public static DependencyObject/* ? */ GetChild(DependencyObject reference, int childIndex)
		{
			// Matches ElementAtOrDefault: any out-of-range index yields null. Negative indices are rejected
			// up front so they stay O(1), as they were with ElementAtOrDefault's own short-circuit.
			if (childIndex < 0 || reference is not UIElement element)
			{
				return null;
			}

			var children = element.GetChildren();
			for (var i = 0; i < children.Count; i++)
			{
				var child = children[i];
				if (child is ElementStub)
				{
					continue;
				}

				if (childIndex == 0)
				{
					return child;
				}

				childIndex--;
			}

			return null;
		}

		public static int GetChildrenCount(DependencyObject reference)
		{
			if (reference is not UIElement element)
			{
				return 0;
			}

			var children = element.GetChildren();
			var count = 0;
			for (var i = 0; i < children.Count; i++)
			{
				if (children[i] is not ElementStub)
				{
					count++;
				}
			}

			return count;
		}

		internal static int GetViewGroupChildrenCount(_ViewGroup reference)
			=> reference.GetChildren().Count;

		internal static void AddView(_ViewGroup parent, _View child, int index)
		{
#if __CROSSRUNTIME__
			parent.AddChild(child, index);
#else
			throw new NotSupportedException("AddView not implemented on this platform.");
#endif
		}

		internal static void AddView(_ViewGroup parent, _View child)
		{
			parent.AddChild(child);
		}

		internal static void RemoveView(_ViewGroup parent, _View child)
		{
#if __CROSSRUNTIME__
			parent.RemoveChild(child);
#else
			throw new NotSupportedException("RemoveView not implemented on this platform.");
#endif
		}

		public static IReadOnlyList<Popup> GetOpenPopups(Window window)
		{
			if (window.RootElement?.XamlRoot?.VisualTree is { } visualTree)
			{
				return GetOpenPopups(visualTree);
			}

			return Array.Empty<Popup>();
		}

		private static IReadOnlyList<Popup> GetOpenFlyoutPopups(XamlRoot xamlRoot)
		{
			if (xamlRoot is null)
			{
				throw new ArgumentNullException(nameof(xamlRoot));
			}

			return GetOpenPopups(xamlRoot.VisualTree)
				.Where(p => p.IsForFlyout)
				.ToList()
				.AsReadOnly();
		}

		public static IReadOnlyList<Popup> GetOpenPopupsForXamlRoot(XamlRoot xamlRoot)
		{
			if (xamlRoot is null)
			{
				throw new ArgumentNullException(nameof(xamlRoot));
			}

			return GetOpenPopups(xamlRoot.VisualTree);
		}

		private static IReadOnlyList<Popup> GetOpenPopups(VisualTree visualTree)
		{
			if (visualTree?.PopupRoot is not { } popupRoot)
			{
				return Array.Empty<Popup>();
			}

			return popupRoot.GetOpenPopups();
		}

		public static DependencyObject/* ? */ GetParent(DependencyObject reference)
		{
			DependencyObject realParent = null;

			realParent ??= reference.GetParent() as DependencyObject;

			if (realParent is null && reference is _ViewGroup uiElement)
			{
				return uiElement.GetVisualTreeParent() as DependencyObject;
			}

			if (realParent is PopupPanel)
			{
				// Skip the popup panel and go to PopupRoot instead.
				realParent = GetParent(realParent);
			}

			return realParent;
		}

		internal static void CloseAllPopups(XamlRoot xamlRoot)
		{
			if (xamlRoot is null)
			{
				throw new ArgumentNullException(nameof(xamlRoot));
			}

			foreach (var popup in GetOpenPopups(xamlRoot.VisualTree))
			{
				popup.IsOpen = false;
			}
		}

		internal static void CloseLightDismissPopups(XamlRoot xamlRoot)
		{
			if (xamlRoot is null)
			{
				throw new ArgumentNullException(nameof(xamlRoot));
			}

			foreach (var popup in GetOpenPopups(xamlRoot.VisualTree).Where(p => p.IsLightDismissEnabled))
			{
				popup.IsOpen = false;
			}
		}

		internal static void CloseAllFlyouts(XamlRoot xamlRoot)
		{
			foreach (var popup in GetOpenFlyoutPopups(xamlRoot))
			{
				popup.IsOpen = false;
			}
		}

#nullable enable
		internal static IEnumerable<T> GetChildren<T>(DependencyObject view)
			=> (view as _ViewGroup)
				?.GetChildren()
				.OfType<T>()
				?? Enumerable.Empty<T>();

#if __CROSSRUNTIME__
		// This overload is more performant than GetChildren(DependecnyObject) below.
		// As the parameter type is more specific, the compiler will prefer it when the argument is UIElement.
		internal static MaterializableList<UIElement> GetChildren(UIElement element)
			=> element._children;
#endif

		internal static IEnumerable<DependencyObject> GetChildren(DependencyObject view)
			=> GetChildren<DependencyObject>(view);

		internal static void AddChild(UIElement view, UIElement child)
		{
#if __CROSSRUNTIME__
			view.AddChild(child);
#else
			throw new NotImplementedException("AddChild not implemented on this platform.");
#endif
		}

		internal static void RemoveChild(UIElement view, UIElement child)
		{
#if __CROSSRUNTIME__
			view.RemoveChild(child);
#else
			throw new NotImplementedException("AddChild not implemented on this platform.");
#endif
		}

		internal static UIElement ReplaceChild(UIElement view, int index, UIElement child)
		{
			throw new NotImplementedException("ReplaceChild not implemented on this platform.");
		}

		internal static void ClearChildren(UIElement view)
		{
#if __CROSSRUNTIME__
			view.ClearChildren();
#else
			throw new NotImplementedException("ClearChildren not implemented on this platform.");
#endif
		}

		internal static readonly GetHitTestability DefaultGetTestability = elt => (elt.GetHitTestVisibility(), DefaultGetTestability!);

		internal static (UIElement? element, Branch? stale) HitTest(
			Point position,
			UIElement? root,
			GetHitTestability? getTestability,
			StalePredicate? isStale,
			string tracingEntryPoint,
			int tracingEntryLine,
			string? tracingReason)
		{
#if TRACE_HIT_TESTING
			using var _ = BEGIN_TRACE();
			TRACE($"HIT_TEST [{tracingEntryPoint!.ToUpperInvariant()}@{tracingEntryLine}{(tracingReason is null ? "" : "--" + tracingReason)}] @{position.ToDebugString()}");
#endif

			if (root is not null)
			{
				return SearchDownForTopMostElementAt(root, position, root, getTestability ?? DefaultGetTestability, isStale);
			}

			return default;
		}

		internal static (UIElement? element, Branch? stale) HitTest(
			Point position,
			UIElement? root,
			GetHitTestability? getTestability = null,
			StalePredicate? isStale = null
#if TRACE_HIT_TESTING
			, [CallerMemberName] string caller = "")
		{
			using var _ = BEGIN_TRACE();
			TRACE($"HIT_TEST [{caller!.ToUpperInvariant()}] @{position.ToDebugString()}");
#else
			)
		{
#endif
			if (root is not null)
			{
				return SearchDownForTopMostElementAt(root, position, root, getTestability ?? DefaultGetTestability, isStale);
			}

			return default;
		}

		/// <param name="root">
		/// Root element for coordinate transforms. All <see cref="UIElement.GetTransform"/>
		/// calls use this as the target so that <paramref name="position"/> is correctly
		/// interpreted in the root's coordinate space. When equal to the visual tree root,
		/// hit-testing works in window-absolute coordinates (standard path). When set to a
		/// subtree element, hit-testing is scoped to that subtree with coordinates relative
		/// to it (used by the scoped <see cref="Windows.UI.Input.Preview.Injection.InputInjector"/>
		/// to bypass design-time overlays — spec 045).
		/// </param>
		/// <param name="position">
		/// On skia: The position relative to <paramref name="root"/>.
		/// Everywhere else: The position relative to the parent (i.e. the position in parent coordinates).
		/// </param>
		internal static (UIElement? element, Branch? stale) SearchDownForTopMostElementAt(
			UIElement root,
			Point position,
			UIElement element,
			GetHitTestability getVisibility,
			StalePredicate? isStale)
		{
			var stale = default(Branch?);
			(var elementHitTestVisibility, getVisibility) = getVisibility(element);

#if TRACE_HIT_TESTING
			using var _ = SET_TRACE_SUBJECT(element);
			TRACE($"- hit test visibility: {elementHitTestVisibility}");
#endif

			// If the element is not hit testable, do not even try to validate it nor its children.
			if (elementHitTestVisibility == HitTestability.Collapsed)
			{
				// Even if collapsed, if the element is stale, we search down for the real stale leaf
				if (isStale?.Method.Invoke(element) ?? false)
				{
					stale = SearchDownForStaleBranch(element, isStale.Value);
				}

				TRACE($"> NOT FOUND (Element is HitTestability.Collapsed) | stale branch: {stale?.ToString() ?? "-- none --"}");
				return (default, stale);
			}

			// LayoutSlotWithMarginsAndAlignments is the region where the element was arranged by its parent.
			// This is expressed in parent coordinate space
			TRACE($"- layoutSlot (rel to parent): {element.LayoutSlotWithMarginsAndAlignments.ToDebugString()}");
			if (element.IsScrollPort)
				TRACE($"- scroller: {element.ScrollOffsets.ToDebugString()}");
			if (element is ScrollViewer sv)
				TRACE($"- scroll viewer: zoom={sv.ZoomFactor:F2}");
			if (element.RenderTransform is { } tr)
				TRACE($"- renderTransform: {tr.ToMatrix(element.RenderTransformOrigin, element.ActualSize.ToSize())}");

#if __SKIA__
			var elementToRoot = UIElement.GetTransform(element, root);

			// The maximum region where the current element and its children might draw themselves
			// This is expressed in the window (absolute) coordinate space.
			var clippingBounds = element.Visual.GetArrangeClipPathInElementCoordinateSpace() is { } clipping
				? elementToRoot.Transform(clipping)
				: Rect.Infinite;
			if (element.Visual.Clip?.GetBounds(element.Visual) is { } clip)
			{
				clippingBounds = clippingBounds.IntersectWith(elementToRoot.Transform(clip)) ?? default;
			}
			TRACE($"- clipping (absolute): {clippingBounds.ToDebugString()}");

			// The region where the current element draws itself.
			// Be aware that children might be out of this rendering bounds if no clipping defined.
			// This is expressed in the window (absolute) coordinate space.
			var renderingBounds = elementToRoot.Transform(new Rect(new Point(), element.LayoutSlotWithMarginsAndAlignments.Size)).IntersectWith(clippingBounds) ?? Rect.Empty;
			TRACE($"- rendering (absolute): {renderingBounds.ToDebugString()}");
#else
			// First compute the transformation between the element and its parent coordinate space
			var matrix = Matrix3x2.Identity;
			element.ApplyRenderTransform(ref matrix);
			element.ApplyLayoutTransform(ref matrix);
			element.ApplyElementCustomTransform(ref matrix);
			element.ApplyFlowDirectionTransform(ref matrix);

			TRACE($"- transform to parent: [{matrix.M11:F2},{matrix.M12:F2} / {matrix.M21:F2},{matrix.M22:F2} / {matrix.M31:F2},{matrix.M32:F2}]");

			// Build 'position' in the current element coordinate space
			var posRelToElement = matrix.Inverse().Transform(position);
			TRACE($"- position relative to element: {posRelToElement.ToDebugString()} | relative to parent: {position.ToDebugString()}");

			// Second compute the transformations applied locally.
			// This is somehow the difference between the "XAML coordinate space" and the effective coordinate space.
			matrix = Matrix3x2.Identity;
			element.ApplyRenderTransform(ref matrix, ignoreOrigin: true);
			matrix.Translation = default; //
			element.ApplyElementCustomTransform(ref matrix);
			element.ApplyFlowDirectionTransform(ref matrix);
			matrix = matrix.Inverse();

			// The maximum region where the current element and its children might draw themselves
			// This is expressed in element coordinate space.
			var clippingBounds = element.Viewport is { IsInfinite: false } clipping ? matrix.Transform(clipping) : Rect.Infinite;
			TRACE($"- clipping (rel to element): {clippingBounds.ToDebugString()}");

			// The region where the current element draws itself.
			// Be aware that children might be out of this rendering bounds if no clipping defined.
			// This is expressed in element coordinate space.
			var renderingBounds = matrix.Transform(new Rect(new Point(), element.LayoutSlotWithMarginsAndAlignments.Size));
			renderingBounds = renderingBounds.IntersectWith(clippingBounds) ?? Rect.Empty;
			TRACE($"- rendering (rel to element): {renderingBounds.ToDebugString()}");
#endif

#if __SKIA__
			var testPosition = position;
#else
			var testPosition = posRelToElement;
#endif
			// Validate that the pointer is in the bounds of the element
			if (!clippingBounds.Contains(testPosition))
			{
				// Even if out of bounds, if the element is stale, we search down for the real stale leaf
				if (isStale is { } stalePredicate)
				{
					if (stalePredicate.Method(element))
					{
						TRACE($"- Is {stalePredicate.Name}");

						stale = SearchDownForStaleBranch(element, stalePredicate);
					}
					else
					{
						TRACE($"- Is NOT {stalePredicate.Name}");
					}
				}

				TRACE($"> NOT FOUND (Out of the **clipped** bounds) | stale branch: {stale?.ToString() ?? "-- none --"}");
				return (default, stale);
			}

			// Validate if any child is an acceptable target
			var children = GetManagedVisualChildren(element);
			var isChildStale = isStale;

			// We only take ZIndex into account on skia, which supports Canvas.Zindex for non-canvas panels.
			// Once Canvas.ZIndex renders correctly elsewhere, remove the conditional OrderBy
			// https://github.com/unoplatform/uno/issues/325
			using var child = children
#if __SKIA__
				// On Skia and Wasm, we can get concrete data structure (MaterializableList in this case) instead of IEnumerable<T>.
				// It has an efficient "ReverseEnumerator". This will also avoid the boxing allocations of the enumerator when it's a struct.
				.GetReverseSortedEnumerator(UIElementToCanvasZIndex);
#else
				.Reverse()
				.GetEnumerator();
#endif

			while (child.MoveNext())
			{
				var childResult = SearchDownForTopMostElementAt(root, testPosition, child.Current!, getVisibility, isChildStale);

				// If we found a stale element in child sub-tree, keep it and stop looking for stale elements
				if (childResult.stale is not null)
				{
					stale = childResult.stale;
					isChildStale = null;
				}

				// If we found an acceptable element in the child's sub-tree, job is done!
				if (childResult.element is not null)
				{
					if (isChildStale is { } childStalePredicate) // Also indicates that stale is null
					{
						// If we didn't find any stale root in previous children or in the child's sub tree,
						// we continue to enumerate sibling children to detect a potential stale root.

						TRACE($"+ Searching for stale {childStalePredicate.Name} branch.");

						while (child.MoveNext())
						{
#if TRACE_HIT_TESTING
							using var __ = SET_TRACE_SUBJECT(child.Current);
#endif

							if (childStalePredicate.Method(child.Current))
							{
								TRACE($"- Is {childStalePredicate.Name}");

								stale = SearchDownForStaleBranch(child.Current!, childStalePredicate);

#if TRACE_HIT_TESTING
								while (child.MoveNext())
								{
									using var ___ = SET_TRACE_SUBJECT(child.Current);
									if (childStalePredicate.Method(child.Current))
									{
										//Debug.Assert(false);
										TRACE($"- Is {childStalePredicate.Name} ***** INVALID: Only one branch can be considered as stale at once! ****");
									}
									TRACE($"> Ignored since leaf and stale branch has already been found.");
								}
#endif

								break;
							}
							else
							{
								TRACE($"- Is NOT {childStalePredicate.Name}");
							}
						}
					}
#if TRACE_HIT_TESTING
					else
					{
						while (child.MoveNext())
						{
							using var __ = SET_TRACE_SUBJECT(child.Current);
							TRACE($"> Ignored since leaf has already been found and no stale branch to find.");
						}
					}
#endif

					TRACE($"> found child: {childResult.element.GetDebugName()} | stale branch: {stale?.ToString() ?? "-- none --"}");
					return (childResult.element, stale);
				}
			}

			// We didn't find any child at the given position, validate that element can be touched,
			// and the position is in actual bounds(which might be different than the clipping bounds)
			if (elementHitTestVisibility == HitTestability.Visible
				&& renderingBounds.Contains(testPosition)
				// TODO: Those HitTest should be provided by the `getVisibility`. SearchDownForTopMostElementAt is NOT about hit-testing (even if derived from and used by)
#if __SKIA__
				&& element.HitTest(elementToRoot.Inverse().Transform(testPosition))
#endif
				)
			{
				TRACE($"> LEAF! ({element.GetDebugName()} is the OriginalSource) | stale branch: {stale?.ToString() ?? "-- none --"}");
				return (element, stale);
			}
			else
			{
				// If no stale element found yet, validate if the current is stale.
				// Note: no needs to search down for stale child, we already did it!
				if (isStale?.Method.Invoke(element) ?? false)
				{
					stale = new Branch(element, stale?.Leaf ?? element);
				}

				TRACE($"> NOT FOUND (HitTestability.Invisible or out of the **render** bounds) | stale branch: {stale?.ToString() ?? "-- none --"}");
				return (default, stale);
			}
		}

		private static Branch SearchDownForStaleBranch(UIElement staleRoot, StalePredicate isStale)
			=> new(staleRoot, SearchDownForLeafCore(staleRoot, isStale));

		internal static UIElement SearchDownForLeaf(UIElement root, StalePredicate predicate)
		{
#if TRACE_HIT_TESTING
			using var trace = ENSURE_TRACE();
#endif
			return SearchDownForLeafCore(root, predicate);
		}

		private static UIElement SearchDownForLeafCore(UIElement root, StalePredicate predicate)
		{
			// We only take ZIndex into account on skia, which supports Canvas.Zindex for non-canvas panels.
			// Once Canvas.ZIndex renders correctly elsewhere, remove the conditional OrderBy
			// https://github.com/unoplatform/uno/issues/325
			using var enumerator = GetManagedVisualChildren(root)
#if __SKIA__
				// On Skia and Wasm, we can get concrete data structure (MaterializableList in this case) instead of IEnumerable<T>.
				// It has an efficient "ReverseEnumerator". This will also avoid the boxing allocations of the enumerator when it's a struct.
				.GetReverseSortedEnumerator(UIElementToCanvasZIndex);
#else
				.Reverse()
				.GetEnumerator();
#endif

			while (enumerator.MoveNext())
			{
				var child = enumerator.Current;
#if TRACE_HIT_TESTING
				SET_TRACE_SUBJECT(child);
#endif

				if (predicate.Method(child))
				{
					TRACE($"- Is {predicate.Name}");
					return SearchDownForLeafCore(child, predicate);
				}
				else
				{
					TRACE($"- Is NOT {predicate.Name}");
				}
			}

			return root;
		}

#if __SKIA__
		// This is used with MaterializableList.GetReverseSortedEnumerator
		private static int UIElementToCanvasZIndex(UIElement element)
			=> element.Visual.ZIndex; // Equivalent to GetValue(Canvas.ZIndexProperty) on skia
#endif

		internal static IEnumerable<DependencyObject> EnumerateAncestors(DependencyObject o)
		{
			while ((o as FrameworkElement)?.Parent is { } parent)
			{
				yield return parent;
				o = parent;
			}
		}

		#region Programmatic hit testing (FindElementsInHostCoordinates)
		// MUX Reference dxaml\xcp\dxaml\lib\VisualTreeHelper.h, tag winui3/release/2.5.4-experimental, commit 7b127093475
		private const bool c_canHitDisabledElementsDefault = false;
		private const bool c_canHitInvisibleElementsDefault = false;

		// MUX Reference dxaml\xcp\dxaml\lib\VisualTreeHelper.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475
		private static IEnumerable<UIElement> FindElementsInHostCoordinatesPointStatic(
			Point intersectingPoint,
			UIElement? pSubTree,
			bool canHitDisabledElements,
			bool canHitInvisibleElements)
			=> FindElementsInHostCoordinatesPointStatic(intersectingPoint, pSubTree, canHitDisabledElements, canHitInvisibleElements, invisibleHitTestMode: false);

		private static IEnumerable<UIElement> FindElementsInHostCoordinatesPointStatic(
			Point intersectingPoint,
			UIElement? pSubTree,
			bool canHitDisabledElements,
			bool canHitInvisibleElements,
			bool invisibleHitTestMode)
		{
			// TODO Uno: The subtree is already the core element, there is no DXamlCore handle to resolve.
			// MakeUIElementList is not needed either: the hit test returns the managed list directly.
			return CoreImports_UIElement_HitTestPoint(
				pSubTree,
				intersectingPoint,
				canHitDisabledElements,
				canHitInvisibleElements,
				invisibleHitTestMode);
		}

		private static IEnumerable<UIElement> FindAllElementsInHostCoordinatesPointStatic(
			Point intersectingPoint,
			UIElement? pSubTree,
			bool includeAllElements)
		{
			// enable invisible hit testing
			// TODO Uno: WinUI toggles the core-wide invisible hit test mode around the call
			// (HostProperties_SetInvisibleHitTestMode), Uno passes the mode down the walk instead.
			//if (includeAllElements)
			//{
			//	CoreImports::HostProperties_SetInvisibleHitTestMode(pCore->GetHandle(), true);
			//	bHistTestModeSet = TRUE;
			//}

			// now call the regular hit test function.
			return FindElementsInHostCoordinatesPointStatic(intersectingPoint, pSubTree, c_canHitDisabledElementsDefault, c_canHitInvisibleElementsDefault, invisibleHitTestMode: includeAllElements);
		}

		// MUX Reference dxaml\xcp\core\core\elements\uielement.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475
		private static void GetProgrammaticHitTestingParams(
			UIElement? uiElement,
			out UIElement ppThisNoRef,
			out PopupRoot? popupRootNoRef,
			out UIElement? popupHitTestSubtreeRootNoRef)
		{
			// TODO Uno: Uno, like a WinUI 3 desktop app (FrameworkApplication), always runs islands-only,
			// so only the InitializationType::IslandsOnly branch is ported.

			// When an app is running in an islands-only context, it must specify a UIElement to scope down the subtree
			// that we're hit testing against. Otherwise there is no way to tell which island it's talking about. Each
			// island has its own coordinate space, so we can't just pass the same coordinates to each island and expect
			// something that makes sense.
			//
			// Note that we do not make an exception for the special case where there's only one island. The app must
			// still specify that island. This way the VisualTreeHelper::FindElementsInHostCoordinates API has more
			// consistent behavior and will not be suddenly broken if the app adds another Xaml island.
			if (uiElement is null)
			{
				// ERROR_HIT_TEST_IS_NOT_ASSOCIATED_WITH_CONTENT_TREE
				throw new ArgumentException("VisualTreeHelper::FindElementsInHostCoordinates failed because the XAML runtime is running to support DesktopWindowXamlSource, and in this mode a UIElement must be provided.");
			}

			// Do not return a transformer. There's no need to transform the incoming coordinates into the island's
			// coordinate space. The island itself does not apply a plateau scale.
			ppThisNoRef = uiElement;

			var visualTree = VisualTree.GetForElement(uiElement);
			if (visualTree != null)
			{
				popupRootNoRef = visualTree.PopupRoot;
			}
			else
			{
				popupRootNoRef = null;
			}

			// An app can't access the true root of the Xaml island to pass in. So if they pass in the Content property
			// (the public root), treat it as if they specified the XamlIslandRoot for popup hit testing. This allows
			// programmatic hit testing to hit parentless popups, which are under the XamlIslandRoot but not under the
			// Content element.
			var uiElementAncestor = GetParent(uiElement);
			if (uiElementAncestor is global::Uno.UI.Xaml.Islands.XamlIslandRoot islandRoot)
			{
				popupHitTestSubtreeRootNoRef = islandRoot;
			}
			// TODO Uno: The window content is hosted in a WindowChrome rather than directly in the root, so the public
			// root is recognized through XamlRoot.Content and mapped to the root of its visual tree.
			else if (visualTree is not null && uiElement.XamlRoot?.Content == uiElement)
			{
				popupHitTestSubtreeRootNoRef = visualTree.RootElement;
			}
			else
			{
				popupHitTestSubtreeRootNoRef = uiElement;
			}
		}

		//------------------------------------------------------------------------
		//
		//  Synopsis:
		//      Public export for the Point case
		//
		//  Notes:
		//      The coordinates for ptHit are absolute coordinates
		//      (relative to RootVisual) and not relative to pvUIElement.
		//
		//      When passing null as pvUIElement, will assume the visual
		//      root and not filter popups (so unrooted popups will
		//      be included).
		//
		//------------------------------------------------------------------------
		private static List<UIElement> CoreImports_UIElement_HitTestPoint(
			UIElement? uiElement,
			Point ptHit,
			bool canHitDisabledElements,
			bool canHitInvisibleElements,
			bool invisibleHitTestMode)
		{
			List<UIElement> oResults = new();
			var hitTestConstrainedToPopup = false;

			GetProgrammaticHitTestingParams(
				uiElement,
				out var pThisNoRef,
				out var popupRootNoRef,
				out var popupHitTestSubtreeRootNoRef);

			// TODO Uno: The islands-only path never returns a transformer, so the plateau scale transform is not ported.
			//if (transformer != nullptr)
			//{
			//	// Use the transform on the internal root visual to account for plateau scale.
			//	IFC_RETURN(transformer->Transform(&ptHit, &ptHit, 1));
			//}

			// TODO Uno: Positions are resolved through each element's transform to the root of its visual tree,
			// so an element that is not in a visual tree cannot be hit.
			if (VisualTree.GetForElement(pThisNoRef)?.RootElement is not { } root)
			{
				return oResults;
			}

			if (popupRootNoRef is not null)
			{
				HitTestParams hitTestParams = new(root, invisibleHitTestMode);

				hitTestParams.SaveWorldSpaceHitTarget(ptHit);

				HitTestPopups(
					popupRootNoRef,
					ptHit,
					hitTestParams,
					true, /*canHitMultipleElements*/
					popupHitTestSubtreeRootNoRef,
					canHitDisabledElements,
					canHitInvisibleElements,
					oResults);
				hitTestConstrainedToPopup = GetClosestPopupAncestor(popupHitTestSubtreeRootNoRef) != null;
			}

			// If we filtered to a subtree within a Popup, we skip the main hit test path
			// to avoid getting duplicate entries. This is because while a normal hit test
			// walk will stop at Popup boundaries, it will merrily proceed along if the
			// walk starts from within a Popup's subtree, and we already did this hit test
			// walk above.
			if (!hitTestConstrainedToPopup)
			{
				var transformedPoint = true;

				HitTestParams hitTestParams = new(root, invisibleHitTestMode);
				hitTestParams.SaveWorldSpaceHitTarget(ptHit);

				// Since the coordinates at this point are relative to the internal root visual,
				// all transforms between the internal root visual and the subtree (pThis)
				// must be pushed onto the transform stack before we start calling
				// the public hit test API on pThis element.
				if (GetParent(pThisNoRef) is UIElement pThisParent)
				{
					// TODO: HWPC: Consider leaving bounds stale and only updating layout+bounds during Tick
					// Bounds are dependent on layout, so layout must be updated first.
					pThisParent.UpdateLayout();

					PrepareHitTestParamsStartingHere(pThisNoRef, ref hitTestParams, ref ptHit);
				}

				if (transformedPoint)
				{
					HitTestEntry(pThisNoRef, hitTestParams, ptHit, true /*canHitMultipleElements*/, canHitDisabledElements, canHitInvisibleElements, oResults);
				}
			}

			// Get the results into a list
			// TODO Uno: CHitTestResults::GetAnswer reverses its newest-first list back into insertion order,
			// the managed list is already in insertion order.
			return oResults;
		}

		//------------------------------------------------------------------------
		//
		//  Synopsis:
		//            Global entry point for point hit testing. Multiple results
		//            can be returned.
		//
		//------------------------------------------------------------------------
		private static void HitTestEntry(
			UIElement element,
			HitTestParams hitTestParams,
			Point hitPoint,
			bool canHitMultipleElements,
			bool canHitDisabledElements,
			bool canHitInvisibleElements,
			List<UIElement> pHitElements)
		{
			BoundedHitTestVisitor visitor = new(pHitElements, canHitMultipleElements, hitTestParams.InvisibleHitTestMode);

			BoundsTestEntry(element, hitTestParams, hitPoint, visitor, canHitDisabledElements, canHitInvisibleElements);
		}

		//------------------------------------------------------------------------
		//
		//  Synopsis:
		//      Should be called before doing programmatic hit testing.
		//      To proceed with programmatic hit testing, Two conditions must
		//      be met:
		//      (1) The element is HitTestVisible.
		//      (2) Should hittest disabled element OR The element is enabled.
		//
		//------------------------------------------------------------------------
		private static bool IsEnabledAndVisibleForHitTest(UIElement element, bool canHitDisabledElements, bool canHitInvisibleElements)
		{
			// TODO Uno: Uno coerces IsHitTestVisible, Visibility and the inherited IsEnabled into HitTestVisibility, so
			// canHitDisabledElements and canHitInvisibleElements cannot relax it. Every public caller passes false for both.
			// A parentless Popup is never loaded in Uno, so it gets its own (non-inherited) flags instead.
			//return IsHitTestVisible(canHitInvisibleElements) && (canHitDisabledElements || IsEnabled());
			if (element is Popup { IsInLiveTree: false } popup)
			{
				return popup.IsHitTestVisible && popup.Visibility == Visibility.Visible;
			}

			return element.GetHitTestVisibility() != HitTestability.Collapsed;
		}

		//------------------------------------------------------------------------
		//
		//  Synopsis:
		//      Returns true if the current element is part of the subtree of the
		//  other element, adjusting for popup boundaries.
		//
		//------------------------------------------------------------------------
		private static bool IsInUIElementsAdjustedVisualSubTree(UIElement element, UIElement pPotentialParentOrSelf)
		{
			UIElement? pElement = element;

			// Need to walk up and figure out the the parent chain.
			while (pElement != null)
			{
				if (pElement == pPotentialParentOrSelf)
				{
					return true;
				}

				// Move to adjusted parent
				pElement = pElement.GetUIElementAdjustedParentInternal(false);
			}

			return false;
		}

		// MUX Reference dxaml\xcp\components\elements\UIElementHitTesting.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475
		private static bool CollectTransformsAndTransformToInner(UIElement element, ref HitTestParams hitTestParams, ref Point transformedTarget)
		{
			// TODO Uno: WinUI composes the local transform of each element onto hitTestParams.combinedTransformMatrix while it
			// walks down and maps the parent-space target into local space. Uno maps the saved world-space target through the
			// absolute transform of the element instead, which covers the same chain of transforms.
			var elementToRoot = UIElement.GetTransform(element, hitTestParams.Root);
			if (Matrix3x2.Invert(elementToRoot, out var rootToElement))
			{
				transformedTarget = rootToElement.Transform(hitTestParams.WorldSpaceHitTarget);
				return true;
			}

			// Transform is not invertible.
			transformedTarget = new Point(double.NaN, double.NaN);
			return false;
		}

		// FindElementsInHostCoordinates can restrict the hit test to start at a particular subtree instead of starting at the root,
		// so we need to collect 3D transforms along 3D branches and transform the hit test target down to that subtree. We then start
		// a normal hit test walk on that subtree. Popup hit testing need to do the same thing for a hit test on a popup in the tree.
		private static bool PrepareHitTestParamsStartingHere(UIElement element, ref HitTestParams hitTestParams, ref Point transformedTarget)
		{
			// TODO Uno: Every element maps the world-space target through its own absolute transform
			// (CollectTransformsAndTransformToInner), so there is no ancestor chain to collect here.
			//Jupiter::stack_vector<CUIElement*, 32> ancestorChain;
			//FillAncestorChainForTransformToRoot(ancestorChain, false /* includeThisElement */, false /* useTargetInformation */);
			var wasTransformed = true;

			// Throw away whatever transforms that have already been collected. We're about to walk up to the root and collect again,
			// and we don't want to double-count anything (e.g. the zoom scale on the root visual). Also reset the hit test target since
			// we're about to start over again.
			transformedTarget = hitTestParams.WorldSpaceHitTarget;

			//for (auto reverse = ancestorChain.m_vector.rbegin();
			//	wasTransformed && reverse != ancestorChain.m_vector.rend();
			//	++reverse)
			//{
			//	wasTransformed = (*reverse)->CollectTransformsAndTransformToInner(hitTestParams, transformedTarget);
			//}

			return wasTransformed;
		}

		// MUX Reference dxaml\xcp\core\core\elements\uielement.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475
		//------------------------------------------------------------------------
		//
		//  Synopsis:
		//      Walk the element and its children finding elements that intersect
		//      with the given point/polygon.
		//
		//------------------------------------------------------------------------
		private static void BoundsTestEntry(
			UIElement element,
			HitTestParams hitTestParams,
			Point target,
			BoundedHitTestVisitor pCallback,
			bool canHitDisabledElements,
			bool canHitInvisibleElements)
		{
			// TODO Uno: No hit test ETW tracing.
			//TraceHitTestBegin();

			// TODO: HitTest: Consider leaving layout stale and only updating layout during Tick
			element.UpdateLayout();

			// Ensure all bounds in the subgraph are up-to-date for hit-testing.
			// TODO Uno: Uno does not cache outer bounds.
			//IFC_RETURN(EnsureOuterBounds(&myParams));   // Use the non-const cloned params

			BoundsTestInternal(element, target, pCallback, hitTestParams, canHitDisabledElements, canHitInvisibleElements, out _);

			//TraceHitTestEnd();
		}

		//------------------------------------------------------------------------
		//
		//  Synopsis:
		//      Walk the element and its children finding elements that intersect
		//      with the given point.
		//
		//------------------------------------------------------------------------
		private static void BoundsTestInternal(
			UIElement element,
			Point target,
			BoundedHitTestVisitor pCallback,
			HitTestParams hitTestParams,
			bool canHitDisabledElements,
			bool canHitInvisibleElements,
			out BoundsWalkHitResult pResult)
		{
			// MUX Reference dxaml\xcp\core\core\elements\Popup.cpp (CPopup::BoundsTestInternal)
			if (element is Popup)
			{
				//
				// Intentionally do not test the element with this API. Popups
				// can only be bounds tested using the BoundsTestPopup API
				// to ensure parented and non-parented popups are treated
				// uniformly.
				//

				pResult = BoundsWalkHitResult.Continue;
				return;
			}

			BoundsTestInternalImpl(element, target, pCallback, hitTestParams, canHitDisabledElements, canHitInvisibleElements, out pResult);
		}

		//------------------------------------------------------------------------
		//
		//  Synopsis:
		//      Walk the element and its children finding elements that intersect
		//      with the given point/polygon.
		//
		//------------------------------------------------------------------------
		private static void BoundsTestInternalImpl(
			UIElement element,
			Point target,
			BoundedHitTestVisitor pCallback,
			HitTestParams hitTestParams,
			bool canHitDisabledElements,
			bool canHitInvisibleElements,
			out BoundsWalkHitResult pResult)
		{
			var hitResult = BoundsWalkHitResult.Continue;

			// We can exit early out of the hit testing walk if we got clipped out or we couldn't do an inverse transform.
			var continueHitTest = true;

			// TODO Uno: Layout transitions are not supported, so no element is hidden for one (IsHiddenForLayoutTransition).
			if (IsEnabledAndVisibleForHitTest(element, canHitDisabledElements, canHitInvisibleElements))
			{
				// TODO Uno: Uno does not cache the outer bounds of the subtree, nor supports 3D transforms, so the walk
				// never culls here. That culling is only a shortcut of the clip and local tests below.
				//continueHitTest =
				//	GetContext()->InvisibleHitTestMode()
				//	|| DoesRectIntersectHitType(m_outerBounds, target)
				//	|| HasTransform3DInSubtree(hitTestParams)
				//	|| HasDepthLegacy()   // TODO: HitTest: combine this with GetHitTestingTransform3D
				//	|| canHitInvisibleElements;

				if (continueHitTest)
				{
					var newParams = hitTestParams;
					var testTarget = target;

					//
					// We start with the target point/rect in the parent coordinate space.
					//

					// Check against the layout clip, if the clip should be applied above transforms
					// In this case we should be testing the parent space point/rect against the layout clip.
					// TODO Uno: The arrange clip of Uno is already mapped to the element space when it acts as an ancestor
					// clip (ContainerVisual.GetArrangeClipPathInElementCoordinateSpace), so it is tested below, in local space.
					//if (ShouldApplyLayoutClipAsAncestorClip() && HasLayoutClip())
					//{
					//	IFC_RETURN(LayoutClipGeometry->ClipToFill(const_cast<HitType&>(target), nullptr, &continueHitTest));
					//}

					// Transform the target point/rect into local space.
					if (continueHitTest)
					{
						CollectTransformsAndTransformToInner(element, ref newParams, ref testTarget);
					}

					//
					// At this point we have testTarget in local space coordinates.
					// Next we check against clips.
					//

					if (continueHitTest)
					{
						// Clip target. Prefer the hand off visual's clip over the Xaml clip, in case the app got the hand off
						// visual and updated the rect.
						if (element.Visual.Clip?.GetBounds(element.Visual) is { } clip)
						{
							continueHitTest = clip.Contains(testTarget);
						}

						// Check against the layout clip, if the clip should be applied below transforms
						if (continueHitTest && element.Visual.GetArrangeClipPathInElementCoordinateSpace() is { } layoutClip)
						{
							continueHitTest = layoutClip.Contains(testTarget);
						}

						// Clip target to the implicit transition clip
						// TODO Uno: Implicit transitions are not supported (GetTransitionTarget).

						// Note: The ClipToFill calls above will return true and not clip the target
						// if the geometry is concave. We will still clip to our bounds rect below as a
						// first approximation.
						// Again, ignore the bounds if we're hit-testing invisible objects. Some objects like
						// shape may have an empty bounds if they have no fill/stroke and we still want to include
						// them as candidate hit elements in this case.
						// TODO Uno: Uno does not cache the combined inner bounds, the local test of each element and child
						// is the precise version of that check.
						//if (continueHitTest && !GetContext()->InvisibleHitTestMode() && !HasTransform3DInSubtree(hitTestParams))
						//{
						//	// Use the pre-calculated inner bounds for fast culling if we're not in 3D mode.
						//	// In 3D mode, m_combinedInnerBounds is not a valid distinguisher for fast culling.
						//	ASSERT(!AreInnerBoundsDirty());
						//	IFC_RETURN(ClipHitTypeToRect(testTarget, m_combinedInnerBounds, &continueHitTest));
						//}

						// TODO Uno: The rounded corners of a Border do not clip the hit test of its children.
						//if (continueHitTest && RequiresCompNodeForRoundedCorners())
						//{
						//	// Perform rounded corner hit-testing in this scenario, as the rounded corners clips children as well as content.
						//	ASSERT(OfTypeByIndex<KnownTypeIndex::FrameworkElement>());
						//	continueHitTest = CBorder::HitTestRoundedCornerClip(static_cast<CFrameworkElement*>(this), testTarget);
						//}
					}

					// If we successfully transformed the point/rect into local space, and it passed all clip checks, proceed
					// with hit testing against content and children.
					if (continueHitTest)
					{
						BoundsTestContentAndChildren(element, testTarget, pCallback, newParams, canHitDisabledElements, canHitInvisibleElements, out hitResult);
					}
				}
			}

			pResult = hitResult;
		}

		//------------------------------------------------------------------------
		//
		//  Synopsis:
		//      Handles walking the TransitionRoot, children, and content and determining
		//      if any were hit.
		//
		//------------------------------------------------------------------------
		private static void BoundsTestContentAndChildren(
			UIElement element,
			Point testTarget,
			BoundedHitTestVisitor pCallback,
			HitTestParams hitTestParams,
			bool canHitDisabledElements,
			bool canHitInvisibleElements,
			out BoundsWalkHitResult pResult)
		{
			var hitResult = BoundsWalkHitResult.Continue;

			// LayoutTransitionElements will call this path directly on their target.
			// We never expect to hit-test something that isn't hit-test visible, enabled, etc, but we do explicitly allow
			// hit-testing elements that are LTE targets via this method only.
			Debug.Assert(IsEnabledAndVisibleForHitTest(element, canHitDisabledElements, canHitInvisibleElements));

			// Test any post-children content drawn by this element (above its children in z-order).
			// Both pre- and post-children content are included in the content inner bounds.
			// TODO Uno: Uno does not cache the content inner bounds, the local test of the visitor is the precise check.
			//if (core->InvisibleHitTestMode() || DoesRectIntersectHitType(m_contentInnerBounds, testTarget) || canHitInvisibleElements)
			{
				pCallback.OnElementHit(element, testTarget, true /*hitPostChildren*/, out hitResult);
			}

			// Use the pre-calculated child bounds for fast culling.
			// Ignore the bounds if we're hit-testing invisible elements.
			// TODO Uno: Uno does not cache the child bounds, each child tests itself.
			//if (core->InvisibleHitTestMode() || DoesRectIntersectHitType(m_childBounds, testTarget) || HasTransform3DInSubtree(hitTestParams) || canHitInvisibleElements)
			{
				if (hitResult.HasFlag(BoundsWalkHitResult.Continue))
				{
					// Any active LayoutTransitions in this element's TransitionRoot draw on top of its children, so test them first.
					// TODO Uno: Layout transitions are not supported (GetLocalTransitionRoot).
				}

				if (hitResult.HasFlag(BoundsWalkHitResult.Continue))
				{
					// Children draw in front of the element's own content, so they need to be tested next.
					BoundsTestChildren(element, testTarget, pCallback, hitTestParams, canHitDisabledElements, canHitInvisibleElements, out hitResult);
				}
			}

			// Check if the parent should be included - this means a child was hit.
			// This element (the parent) will be included whether its content would be hit or not.
			if (hitResult.HasFlag(BoundsWalkHitResult.IncludeParents))
			{
				pCallback.OnParentIncluded(element, testTarget, out hitResult);
			}
			// Otherwise, if bounds testing should continue test the element's content last.
			else if (hitResult.HasFlag(BoundsWalkHitResult.Continue))
			{
				// Use the pre-calculated content bounds for fast culling.
				// Ignore the bounds if we're hit-testing invisible elements. Otherwise check if the
				// test is in the bounding rectangle before recording the hit element.
				// TODO Uno: Uno does not cache the content inner bounds, the local test of the visitor is the precise check.
				//if (core->InvisibleHitTestMode() || DoesRectIntersectHitType(m_contentInnerBounds, testTarget) || canHitInvisibleElements)
				{
					pCallback.OnElementHit(element, testTarget, false /*hitPostChildren*/, out hitResult);
				}
			}

			pResult = hitResult;
		}

		private static void BoundsTestChildren(
			UIElement element,
			Point target,
			BoundedHitTestVisitor pCallback,
			HitTestParams hitTestParams,
			bool canHitDisabledElements,
			bool canHitInvisibleElements,
			out BoundsWalkHitResult pResult)
		{
			// TODO Uno: CPopupRoot and CPopup override BoundsTestChildren, Uno dispatches on the type here.
			if (element is PopupRoot popupRoot)
			{
				PopupRootBoundsTestChildrenImpl(popupRoot, target, pCallback, hitTestParams, null /*pSubRoot*/, canHitDisabledElements, canHitInvisibleElements, out pResult);
			}
			else if (element is Popup popup)
			{
				PopupBoundsTestChildren(popup, target, pCallback, hitTestParams, canHitDisabledElements, canHitInvisibleElements, out pResult);
			}
			else
			{
				BoundsTestChildrenImpl(element, target, pCallback, hitTestParams, canHitDisabledElements, canHitInvisibleElements, out pResult);
			}
		}

		//------------------------------------------------------------------------
		//
		//  Synopsis:
		//      Walk the children of the element finding elements that intersect
		//      with the given point/polygon.
		//
		//  NOTE:
		//      Uses reverse render order (front to back).
		//
		//------------------------------------------------------------------------
		private static void BoundsTestChildrenImpl(
			UIElement element,
			Point target,
			BoundedHitTestVisitor pCallback,
			HitTestParams hitTestParams,
			bool canHitDisabledElements,
			bool canHitInvisibleElements,
			out BoundsWalkHitResult pResult)
		{
			var hitResult = BoundsWalkHitResult.Continue;
			var childHitResult = BoundsWalkHitResult.Continue;

			// TODO Uno: The render order is the children order sorted by Canvas.ZIndex, as rendered by the Skia visuals.
			using var children = GetManagedVisualChildren(element).GetReverseSortedEnumerator(UIElementToCanvasZIndex);

			// Test bounds in reverse render order (front to back).
			while (childHitResult.HasFlag(BoundsWalkHitResult.Continue) && children.MoveNext())
			{
				// Workaround for a crash in a scenario where items are removed while ListView reordering is in progress.
				// If element leaves the tree while it is being dragged, pointer capture loss event is fired which will trigger bounds walk.
				// If pointer was over the element leaving the tree it will be tested and since it was replaced with null
				// in children collection to prevent reentrancy (CDOCollection::Neat), the pointer can be null and this case needs to be guarded.
				// Bounds check can be skipped since it is called via CCollection::Destroy().
				if (children.Current is { } child)
				{
					BoundsTestInternal(child, target, pCallback, hitTestParams, canHitDisabledElements, canHitInvisibleElements, out childHitResult);

					// If any child wanted to include its parent chain, copy the flag.
					if (childHitResult.HasFlag(BoundsWalkHitResult.IncludeParents))
					{
						hitResult |= BoundsWalkHitResult.IncludeParents;
					}
				}
			}

			// If the child element wanted the bounds walk to stop, remove the continue flag
			// from the result.
			if (!childHitResult.HasFlag(BoundsWalkHitResult.Continue))
			{
				hitResult &= ~BoundsWalkHitResult.Continue;
			}

			pResult = hitResult;
		}

		// MUX Reference dxaml\xcp\core\core\elements\Popup.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475
		//------------------------------------------------------------------------
		//
		//  Synopsis:
		//      Walk the element and its children finding elements that intersect
		//      with the given point/polygon.
		//
		//------------------------------------------------------------------------
		private static void BoundsTestPopup(
			Popup popup,
			Point target,
			BoundedHitTestVisitor pCallback,
			HitTestParams hitTestParams,
			bool canHitDisabledElements,
			bool canHitInvisibleElements,
			out BoundsWalkHitResult pResult)
		{
			// Skip the Popup::BoundsTestInternal override (called during the regular tree walk) and
			// call into the real implementation.  BoundsTestPopup is only called from the PopupRoot,
			// which is where we actually want to test the Popups from.
			BoundsTestInternalImpl(popup, target, pCallback, hitTestParams, canHitDisabledElements, canHitInvisibleElements, out pResult);
		}

		//------------------------------------------------------------------------
		//
		//  Synopsis:
		//      Walk the children of the element finding elements that intersect
		//      with the given point.
		//
		//------------------------------------------------------------------------
		private static void PopupBoundsTestChildren(
			Popup popup,
			Point target,
			BoundedHitTestVisitor pCallback,
			HitTestParams hitTestParams,
			bool canHitDisabledElements,
			bool canHitInvisibleElements,
			out BoundsWalkHitResult pResult)
		{
			var childHitResult = BoundsWalkHitResult.Continue;

			if (popup.Child != null)
			{
				BoundsTestInternal(popup.Child, target, pCallback, hitTestParams, canHitDisabledElements, canHitInvisibleElements, out childHitResult);
			}

			pResult = childHitResult;
		}

		//------------------------------------------------------------------------
		//
		//  Synopsis:
		//      Returns the closest Popup ancestor or NULL if one does not exist.
		//
		//------------------------------------------------------------------------
		private static Popup? GetClosestPopupAncestor(UIElement? pChild)
		{
			var pNode = pChild;

			while (pNode != null)
			{
				if (pNode is Popup popup)
				{
					return popup;
				}

				pNode = pNode.GetUIElementAdjustedParentInternal(false);
			}

			return null;
		}

		//------------------------------------------------------------------------
		//
		//  Synopsis:
		//      Walk the children of the element finding elements that intersect
		//      with the given point/polygon.
		//
		//  Notes:
		//      Order of tested elements is most recent opened to oldest opened.
		//
		//------------------------------------------------------------------------
		private static void PopupRootBoundsTestChildrenImpl(
			PopupRoot popupRoot,
			Point target,
			BoundedHitTestVisitor pCallback,
			HitTestParams hitTestParams,
			UIElement? pSubTreeRoot,  // Used to support explicit hit testing against a subtree (i.e. VisualTreeHelper.FindElementsInHostCoordinates)
			bool canHitDisabledElements,
			bool canHitInvisibleElements,
			out BoundsWalkHitResult pResult)
		{
			var hitResult = BoundsWalkHitResult.Continue;
			var childHitResult = BoundsWalkHitResult.Continue;
			var pSubTreeRootPopup = GetClosestPopupAncestor(pSubTreeRoot);

			var newParams = hitTestParams;

			// TODO Uno: GetOpenPopups returns the open popups from the most recently opened one.
			var openPopups = popupRoot.GetOpenPopups();

			// Test bounds in opened order.
			for (var i = 0; i < openPopups.Count && childHitResult.HasFlag(BoundsWalkHitResult.Continue); i++)
			{
				var pPopup = openPopups[i];

				// TODO Uno: Uno does not track the unloading state of popups (IsUnloading).
				//if (!pPopup->IsUnloading())
				{
					// TODO Uno: A parentless popup has no adjusted parent in Uno, WinUI falls back to the popup root.
					var pPopupParent = pPopup.GetUIElementAdjustedParentInternal(false) ?? popupRoot;

					if (pPopupParent != null)
					{
						//
						// Only test against this popup if:
						//    A) We are not restricting to a subtree OR
						//    B) This popup contains the subtree root OR
						//    C) The subtree contains this popup.
						//

						var isSubTreeInPopup = pSubTreeRootPopup == pPopup;
						var isPopupInSubTree = pSubTreeRoot != null && IsInUIElementsAdjustedVisualSubTree(pPopupParent, pSubTreeRoot);

						if (pSubTreeRoot == null || isSubTreeInPopup || isPopupInSubTree)
						{
							Point popupTarget = default;
							var transformSucceeded = false;

							var shouldStartFromSubTreeRoot = pSubTreeRoot != null && !isPopupInSubTree;

							if (shouldStartFromSubTreeRoot)
							{
								transformSucceeded = PrepareHitTestParamsStartingHere(pSubTreeRoot!, ref newParams, ref popupTarget);
							}
							else
							{
								transformSucceeded = PrepareHitTestParamsStartingHere(pPopup, ref newParams, ref popupTarget);
							}

							if (transformSucceeded)
							{
								if (shouldStartFromSubTreeRoot)
								{
									// Ensure all bounds in the subgraph are up-to-date for hit-testing. Note that we already walked the popup
									// root and cleaned all bounds, but that walk stopped at collapsed elements. Here we're explicitly starting
									// a hit test at pSubTreeRoot, which might have dirty bounds because there was a collapsed ancestor between
									// it and the popup root.
									// TODO Uno: Uno does not cache outer bounds.
									//IFC_RETURN(pSubTreeRoot->EnsureOuterBounds(&newParams));

									if (pSubTreeRoot is Popup popup)
									{
										// If pSubTreeRoot is a popup, then its BoundsTestInternal is no-oped out. We want to call into the real implementation in BoundsTestPopup.
										BoundsTestPopup(popup, popupTarget, pCallback, newParams, canHitDisabledElements, canHitInvisibleElements, out childHitResult);
									}
									else
									{
										BoundsTestInternal(pSubTreeRoot!, popupTarget, pCallback, newParams, canHitDisabledElements, canHitInvisibleElements, out childHitResult);
									}
								}
								else
								{
									BoundsTestPopup(pPopup, popupTarget, pCallback, newParams, canHitDisabledElements, canHitInvisibleElements, out childHitResult);
								}

								// If any child wanted to include its parent chain, copy the flag.
								if (childHitResult.HasFlag(BoundsWalkHitResult.IncludeParents))
								{
									hitResult |= BoundsWalkHitResult.IncludeParents;
								}
							}
						}

						// Check for light dismiss.
						// If a drag and drop operation is in progress, we allow it to hit test through the light dismiss layer.
						// TODO Uno: m_fIsLightDismiss is the IsLightDismissEnabled value captured when the popup opened.
						if (pPopup.IsLightDismissEnabled &&
							childHitResult.HasFlag(BoundsWalkHitResult.Continue) &&
							!global::DirectUI.DXamlCore.IsWinRTDndOperationInProgress())
						{
							pCallback.OnElementHit(popupRoot, target, false /*hitPostChildren*/, out childHitResult);
						}
					}
				}
			}

			// If the child element wanted the bounds walk to stop, remove the continue flag
			// from the result.
			if (!childHitResult.HasFlag(BoundsWalkHitResult.Continue))
			{
				hitResult &= ~BoundsWalkHitResult.Continue;
			}

			pResult = hitResult;
		}

		//------------------------------------------------------------------------
		//
		//  Synopsis:
		//      Public hit test entry point for hit testing against all popups held
		//      inside this popup root. Only called as a result of a call to
		//      VisualTreeHelper.FindElementsInHostCoordinates.
		//
		//  Notes:
		//      Target is in absolute coordinates (relative to RootVisual).
		//
		//------------------------------------------------------------------------
		private static void HitTestPopups(
			PopupRoot popupRoot,
			Point target,
			HitTestParams hitTestParams,
			bool canHitMultipleElements,
			UIElement? pSubTreeRoot,
			bool canHitDisabledElements,
			bool canHitInvisibleElements,
			List<UIElement> pHitElements)
		{
			var hitResult = BoundsWalkHitResult.Continue;

			BoundedHitTestVisitor visitor = new(pHitElements, canHitMultipleElements, hitTestParams.InvisibleHitTestMode);

			var newParams = hitTestParams;

			// TODO: HitTest: Consider only updating layout during Tick.
			popupRoot.UpdateLayout();

			// Ensure all bounds in the subgraph are up-to-date for hit-testing.
			// TODO Uno: Uno does not cache outer bounds.
			//IFC_RETURN(EnsureOuterBounds(&newParams));

			// Any active LayoutTransitions in this element's TransitionRoot draw on top of its children, so test them first.
			// TODO Uno: Layout transitions are not supported (GetLocalTransitionRoot).

			if (!hitResult.HasFlag(BoundsWalkHitResult.Continue))
			{
				return;
			}

			// We don't need to transform from the root visual down to the popup root. We'll do that before walking into the popup.
			// Proceed with hit testing directly.
			PopupRootBoundsTestChildrenImpl(popupRoot, target, visitor, newParams, pSubTreeRoot, canHitDisabledElements, canHitInvisibleElements, out _);
		}

		private static bool PopupRootHitTestLocalInternal(PopupRoot popupRoot)
		{
			var isHit = false;

			// TODO Uno: Uno does not suppress hit testing of the root (m_isRootHitTestingSuppressed).
			//if (m_pOpenPopups && !m_isRootHitTestingSuppressed)
			{
				// Hit testing doesn't run on unloading popups.
				// TODO Uno: Uno does not track the unloading state of popups (IsUnloading).
				foreach (var pPopup in popupRoot.GetOpenPopups())
				{
					if (/*!pPopup->IsUnloading() &&*/ pPopup.IsLightDismissEnabled)
					{
						isHit = true;
						break;
					}
				}
			}

			return isHit;
		}

		// MUX Reference dxaml\xcp\core\inc\BoundsWalkHitResult.h, tag winui3/release/2.5.4-experimental, commit 7b127093475
		// There are multiple states for a hit testing walk. We could be looking for the element that will be hit. We could
		// have hit an element in the subtree and be walking back out of the tree while collecting its ancestor elements.
		// We could have hit an element in the subtree and decided not to include ancestors. These multiple states require
		// an enum to track.
		[Flags]
		private enum BoundsWalkHitResult
		{
			Stop = 0x00,
			Continue = 0x01,
			IncludeParents = 0x02
		}

		// MUX Reference dxaml\xcp\components\transforms\inc\HitTestParams.h, tag winui3/release/2.5.4-experimental, commit 7b127093475
		private struct HitTestParams
		{
			public HitTestParams(UIElement root, bool invisibleHitTestMode)
			{
				Root = root;
				InvisibleHitTestMode = invisibleHitTestMode;
			}

			// TODO Uno: The element every transform is resolved against, WinUI accumulates combinedTransformMatrix instead.
			public UIElement Root { get; }

			// TODO Uno: WinUI reads the core-wide flag (CCoreServices::InvisibleHitTestMode).
			public bool InvisibleHitTestMode { get; }

			public Point WorldSpaceHitTarget { get; private set; }

			public void SaveWorldSpaceHitTarget(Point target) => WorldSpaceHitTarget = target;
		}

		// MUX Reference dxaml\xcp\core\inc\UIElementStructs.h, tag winui3/release/2.5.4-experimental, commit 7b127093475
		// Responds to bounds walk hit testing. Further refines hit test results by hit testing the element
		// in its local coordinate space. Tracks the list of parent elements that were also hit.
		private sealed class BoundedHitTestVisitor
		{
			private readonly List<UIElement> m_pHitElements;
			private readonly bool m_hitMultiple;
			private readonly bool m_invisibleHitTestMode;

			// MUX Reference dxaml\xcp\core\core\elements\uielement.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475
			//------------------------------------------------------------------------
			//
			//  Synopsis:
			//      (ctor) - Create a new bounding region visitor for hit testing.
			//
			//------------------------------------------------------------------------
			public BoundedHitTestVisitor(
				List<UIElement> pHitElements,
				bool hitMultiple,
				bool invisibleHitTestMode)
			{
				m_pHitElements = pHitElements;
				m_hitMultiple = hitMultiple;
				// TODO Uno: WinUI reads the core-wide flag (CCoreServices::InvisibleHitTestMode).
				m_invisibleHitTestMode = invisibleHitTestMode;
			}

			//------------------------------------------------------------------------
			//
			//  Synopsis:
			//      Handler for an elements bounding region intersecting with the
			//      hit test point/polygon. Performs a detailed hit test on the element.
			//
			//------------------------------------------------------------------------
			public void OnElementHit(
				UIElement pElement,
				Point target,
				bool hitPostChildren,
				out BoundsWalkHitResult pResult)
			{
				bool hitTestResult;
				BoundsWalkHitResult result;

				if (hitPostChildren)
				{
					hitTestResult = HitTestLocalPostChildren(pElement, target);
				}
				else
				{
					hitTestResult = HitTestLocal(pElement, target);
				}

				if (hitTestResult)
				{
					Add(pElement);
				}

				//
				// If the element wasn't hit or multiple elements should be included, continue
				// the bounds walk.
				//
				result = (!hitTestResult || m_hitMultiple) ? BoundsWalkHitResult.Continue : BoundsWalkHitResult.Stop;

				//
				// If the element was hit and multiple elements should be included, force the
				// parent to be included.
				//
				if (hitTestResult && m_hitMultiple)
				{
					result |= BoundsWalkHitResult.IncludeParents;
				}

				pResult = result;
			}

			//------------------------------------------------------------------------
			//
			//  Synopsis:
			//      Handler for an elements parent being included due to the
			//      return value from OnElementHit.
			//
			//------------------------------------------------------------------------
			public void OnParentIncluded(
				UIElement pElement,
				Point target,
				out BoundsWalkHitResult pResult)
			{
				Debug.Assert(m_hitMultiple);

				Add(pElement);

				//
				// If the parent element is being included, then multiple hit test elements
				// must have been requested and the bounds walk should continue. Parent
				// elements should also be included from this element.
				//
				pResult = BoundsWalkHitResult.Continue | BoundsWalkHitResult.IncludeParents;
			}

			//------------------------------------------------------------------------
			//
			//  Synopsis:
			//      Adds a new UIElement to the current result set.
			//
			//------------------------------------------------------------------------
			private void Add(UIElement pResult)
				=> m_pHitElements.Add(pResult ?? throw new ArgumentNullException(nameof(pResult)));

			//------------------------------------------------------------------------
			//
			//  Synopsis:
			//      Common entry point for local space hit testing.
			//
			//------------------------------------------------------------------------
			private bool HitTestLocal(UIElement element, Point target)
			{
				// TODO Uno: CPopupRoot overrides HitTestLocalInternal, Uno dispatches on the type here.
				if (element is PopupRoot popupRoot)
				{
					return PopupRootHitTestLocalInternal(popupRoot);
				}

				// TODO Uno: The HitTestLocalInternal overrides of each element type map to the hit testability of the element
				// (Invisible when it has no Background/Fill) and to UIElement.HitTest, inside the bounds it renders in.
				if (element.GetHitTestVisibility() == HitTestability.Visible
					&& new Rect(default, element.LayoutSlotWithMarginsAndAlignments.Size).Contains(target)
					&& element.HitTest(target))
				{
					return true;
				}

				// Various uielements don't implement HitTestLocalInternal and rely on the base implementation.
				// Most of these require a very basic hit test algorithm - hit against the bounds.
				// Examples include ItemsPresenter, Page and UserControl. These don't hit by default, but
				// we will want them in InvisibleHitTestMode.
				if (m_invisibleHitTestMode)
				{
					var rc = new Rect(0, 0, element.ActualSize.X, element.ActualSize.Y);
					return rc.Contains(target);
				}

				return false;
			}

			//------------------------------------------------------------------------
			//
			//  Synopsis:
			//      Common entry point for local space hit testing of content that's
			//      rendered post-children (on top of, in z-order).
			//
			//------------------------------------------------------------------------
			private static bool HitTestLocalPostChildren(UIElement element, Point target)
			{
				// TODO Uno: The post-children content of ListViewBaseItemPresenter (CListViewBaseItemChrome) is rendered
				// by its regular visual, so it is hit tested with the content of the element.
				//if(OfTypeByIndex<KnownTypeIndex::ListViewBaseItemPresenter>())
				//{
				//	IFC_RETURN(static_cast<CListViewBaseItemChrome*>(this)->HitTestLocalInternalPostChildren(target, pHit));
				//}
				//else
				{
					return false;
				}
			}
		}
		#endregion

		#region Helpers
		internal static Func<IEnumerable<UIElement>, IEnumerable<UIElement>> Except(UIElement element)
			=> children => children.Except(element);

		internal static Func<IEnumerable<UIElement>, IEnumerable<UIElement>> SkipUntil(UIElement element)
			=> children => SkipUntilCore(element, children);

		private static IEnumerable<UIElement> SkipUntilCore(UIElement element, IEnumerable<UIElement> children)
		{
			using var enumerator = children.GetEnumerator();
			while (enumerator.MoveNext() && enumerator.Current != element)
			{
			}

			if (!enumerator.MoveNext())
			{
				yield break;
			}

			while (enumerator.MoveNext())
			{
				yield return enumerator.Current;
			}
		}

		internal static IEnumerable<UIElement> GetManagedVisualChildren(object view)
			=> view is _ViewGroup elt
				? GetManagedVisualChildren(elt)
				: Enumerable.Empty<UIElement>();

		internal static MaterializableList<UIElement> GetManagedVisualChildren(_View view)
			=> view._children;

		internal static MaterializableList<UIElement>.ReverseEnumerator GetManagedVisualChildrenReversedEnumerator(_View view)
			=> view._children.GetReverseEnumerator();

		internal static MaterializableList<UIElement>.ReverseReduceEnumerator GetManagedVisualChildrenReversedEnumerator(_View view, Predicate<UIElement> predicate)
			=> view._children.GetReverseEnumerator(predicate);
		#endregion

		#region HitTest tracing
#if TRACE_HIT_TESTING
		[ThreadStatic]
		private static StringBuilder? _trace;

		[ThreadStatic]
		private static UIElement? _traceSubject;

		private static IDisposable BEGIN_TRACE()
		{
			_trace = new StringBuilder();

			return Disposable.Create(() =>
			{
				Debug.WriteLine(_trace.ToString());
				_trace = null;
			});
		}

		private static IDisposable ENSURE_TRACE()
			=> _trace is null ? BEGIN_TRACE() : Disposable.Empty;

		private static IDisposable SET_TRACE_SUBJECT(UIElement element)
		{
			if (_trace is { })
			{
				var previous = _traceSubject;
				_traceSubject = element;

				_trace.AppendLine(_traceSubject.GetDebugIdentifier());

				return Disposable.Create(() => _traceSubject = previous);
			}
			else
			{
				return Disposable.Empty;
			}
		}
#endif

		[Conditional("TRACE_HIT_TESTING")]
		private static void TRACE(FormattableString msg)
		{
#if TRACE_HIT_TESTING
			if (_trace is { })
			{
				_trace.Append(_traceSubject.GetDebugIndent(subLine: true));
				_trace.Append(' ');
				_trace.Append(Uno.Extensions.FormattableExtensions.ToStringInvariant(msg));
				_trace.Append("\r\n");
			}
#endif
		}
		#endregion

		internal struct Branch
		{
			public static Branch ToPublicRoot(UIElement leaf)
				=> new Branch(
					leaf.XamlRoot?.VisualTree?.RootElement ?? throw new InvalidOperationException("Element must be part of a visual tree"),
					leaf);

			public Branch(UIElement root, UIElement leaf)
			{
				Root = root;
				Leaf = leaf;
			}

			public readonly UIElement Root;
			public readonly UIElement Leaf;

			public void Deconstruct(out UIElement root, out UIElement leaf)
			{
				root = Root;
				leaf = Leaf;
			}

			/// <summary>
			///
			/// </summary>
			/// <remarks>This method will pass through native element but will enumerate only UIElements</remarks>
			/// <returns></returns>
			public IEnumerable<UIElement> EnumerateLeafToRoot()
			{
				var current = Leaf;

				yield return Leaf;

				while (current != Root)
				{
					var parentDo = GetParent(current);
					while ((current = parentDo as UIElement) is null)
					{
						parentDo = GetParent(parentDo!);
					}

					yield return current;
				}
			}

			public bool Contains(UIElement element)
			{
				var current = Leaf;
				if (current == element)
				{
					return true;
				}

				while (current != Root)
				{
					var parentDo = GetParent(current);
					while ((current = parentDo as UIElement) is null)
					{
						parentDo = GetParent(parentDo!);
					}

					if (current == element)
					{
						return true;
					}
				}

				return false;
			}

			public override string ToString() => $"Root={Root.GetDebugName()} | Leaf={Leaf.GetDebugName()}";
		}
	}
}
