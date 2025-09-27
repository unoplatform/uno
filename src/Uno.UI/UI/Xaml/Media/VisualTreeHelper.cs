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

#if __APPLE_UIKIT__
using UIKit;
using _View = UIKit.UIView;
using _ViewGroup = UIKit.UIView;
#elif __ANDROID__
using _View = Android.Views.View;
using _ViewGroup = Android.Views.ViewGroup;
#else
using _View = Microsoft.UI.Xaml.UIElement;
using _ViewGroup = Microsoft.UI.Xaml.UIElement;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Shapes;
#endif

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
			=> FindElementsInHostCoordinates(intersectingPoint, subtree, false);

		[Uno.NotImplemented]
		public static IEnumerable<UIElement> FindElementsInHostCoordinates(Rect intersectingRect, UIElement/* ? */ subtree)
		{
			throw new NotSupportedException();
		}

		public static IEnumerable<UIElement> FindElementsInHostCoordinates(Point intersectingPoint, UIElement/* ? */ subtree, bool includeAllElements)
		{
			if (subtree != null)
			{
				if (IsElementIntersecting(intersectingPoint, subtree))
				{
					yield return subtree;
				}

				foreach (var child in subtree.GetChildren())
				{
#if __ANDROID__ || __APPLE_UIKIT__
					// On Wasm and Skia, child is always UIElement.
					if (child is not UIElement uiElement)
					{
						continue;
					}
#else
					var uiElement = child;
#endif
					var canTest = includeAllElements
						|| (uiElement.IsHitTestVisible && uiElement.IsViewHit());

					if (canTest)
					{
						if (IsElementIntersecting(intersectingPoint, uiElement))
						{
							yield return uiElement;
						}

						foreach (var subChild in FindElementsInHostCoordinates(intersectingPoint, uiElement, includeAllElements))
						{
							yield return subChild;
						}
					}
				}
			}
		}

		private static bool IsElementIntersecting(Point intersectingPoint, UIElement uiElement)
		{
			GeneralTransform transformToRoot = uiElement.TransformToVisual(null);
			var target = transformToRoot.TransformBounds(LayoutInformation.GetLayoutSlot(uiElement));
			return target.Contains(intersectingPoint);
		}

		[Uno.NotImplemented]
		public static IEnumerable<UIElement> FindElementsInHostCoordinates(Rect intersectingRect, UIElement/* ? */ subtree, bool includeAllElements)
		{
			throw new NotSupportedException();
		}

		public static DependencyObject/* ? */ GetChild(DependencyObject reference, int childIndex)
		{
#if XAMARIN
			return (reference as _ViewGroup)?
				.GetChildren()
				.OfType<DependencyObject>()
				.Where(c => c is not ElementStub)
				.ElementAtOrDefault(childIndex);
#else
			return (reference as UIElement)?
				.GetChildren()
				.Where(c => c is not ElementStub)
				.ElementAtOrDefault(childIndex);
#endif
		}

		public static int GetChildrenCount(DependencyObject reference)
		{
#if XAMARIN
			return (reference as _ViewGroup)?
				.GetChildren()
				.OfType<DependencyObject>()
				.Count(c => c is not ElementStub) ?? 0;
#else
			return (reference as UIElement)?
				.GetChildren()
				.Count(c => c is not ElementStub) ?? 0;
#endif
		}

		internal static int GetViewGroupChildrenCount(_ViewGroup reference)
#if __CROSSRUNTIME__ || IS_UNIT_TESTS
			=> reference.GetChildren().Count;
#else
			=> reference.GetChildren().Count();
#endif

		internal static void AddView(_ViewGroup parent, _View child, int index)
		{
#if __APPLE_UIKIT__
			parent.InsertSubview(child, index);
#elif __ANDROID__
			parent.AddView(child, index);
#elif __CROSSRUNTIME__
			parent.AddChild(child, index);
#elif IS_UNIT_TESTS
			if (parent is FrameworkElement fe)
			{
				fe.AddChild(child, index);
			}
			else
			{
				throw new NotSupportedException("AddView on UIElement is not implemented on IS_UNIT_TESTS.");
			}
#else
			throw new NotSupportedException("AddView not implemented on this platform.");
#endif
		}

		internal static void AddView(_ViewGroup parent, _View child)
		{
#if __APPLE_UIKIT__
			parent.AddSubview(child);
#elif __ANDROID__
			parent.AddView(child);
#else
			parent.AddChild(child);
#endif
		}

		internal static void RemoveView(_ViewGroup parent, _View child)
		{
#if __APPLE_UIKIT__
			child.RemoveFromSuperview();
#elif __ANDROID__
			parent.RemoveView(child);
#elif __CROSSRUNTIME__
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
#if XAMARIN
			realParent = (reference as _ViewGroup)?
				.FindFirstParent<DependencyObject>();
#endif

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

		/// <summary>
		/// Adapts a native view by wrapping it in a <see cref="FrameworkElement"/> container so that it can be added to the managed visual tree.
		/// </summary>
		/// <remarks>
		/// This method is present to support adding native view types on Android, iOS and MacOS to Uno's visual tree.
		///
		/// Calling it with a type that's already a <see cref="FrameworkElement"/> will throw an <see cref="InvalidOperationException"/>.
		/// If there's a possibility that the wrapped type may be a <see cref="FrameworkElement"/>, use <see cref="TryAdaptNative(_View)"/>
		/// instead.
		/// </remarks>
		public static FrameworkElement AdaptNative(_View nativeView)
		{
			if (nativeView is FrameworkElement)
			{
				throw new InvalidOperationException($"{nameof(AdaptNative)}() should only be called for non-{nameof(FrameworkElement)} native views." +
					$"Use {nameof(TryAdaptNative)} if it's not known whether view will be native.");
			}

			var host = new ContentPresenter
			{
				IsNativeHost = true,
				Content = nativeView,
				ContentTemplate = null
			};

			// Propagate layout-related attached properties to the managed wrapper, so the host panel takes them into account
			PropagateAttachedProperties(
				host,
				nativeView,
				Grid.RowProperty,
				Grid.RowSpanProperty,
				Grid.ColumnProperty,
				Grid.ColumnSpanProperty,
				Canvas.LeftProperty,
				Canvas.TopProperty,
				Canvas.ZIndexProperty
			);

			return host;
		}

		private static void PropagateAttachedProperties(FrameworkElement host, _View nativeView, params DependencyProperty[] properties)
		{
			foreach (var property in properties)
			{
				host.SetValue(property, nativeView.GetValue(property));
			}
		}

		/// <summary>
		/// Adapts a native view by wrapping it in a <see cref="FrameworkElement"/> container so that it can be added to the managed visual tree.
		///
		/// This method is safe to call for any view. If <paramref name="view"/> is a <see cref="FrameworkElement"/>, it will simply be returned unmodified.
		/// </summary>
		public static FrameworkElement TryAdaptNative(_View view)
		{
			if (view is FrameworkElement fe)
			{
				return fe;
			}

			return AdaptNative(view);
		}

#nullable enable
		public static IEnumerable<T> GetChildren<T>(DependencyObject view)
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

		public static IEnumerable<DependencyObject> GetChildren(DependencyObject view)
			=> GetChildren<DependencyObject>(view);

		internal static void AddChild(UIElement view, UIElement child)
		{
#if __ANDROID__
			view.AddView(child);
#elif __APPLE_UIKIT__
			view.AddSubview(child);
#elif __CROSSRUNTIME__
			view.AddChild(child);
#elif IS_UNIT_TESTS
			if (view is FrameworkElement fe)
			{
				fe.AddChild(child);
			}
			else
			{
				throw new NotImplementedException("AddChild on UIElement is not implemented on IS_UNIT_TESTS.");
			}
#else
			throw new NotImplementedException("AddChild not implemented on this platform.");
#endif
		}

		internal static void RemoveChild(UIElement view, UIElement child)
		{
#if __ANDROID__
			view.RemoveView(child);
#elif __APPLE_UIKIT__
			if (child.Superview == view)
			{
				child.RemoveFromSuperview();
			}
#elif __CROSSRUNTIME__
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
#if __ANDROID__
			view.RemoveAllViews();
#elif __APPLE_UIKIT__
			var children = view.ChildrenShadow;
			children.ForEach(v => v.RemoveFromSuperview());
#elif __CROSSRUNTIME__
			view.ClearChildren();
#else
			throw new NotImplementedException("ClearChildren not implemented on this platform.");
#endif
		}

		internal static readonly GetHitTestability DefaultGetTestability = elt => (elt.GetHitTestVisibility(), DefaultGetTestability!);

		internal static (UIElement? element, Branch? stale) HitTest(
			Point position,
			XamlRoot? xamlRoot,
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

			if (xamlRoot?.VisualTree.RootElement is UIElement root)
			{
				return SearchDownForTopMostElementAt(position, root, getTestability ?? DefaultGetTestability, isStale);
			}

			return default;
		}

		internal static (UIElement? element, Branch? stale) HitTest(
			Point position,
			XamlRoot? xamlRoot,
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
			if (xamlRoot?.VisualTree.RootElement is UIElement root)
			{
				return SearchDownForTopMostElementAt(position, root, getTestability ?? DefaultGetTestability, isStale);
			}

			return default;
		}

		/// <param name="position">
		/// On skia: The absolute position relative to the window origin.
		/// Everywhere else: The position relative to the parent (i.e. the position in parent coordinates).
		/// </param>
		internal static (UIElement? element, Branch? stale) SearchDownForTopMostElementAt(
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
			var elementToRoot = UIElement.GetTransform(element, null);

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
#elif __WASM__
				.GetReverseEnumerator();
#else
				.Reverse()
				.GetEnumerator();
#endif

			while (child.MoveNext())
			{
				var childResult = SearchDownForTopMostElementAt(testPosition, child.Current!, getVisibility, isChildStale);

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
#elif __WASM__
				&& element.HitTest(testPosition)
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
#elif __WASM__
				.GetReverseEnumerator();
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

#if __APPLE_UIKIT__ || __ANDROID__
		/// <summary>
		/// Gets all immediate UIElement children of this <paramref name="view"/>. If any immediate subviews are native, it will descend into
		/// them depth-first until it finds a UIElement, and return those UIElements.
		/// </summary>
		internal static IEnumerable<UIElement> GetManagedVisualChildren(_ViewGroup view)
		{
			foreach (var child in view.GetChildren())
			{
				if (child is UIElement uiElement)
				{
					yield return uiElement;
				}
				else if (child is _ViewGroup childVG)
				{
					foreach (var firstManagedChild in GetManagedVisualChildren(childVG))
					{
						yield return firstManagedChild;
					}
				}
			}
		}
#elif IS_UNIT_TESTS
		internal static IEnumerable<UIElement> GetManagedVisualChildren(_View view)
			=> view.GetChildren();
#else
		internal static MaterializableList<UIElement> GetManagedVisualChildren(_View view)
			=> view._children;
#endif

#if __APPLE_UIKIT__ || __ANDROID__ || IS_UNIT_TESTS
		internal static IEnumerator<UIElement> GetManagedVisualChildrenReversedEnumerator(_View view)
			=> GetManagedVisualChildren(view).Reverse().GetEnumerator();
#else
		internal static MaterializableList<UIElement>.ReverseEnumerator GetManagedVisualChildrenReversedEnumerator(_View view)
			=> view._children.GetReverseEnumerator();
#endif

#if __APPLE_UIKIT__ || __ANDROID__ || IS_UNIT_TESTS
		internal static IEnumerator<UIElement> GetManagedVisualChildrenReversedEnumerator(_View view, Predicate<UIElement> predicate)
			=> GetManagedVisualChildren(view).Where(elt => predicate(elt)).Reverse().GetEnumerator();
#else
		internal static MaterializableList<UIElement>.ReverseReduceEnumerator GetManagedVisualChildrenReversedEnumerator(_View view, Predicate<UIElement> predicate)
			=> view._children.GetReverseEnumerator(predicate);
#endif
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
