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

#if __IOS__
using UIKit;
using _View = UIKit.UIView;
using _ViewGroup = UIKit.UIView;
#elif __MACOS__
using AppKit;
using _View = AppKit.NSView;
using _ViewGroup = AppKit.NSView;
#elif __ANDROID__
using _View = Android.Views.View;
using _ViewGroup = Android.Views.ViewGroup;
#else
using _View = Microsoft.UI.Xaml.UIElement;
using _ViewGroup = Microsoft.UI.Xaml.UIElement;
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
#if __ANDROID__ || __IOS__ || __MACOS__
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
			var target = transformToRoot.TransformBounds(uiElement.LayoutSlot);
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
				.ElementAtOrDefault(childIndex);
#else
			return (reference as UIElement)?
				.GetChildren()
				.ElementAtOrDefault(childIndex);
#endif
		}

		internal static _View GetViewGroupChild(_ViewGroup reference, int childIndex) => (reference as _ViewGroup)?.GetChildren().ElementAtOrDefault(childIndex);

		public static int GetChildrenCount(DependencyObject reference)
		{
#if XAMARIN
			return (reference as _ViewGroup)?
				.GetChildren()
				.OfType<DependencyObject>()
				.Count() ?? 0;
#else
			return (reference as UIElement)?
				.GetChildren()
				.Count ?? 0;
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
#if __MACOS__
			if (index == 0)
			{
				if (parent.Subviews.Length == 0)
				{
					parent.AddSubview(child);
				}
				else
				{
					parent.AddSubview(child, NSWindowOrderingMode.Below, null);
				}
			}
			else
			{
				parent.AddSubview(child, NSWindowOrderingMode.Above, parent.Subviews[index - 1]);
			}
#elif __IOS__
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
#if __IOS__ || __MACOS__
			parent.AddSubview(child);
#elif __ANDROID__
			parent.AddView(child);
#else
			parent.AddChild(child);
#endif
		}

		internal static void RemoveView(_ViewGroup parent, _View child)
		{
#if __IOS__ || __MACOS__
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
			if (window.RootElement?.XamlRoot?.VisualTree is { } visualTree) // TODO:MZ: Verify if there is not a better way
			{
				return GetOpenPopups(visualTree);
			}

			return Array.Empty<Popup>();
		}

		private static IReadOnlyList<Popup> GetOpenFlyoutPopups(XamlRoot xamlRoot) =>
			GetOpenPopups(xamlRoot.VisualTree)
				.Where(p => p.IsForFlyout)
				.ToList()
				.AsReadOnly();

		public static IReadOnlyList<Popup> GetOpenPopupsForXamlRoot(XamlRoot xamlRoot) =>
			GetOpenPopups(xamlRoot.VisualTree);

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

			return realParent;
		}

		internal static void CloseAllPopups(XamlRoot xamlRoot)
		{
			foreach (var popup in GetOpenPopups(xamlRoot.VisualTree))
			{
				popup.IsOpen = false;
			}
		}

		internal static void CloseLightDismissPopups(XamlRoot xamlRoot)
		{
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
				Content = nativeView
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

		public static IEnumerable<DependencyObject> GetChildren(DependencyObject view)
			=> GetChildren<DependencyObject>(view);

		internal static void AddChild(UIElement view, UIElement child)
		{
#if __ANDROID__
			view.AddView(child);
#elif __IOS__ || __MACOS__
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
#elif __IOS__ || __MACOS__
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

		internal static IReadOnlyList<_View> ClearChildren(UIElement view)
		{
#if __ANDROID__
			var children = GetChildren<_View>(view).ToList();
			view.RemoveAllViews();

			return children;
#elif __IOS__ || __MACOS__
			var children = view.ChildrenShadow.ToList();
			children.ForEach(v => v.RemoveFromSuperview());

			return children;
#elif __CROSSRUNTIME__
			var children = GetChildren<_View>(view).ToList();
			view.ClearChildren();

			return children;
#else
			throw new NotImplementedException("ClearChildren not implemented on this platform.");
#endif
		}

		internal static readonly GetHitTestability DefaultGetTestability = elt => (elt.GetHitTestVisibility(), DefaultGetTestability!);

		internal static (UIElement? element, Branch? stale) HitTest(
			Point position,
			XamlRoot? xamlRoot,
			GetHitTestability? getTestability = null,
			StalePredicate? isStale = null
#if TRACE_HIT_TESTING
			, [CallerMemberName] string caller = "")
		{
			using var _ = BEGIN_TRACE();
			TRACE($"[{caller!.ToUpperInvariant()}] @{position.ToDebugString()}");
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

		private static (UIElement? element, Branch? stale) SearchDownForTopMostElementAt(
			Point posRelToParent,
			UIElement element,
			GetHitTestability getVisibility,
			StalePredicate? isStale = null)
		{
			var stale = default(Branch?);
			HitTestability elementHitTestVisibility;
			(elementHitTestVisibility, getVisibility) = getVisibility(element);

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

			// First compute the transformation between the element and its parent coordinate space
			var matrix = Matrix3x2.Identity;
			element.ApplyRenderTransform(ref matrix);
			element.ApplyLayoutTransform(ref matrix);
			element.ApplyElementCustomTransform(ref matrix);
			element.ApplyFlowDirectionTransform(ref matrix);

			TRACE($"- transform to parent: [{matrix.M11:F2},{matrix.M12:F2} / {matrix.M21:F2},{matrix.M22:F2} / {matrix.M31:F2},{matrix.M32:F2}]");

			// Build 'position' in the current element coordinate space
			var posRelToElement = matrix.Inverse().Transform(posRelToParent);
			TRACE($"- position relative to element: {posRelToElement.ToDebugString()} | relative to parent: {posRelToParent.ToDebugString()}");

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

			// Validate that the pointer is in the bounds of the element
			if (!clippingBounds.Contains(posRelToElement))
			{
				// Even if out of bounds, if the element is stale, we search down for the real stale leaf
				if (isStale is not null)
				{
					if (isStale.Value.Method(element))
					{
						TRACE($"- Is {isStale.Value.Name}");

						stale = SearchDownForStaleBranch(element, isStale.Value);
					}
					else
					{
						TRACE($"- Is NOT {isStale.Value.Name}");
					}
				}

				TRACE($"> NOT FOUND (Out of the **clipped** bounds) | stale branch: {stale?.ToString() ?? "-- none --"}");
				return (default, stale);
			}

			// Validate if any child is an acceptable target
			var children = GetManagedVisualChildren(element);

			var isChildStale = isStale;

			using var child = children
#if __IOS__ || __MACOS__ || __ANDROID__ || IS_UNIT_TESTS
				.Reverse().GetEnumerator();
#else
				// On Skia and Wasm, we can get concrete data structure (MaterializableList in this case) instead of IEnumerable<T>.
				// It has an efficient "ReverseEnumerator". This will also avoid the boxing allocations of the enumerator when it's a struct.
				.GetReverseEnumerator();
#endif

			while (child.MoveNext())
			{
				var childResult = SearchDownForTopMostElementAt(posRelToElement, child.Current!, getVisibility, isChildStale);

				// If we found a stale element in child sub-tree, keep it and stop looking for stale elements
				if (childResult.stale is not null)
				{
					stale = childResult.stale;
					isChildStale = null;
				}

				// If we found an acceptable element in the child's sub-tree, job is done!
				if (childResult.element is not null)
				{
					if (isChildStale is not null) // Also indicates that stale is null
					{
						// If we didn't find any stale root in previous children or in the child's sub tree,
						// we continue to enumerate sibling children to detect a potential stale root.

						TRACE($"+ Searching for stale {isChildStale.Value.Name} branch.");

						while (child.MoveNext())
						{
#if TRACE_HIT_TESTING
							using var __ = SET_TRACE_SUBJECT(child.Current);
#endif

							if (isChildStale.Value.Method(child.Current))
							{
								TRACE($"- Is {isChildStale.Value.Name}");

								stale = SearchDownForStaleBranch(child.Current!, isChildStale.Value);

#if TRACE_HIT_TESTING
								while (child.MoveNext())
								{
									using var ___ = SET_TRACE_SUBJECT(child.Current);
									if (isChildStale.Value.Method(child.Current))
									{
										//Debug.Assert(false);
										TRACE($"- Is {isChildStale.Value.Name} ***** INVALID: Only one branch can be considered as stale at once! ****");
									}
									TRACE($"> Ignored since leaf and stale branch has already been found.");
								}
#endif

								break;
							}
							else
							{
								TRACE($"- Is NOT {isChildStale.Value.Name}");
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

			// We didn't find any child at the given position, validate that element can be touched (i.e. not HitTestability.Invisible),
			// and the position is in actual bounds (which might be different than the clipping bounds)
			if (elementHitTestVisibility == HitTestability.Visible && renderingBounds.Contains(posRelToElement))
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
			using var enumerator = GetManagedVisualChildren(root)
#if __IOS__ || __MACOS__ || __ANDROID__ || IS_UNIT_TESTS
				.Reverse().GetEnumerator();
#else
				.GetReverseEnumerator();
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

		internal static IEnumerable<DependencyObject> EnumerateAncestors(DependencyObject o)
		{
			while ((o as FrameworkElement)?.Parent is { } parent)
			{
				yield return parent;
				o = parent;
			}
		}

		#region Helpers
		private static Func<IEnumerable<UIElement>, IEnumerable<UIElement>> Except(UIElement element)
			=> children => children.Except(element);

		private static Func<IEnumerable<UIElement>, IEnumerable<UIElement>> SkipUntil(UIElement element)
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

#if __IOS__ || __MACOS__ || __ANDROID__
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
				_trace.Append(msg.ToStringInvariant());
				_trace.Append("\r\n");
			}
#endif
		}
		#endregion

		internal struct Branch
		{
			public static Branch ToPublicRoot(UIElement leaf)
				=> new Branch(
					leaf.XamlRoot?.VisualTree?.PublicRootVisual ?? throw new InvalidOperationException("Element must be part of a visual tree"), 
					leaf); // TODO:MZ: Multi-window support

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

			public override string ToString() => $"Root={Root.GetDebugName()} | Leaf={Leaf.GetDebugName()}";
		}
	}
}
