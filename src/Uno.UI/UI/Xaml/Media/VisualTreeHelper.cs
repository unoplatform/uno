#nullable disable // Not supported by WinUI yet
// #define TRACE_HIT_TESTING

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.Extensions;
using Uno.Disposables;
using Windows.Globalization.DateTimeFormatting;
using Windows.UI.Core;
using Uno.Logging;
using Uno.UI.Extensions;

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
using _View = Windows.UI.Xaml.UIElement;
using _ViewGroup = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Media
{
	public partial class VisualTreeHelper
	{
		private static readonly List<WeakReference<IPopup>> _openPopups = new List<WeakReference<IPopup>>();

		internal static IDisposable RegisterOpenPopup(IPopup popup)
		{
			var weakPopup = new WeakReference<IPopup>(popup);

			_openPopups.AddDistinct(weakPopup);
			return Disposable.Create(() => _openPopups.Remove(weakPopup));
		}

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

				foreach (var child in subtree.GetChildren().OfType<UIElement>())
				{
					var canTest = includeAllElements
						|| (child.IsHitTestVisible && child.IsViewHit());

					if (child is UIElement uiElement && canTest)
					{
						if (IsElementIntersecting(intersectingPoint, uiElement))
						{
							yield return uiElement;
						}

						foreach (var subChild in FindElementsInHostCoordinates(intersectingPoint, child, includeAllElements))
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
				.OfType<DependencyObject>()
				.ElementAtOrDefault(childIndex);
#endif
		}

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
				.OfType<DependencyObject>()
				.Count() ?? 0;
#endif
		}

		public static IReadOnlyList<Popup> GetOpenPopups(Window window)
		{
			return _openPopups
				.Select(WeakReferenceExtensions.GetTarget)
				.OfType<Popup>()
				.ToList()
				.AsReadOnly();
		}

		public static IReadOnlyList<Popup> GetOpenPopupsForXamlRoot(XamlRoot xamlRoot)
		{
			if (xamlRoot == XamlRoot.Current)
			{
				return GetOpenPopups(Window.Current);
			}

			return new Popup[0];
		}


		public static DependencyObject/* ? */ GetParent(DependencyObject reference)
		{
#if XAMARIN
			return (reference as _ViewGroup)?
				.FindFirstParent<DependencyObject>();
#else
			return reference.GetParent() as DependencyObject;
#endif
		}

		internal static void CloseAllPopups()
		{
			foreach (var popup in GetOpenPopups(Window.Current))
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

			return new ContentPresenter
			{
				IsNativeHost = true,
				Content = nativeView
			};
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
#elif UNO_REFERENCE_API
			view.AddChild(child);
#else
			throw new NotImplementedException("AddChild not implemented on this platform.");
#endif
		}

		internal static void RemoveChild(UIElement view, UIElement child)
		{
#if __ANDROID__
			view.RemoveView(child);
#elif __IOS__ || __MACOS__
			if(child.Superview == view)
			{
				child.RemoveFromSuperview();
			}
#elif UNO_REFERENCE_API
			view.RemoveChild(child);
#else
			throw new NotImplementedException("AddChild not implemented on this platform.");
#endif
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
#elif UNO_REFERENCE_API
			var children = GetChildren<_View>(view).ToList();
			view.ClearChildren();

			return children;
#else
			throw new NotImplementedException("ClearChildren not implemented on this platform.");
#endif
		}

		internal static readonly GetHitTestability DefaultGetTestability;
		static VisualTreeHelper()
		{
			DefaultGetTestability = elt => (elt.GetHitTestVisibility(), DefaultGetTestability!);
		}

		internal static (UIElement? element, Branch? stale) HitTest(
			Point position,
			GetHitTestability? getTestability = null,
			Predicate<UIElement>? isStale = null
#if TRACE_HIT_TESTING
			, [CallerMemberName] string? caller = null)
		{
			using var _ = BEGIN_TRACE();
			TRACE($"[{caller!.Replace("CoreWindow_Pointer", "").ToUpperInvariant()}] @{position.ToDebugString()}");
#else
			)
		{
#endif
			if (Window.Current.RootElement is UIElement root)
			{
				return SearchDownForTopMostElementAt(position, root, getTestability ?? DefaultGetTestability, isStale);
			}

			return default;
		}

		private static (UIElement? element, Branch? stale) SearchDownForTopMostElementAt(
			Point posRelToParent,
			UIElement element,
			GetHitTestability getVisibility,
			Predicate<UIElement>? isStale = null,
			Func<IEnumerable<UIElement>, IEnumerable<UIElement>>? childrenFilter = null)
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
				if (isStale?.Invoke(element) ?? false)
				{
					stale = SearchDownForStaleBranch(element, isStale);
				}

				TRACE($"> NOT FOUND (Element is HitTestability.Collapsed) | stale branch: {stale?.ToString() ?? "-- none --"}");
				return (default, stale);
			}

			// The region where the element was arrange by its parent.
			// This is expressed in parent coordinate space
			var layoutSlot = element.LayoutSlotWithMarginsAndAlignments;

			// The maximum region where the current element and its children might draw themselves
			// TODO: Get the real clipping rect! For now we assume no clipping.
			// This is expressed in element coordinate space.
			var clippingBounds = Rect.Infinite;

			// The region where the current element draws itself.
			// Be aware that children might be out of this rendering bounds if no clipping defined. TODO: .Intersect(clippingBounds)
			// This is expressed in element coordinate space.
			var renderingBounds = new Rect(new Point(), layoutSlot.Size);

			// First compute the 'position' in the current element coordinate space
			var posRelToElement = posRelToParent;

			posRelToElement.X -= layoutSlot.X;
			posRelToElement.Y -= layoutSlot.Y;

			var renderTransform = element.RenderTransform;
			if (renderTransform != null)
			{
				var parentToElement = renderTransform.MatrixCore.Inverse();

				TRACE($"- renderTransform: [{parentToElement.M11:F2},{parentToElement.M12:F2} / {parentToElement.M21:F2},{parentToElement.M22:F2} / {parentToElement.M31:F2},{parentToElement.M32:F2}]");

				posRelToElement = parentToElement.Transform(posRelToElement);
				renderingBounds = parentToElement.Transform(renderingBounds);
			}

#if !UNO_HAS_MANAGED_SCROLL_PRESENTER
			// On Skia, the Scrolling is managed by the ScrollContentPresenter (as UWP), which is flagged as IsScrollPort.
			// Note: We should still add support for the zoom factor ... which is not yet supported on Skia.
			if (element is ScrollViewer sv)
			{
				var zoom = sv.ZoomFactor;

				TRACE($"- scroller: x={sv.HorizontalOffset} | y={sv.VerticalOffset} | zoom={zoom}");

				// Note: This is probably wrong for skia as the zoom is probably also handled by the ScrollContentPresenter
				posRelToElement.X /= zoom;
				posRelToElement.Y /= zoom;

				posRelToElement.X += sv.HorizontalOffset;
				posRelToElement.Y += sv.VerticalOffset;

				renderingBounds = new Rect(renderingBounds.Location, new Size(sv.ExtentWidth, sv.ExtentHeight));
			}
			else
#endif
#if !__MACOS__ // On macOS the SCP is using RenderTransforms for scrolling which has already been included.
			if (element.IsScrollPort)
			{
				posRelToElement.X += element.ScrollOffsets.X;
				posRelToElement.Y += element.ScrollOffsets.Y;
			}
#endif

			TRACE($"- layoutSlot: {layoutSlot.ToDebugString()}");
			TRACE($"- renderBounds (relative to element): {renderingBounds.ToDebugString()}");
			TRACE($"- clippingBounds (relative to element): {clippingBounds.ToDebugString()}");
			TRACE($"- position relative to element: {posRelToElement.ToDebugString()} | relative to parent: {posRelToParent.ToDebugString()}");

			// Validate that the pointer is in the bounds of the element
			if (!clippingBounds.Contains(posRelToElement))
			{
				// Even if out of bounds, if the element is stale, we search down for the real stale leaf
				if (isStale?.Invoke(element) ?? false)
				{
					stale = SearchDownForStaleBranch(element, isStale);
				}

				TRACE($"> NOT FOUND (Out of the **clipped** bounds) | stale branch: {stale?.ToString() ?? "-- none --"}");
				return (default, stale);
			}

			// Validate if any child is an acceptable target
			var children = childrenFilter is null ? element.GetChildren().OfType<UIElement>() : childrenFilter(element.GetChildren().OfType<UIElement>());
			using var child = children.Reverse().GetEnumerator();
			var isChildStale = isStale;
			while (child.MoveNext())
			{
				var childResult = SearchDownForTopMostElementAt(posRelToElement, child.Current!, getVisibility, isChildStale);

				// If we found a stale element in child sub-tree, keep it and stop looking for stale elements
				if (childResult.stale is { })
				{
					stale = childResult.stale;
					isChildStale = null;
				}

				// If we found an acceptable element in the child's sub-tree, job is done!
				if (childResult.element is { })
				{
					if (isChildStale is { }) // Also indicates that stale is null
					{
						// If we didn't find any stale root in previous children or in the child's sub tree,
						// we continue to enumerate sibling children to detect a potential stale root.

						while (child.MoveNext())
						{
							if (isChildStale(child.Current))
							{
								stale = SearchDownForStaleBranch(child.Current!, isChildStale);
								break;
							}
						}
					}

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
				if (isStale?.Invoke(element) ?? false)
				{
					stale = new Branch(element, stale?.Leaf ?? element);
				}

				TRACE($"> NOT FOUND (HitTestability.Invisible or out of the **render** bounds) | stale branch: {stale?.ToString() ?? "-- none --"}");
				return (default, stale);
			}
		}

		private static Branch SearchDownForStaleBranch(UIElement staleRoot, Predicate<UIElement> isStale)
			=> new Branch(staleRoot, SearchDownForLeaf(staleRoot, isStale));

		internal static UIElement SearchDownForLeaf(UIElement root, Predicate<UIElement> predicate)
		{
			foreach (var child in root.GetChildren().OfType<UIElement>().Reverse())
			{
				if (predicate(child))
				{
					return SearchDownForLeaf(child, predicate);
				}
			}

			return root;
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

		private static IDisposable SET_TRACE_SUBJECT(UIElement element)
		{
			if (_trace is { })
			{
				var previous = _traceSubject;
				_traceSubject = element;

				_trace.Append(new string('\t', _traceSubject.Depth - 1));
				_trace.Append($"[{element.GetDebugName()}]\r\n");

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
				_trace.Append(new string('\t', _traceSubject?.Depth ?? 0));
				_trace.Append(msg.ToStringInvariant());
				_trace.Append("\r\n");
			}
#endif
		}
		#endregion

		internal struct Branch
		{
			public static Branch ToWindowRoot(UIElement leaf)
				=> new Branch(Window.Current.RootElement, leaf);

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
