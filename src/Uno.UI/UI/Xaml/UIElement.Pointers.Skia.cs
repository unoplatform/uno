#nullable enable
#define TRACE_HIT_TESTING

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Input;
using Windows.UI.Input;
using Windows.UI.Xaml.Input;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;
using Uno.Foundation.Extensibility;
using Windows.UI.Core;
using Windows.Foundation;
using System.Threading;
using System.Numerics;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Controls;
using Uno.Disposables;
using Uno.Extensions.ValueType;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Extensions;

namespace Windows.UI.Xaml
{
	partial class UIElement
	{
		private class PointerManager
		{
			private struct Branch
			{
				public Branch(UIElement root, UIElement leaf)
				{
					Root = root;
					Leaf = leaf;
				}

				public UIElement Root;
				public UIElement Leaf;

				public void Deconstruct(out UIElement root, out UIElement leaf)
				{
					root = Root;
					leaf = Leaf;
				}

				/// <inheritdoc />
				public override string ToString()
					=> $"Root={Root.GetDebugName()} | Leaf={Leaf.GetDebugName()}";
			}

#if TRACE_HIT_TESTING
			[ThreadStatic]
			private static IndentedStringBuilder? _trace;

			[ThreadStatic]
			private static UIElement _traceCurrentElement;

			private static IDisposable BEGIN_TRACE(UIElement element)
			{
				if (_trace is { })
				{
					var previous = _traceCurrentElement;
					_traceCurrentElement = element;

					_trace.Append(new string('\t', _traceCurrentElement.Depth - 1));
					_trace.Append($"[{element.GetDebugName()}]\r\n");

					return Disposable.Create(() => _traceCurrentElement = previous);
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
					_trace.Append(new string('\t', _traceCurrentElement.Depth));
					_trace.Append(msg.ToStringInvariant());
					_trace.Append("\r\n");
				}
#endif
			}

			public PointerManager()
			{
				Window.Current.CoreWindow.PointerMoved += CoreWindow_PointerMoved;
				Window.Current.CoreWindow.PointerEntered += CoreWindow_PointerEntered;
				Window.Current.CoreWindow.PointerExited += CoreWindow_PointerExited;
				Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
				Window.Current.CoreWindow.PointerReleased += CoreWindow_PointerReleased;
				Window.Current.CoreWindow.PointerWheelChanged += CoreWindow_PointerWheelChanged;
			}

			private void CoreWindow_PointerWheelChanged(CoreWindow sender, PointerEventArgs args)
			{
				//if (this.Log().IsEnabled(LogLevel.Trace))
				//{
				//	this.Log().Trace($"CoreWindow_PointerWheelChanged ({args.CurrentPoint.Position})");
				//}

				//PropagateEvent(args, e =>
				//{
				//	var pointerArgs = new PointerRoutedEventArgs(args, e);

				//	e.OnNativePointerWheel(pointerArgs);
				//});


				var source = FindOriginalSource(args, _cache);
				if (source.element is null)
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"CoreWindow_PointerPressed ({args.CurrentPoint.Position}) **undispatched**");
					}

					return;
				}

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerPressed [{source.element}/{source.element.GetHashCode():X8}");
				}

				var routedArgs = new PointerRoutedEventArgs(args, source.element);

				// Second raise the event, either on the OriginalSource or on the capture owners if any
				if (PointerCapture.TryGet(routedArgs.Pointer, out var capture))
				{
					foreach (var target in capture.Targets.ToArray())
					{
						target.Element.OnNativePointerWheel(routedArgs);
					}
				}
				else
				{
					source.element.OnNativePointerWheel(routedArgs);
				}
			}

			private void CoreWindow_PointerEntered(CoreWindow sender, PointerEventArgs args)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerEntered ({args.CurrentPoint.Position})");
				}
			}

			private void CoreWindow_PointerExited(CoreWindow sender, PointerEventArgs args)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerExited ({args.CurrentPoint.Position})");
				}
			}

			private void CoreWindow_PointerReleased(CoreWindow sender, PointerEventArgs args)
			{
				//if (FindOriginalSource(args) is { } originalSource)
				//{
				//	if (this.Log().IsEnabled(LogLevel.Trace))
				//	{
				//		this.Log().Trace($"CoreWindow_PointerReleased [{originalSource}/{originalSource.GetHashCode():X8}");
				//	}

				//	var routedArgs = new PointerRoutedEventArgs(args, originalSource);

				//	if (UIElement.PointerCapture.TryGet(routedArgs.Pointer, out var capture))
				//	{
				//		foreach (var target in capture.Targets.ToArray())
				//		{
				//			target.Element.OnNativePointerUp(routedArgs);
				//		}
				//	}
				//	else
				//	{
				//		originalSource.OnNativePointerUp(routedArgs);
				//	}
				//}
				//else if (this.Log().IsEnabled(LogLevel.Trace))
				//{
				//	this.Log().Trace($"CoreWindow_PointerReleased ({args.CurrentPoint.Position}) **undispatched**");
				//}


				var source = FindOriginalSource(args, _cache);
				if (source.element is null)
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"CoreWindow_PointerPressed ({args.CurrentPoint.Position}) **undispatched**");
					}

					return;
				}

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerPressed [{source.element}/{source.element.GetHashCode():X8}");
				}

				var routedArgs = new PointerRoutedEventArgs(args, source.element);

				// Second raise the event, either on the OriginalSource or on the capture owners if any
				if (PointerCapture.TryGet(routedArgs.Pointer, out var capture))
				{
					foreach (var target in capture.Targets.ToArray())
					{
						target.Element.OnNativePointerUp(routedArgs);
					}
				}
				else
				{
					source.element.OnNativePointerUp(routedArgs);
				}
			}

			private void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs args)
			{
				//var source = FindOriginalSource(args, _pressedCache, isStale: _isPressed);
				//if (source.element is null)
				//{
				//	if (this.Log().IsEnabled(LogLevel.Trace))
				//	{
				//		this.Log().Trace($"CoreWindow_PointerMoved ({args.CurrentPoint.Position}) **undispatched**");
				//	}

				//	return;
				//}

				//if (FindOriginalSource(args) is { } originalSource)
				//{
				//	if (this.Log().IsEnabled(LogLevel.Trace))
				//	{
				//		this.Log().Trace($"CoreWindow_PointerPressed ({args.CurrentPoint.Position}) [{originalSource}/{originalSource.GetHashCode():X8}");
				//	}

				//	var routedArgs = new PointerRoutedEventArgs(args, originalSource);

				//	originalSource.OnNativePointerDown(routedArgs);
				//}
				//else if (this.Log().IsEnabled(LogLevel.Trace))
				//{
				//	this.Log().Trace($"CoreWindow_PointerPressed ({args.CurrentPoint.Position}) **undispatched**");
				//}


				var source = FindOriginalSource(args, _cache);
				if (source.element is null)
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"CoreWindow_PointerPressed ({args.CurrentPoint.Position}) **undispatched**");
					}

					return;
				}

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerPressed [{source.element}/{source.element.GetHashCode():X8}");
				}

				var routedArgs = new PointerRoutedEventArgs(args, source.element);

				source.element.OnNativePointerDown(routedArgs);
			}

			private void CoreWindow_PointerMoved(CoreWindow sender, PointerEventArgs args)
			{
				var source = FindOriginalSource(args, _cache, isStale: _isOver);
				if (source.element is null)
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"CoreWindow_PointerMoved ({args.CurrentPoint.Position}) **undispatched**");
					}

					return;
				}

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerMoved [{source.element}/{source.element.GetHashCode():X8}");
				}

				var routedArgs = new PointerRoutedEventArgs(args, source.element);

				// First raise the PointerExited events on the stale branch
				if (source.stale.HasValue)
				{
					routedArgs.CanBubbleNatively = true; // TODO: UGLY HACK TO AVOID BUBBLING: we should be able to request to bubble only up to a the root
					var (root, stale) = source.stale.Value;

					Debug.Write($"Exiting branch from (root) {root.GetDebugName()} to (leaf) {stale.GetDebugName()}\r\n");

					while (stale is { })
					{
						//Debug.WriteLine($"[EXITED] {GetTraceName(stale)}");

						routedArgs.Handled = false;
						stale.OnNativePointerExited(routedArgs);
						// TODO: This differs of how we behave on iOS, macOS and Android which does have "implicit capture" while pressed.
						//		 It should only impact the "Pressed" visual states of controls.
						stale.SetPressed(routedArgs, isPressed: false, muteEvent: true);

						if (stale == root)
						{
							break;
						}

						stale = stale.GetParent() as UIElement;
					}
					routedArgs.CanBubbleNatively = false;
				}

				// Second (try to) raise the PointerEnter on the OriginalSource
				// Note: This won't do anything if already over
				//Debug.WriteLine($"[ENTER] {GetTraceName(source.element)}");
				routedArgs.Handled = false;
				source.element.OnNativePointerEnter(routedArgs);

				// Second raise the event, either on the OriginalSource or on the capture owners if any
				if (PointerCapture.TryGet(routedArgs.Pointer, out var capture))
				{
					foreach (var target in capture.Targets.ToArray())
					{
						//Debug.WriteLine($"[MOVE] RE-ROUTED TO {GetTraceName(target.Element)}");
						routedArgs.Handled = false;
						target.Element.OnNativePointerMove(routedArgs);
					}
				}
				else
				{
					// Note: We prefer to use the "WithOverCheck" overload as we already know that the pointer is effectively over
					//cDebug.WriteLine($"[MOVE] {GetTraceName(source.element)}");
					routedArgs.Handled = false;
					source.element.OnNativePointerMoveWithOverCheck(routedArgs, isOver: true);
				}
			}

			// TODO: Use pointer ID for the predicates
			private static Predicate<UIElement> _isOver = e => e.IsPointerOver;
			//private static Predicate<UIElement> _isPressed = e => e.IsPointerPressed;

			//private Dictionary<uint, (Rect validity, ManagedWeakReference orginalSource)> _pressedCache = new Dictionary<uint, (Rect, ManagedWeakReference)>();
			//private Dictionary<uint, (Rect validity, ManagedWeakReference orginalSource)> _overCache = new Dictionary<uint, (Rect, ManagedWeakReference)>();
			private Dictionary<uint, (Rect validity, ManagedWeakReference orginalSource)> _cache = new Dictionary<uint, (Rect, ManagedWeakReference)>();
			// TODO: The cache must contains the whole tree: for instance if the leaf is removed from the visual tree,
			//		 we won't be able to walk the tree up to get the stale branch
			// And it's here we we need to have a dedicated cache for the pressed: we must be able reset the state!

			private (UIElement? element, Branch? stale) FindOriginalSource(
				PointerEventArgs args,
				Dictionary<uint, (Rect validity, ManagedWeakReference orginalSource)> cache,
				Predicate<UIElement>? isStale = null,
				[CallerMemberName] string? caller = null)
			{
#if TRACE_HIT_TESTING
				try
				{ 
					_trace = new IndentedStringBuilder();
					_trace.AppendLineInvariant(
						$"[{caller!.Replace("CoreWindow_Pointer", "").ToUpperInvariant()}] "
						+ $"@{args.CurrentPoint.Position.X:F2},{args.CurrentPoint.Position.Y:F2} (args: {args.GetHashCode():X8})");
#endif

				var pointerId = args.CurrentPoint.PointerId;
				//if (cache.TryGetValue(pointerId, out var cached)
				//	&& cached.validity.Contains(args.CurrentPoint.RawPosition)
				//	&& cached.orginalSource.Target is UIElement cachedElement
				//	&& cachedElement.IsHitTestVisibleCoalesced) 
				//{
				//	// Note about cachedElement.IsHitTestVisibleCoalesced
				//	// If not visible, either the auto reset on load/unload should have clean the internal pointers state,
				//	// either the stale branch detection in SearchDownForTopMostElementAt(root) will find it and clear state.

				//	Matrix3x2.Invert(GetTransform(cachedElement, null), out var rootToCachedElement);
				//	var positionInCachedElementCoordinates = rootToCachedElement.Transform(args.CurrentPoint.Position);

				//	var result = SearchUpAndDownForTopMostElementAt(positionInCachedElementCoordinates, cachedElement, isStale);

				//	if (result.element is { })
				//	{
				//		UpdateCache(cache, pointerId, (cached.orginalSource, cachedElement), result.element);
				//		return result;
				//	}

				//	// We walked all the tree up from the provided element, but were not able to find any target!
				//	// Maybe the cached element has been removed from the tree (but the IsLoaded should have been false :/)

				//	this.Log().Warn(
				//		"Enable to find any acceptable original source by walking up the tree from the cached element, "
				//		+ "which is suspicious as the element has not been flag as unloaded."
				//		+ "Trying now by looking down from the root.");
				//}

				if (Window.Current.RootElement is UIElement root)
				{
					var result = SearchDownForTopMostElementAt(args.CurrentPoint.Position, root, isStale);
					UpdateCache(cache, pointerId, default, result.element);
					return result;
				}

				this.Log().Warn("The root element not set yet, impossible to find the original source.");

#if TRACE_HIT_TESTING
				}
				finally
				{
					Debug.WriteLine(_trace);
					_trace = null;
				}
#endif

				return default;
			}

			private void UpdateCache(
				Dictionary<uint, (Rect validity, ManagedWeakReference orginalSource)> cache,
				uint pointerId,
				(ManagedWeakReference weak, UIElement instance)? currentEntry,
				UIElement? updated)
			{
				if (currentEntry.HasValue)
				{
					if (updated == currentEntry.Value.instance)
					{
						return;
					}

					WeakReferencePool.ReturnWeakReference(this, currentEntry.Value.weak);
				}

				if (updated is null)
				{
					cache.Remove(pointerId);
				}
				else
				{
					cache[pointerId] = (
						validity: new Rect(new Point(), new Size(double.PositiveInfinity, double.PositiveInfinity)), // TODO
						orginalSource: WeakReferencePool.RentWeakReference(this, updated)
					);
				}
			}

			private static (UIElement? element, Branch? stale) SearchUpAndDownForTopMostElementAt(
				Point position,
				UIElement element,
				Predicate<UIElement>? isStale)
			{
				var (foundElement, stale) = SearchDownForTopMostElementAt(position, element, isStale);
				if (foundElement is { })
				{
					return (foundElement, stale); // Success match
				}

				// If we already have a stale root (the cached element) avoid the cost to search it again in siblings
				//if (staleRoot is { })
				//{
				//	isStale = default;
				//}

				// Given element is no longer the top most element, we walk the tree upward to find the new element
				// At this point we assume that the pointer is probably not far enough from the cached element,
				// so it's faster to search in sibling walking the tree upward instead of starting from visual root.
				double offsetX = 0, offsetY = 0;
				while (element.TryGetParentUIElementForTransformToVisual(out var parent, ref offsetX, ref offsetY))
				{
					// Compute the position in the parent coordinate space
					position.X += offsetX;
					position.Y += offsetY;

					if (stale is null)
					{
						(foundElement, stale) = SearchDownForTopMostElementAt(position, parent, isStale, excludedChild: element);
					}
					else
					{
						// Do search for the stale branch AND DO NOT ERASE the current stale branch !
						(foundElement, _) = SearchDownForTopMostElementAt(position, parent, excludedChild: element);
					}

					if (foundElement is { })
					{
						return (foundElement, stale);
					}

					if (isStale?.Invoke(parent) ?? false)
					{
						stale = new Branch(parent, stale?.Leaf ?? parent);
					}

					element = parent;
				}

				return (foundElement, stale);
			}

			private static (UIElement? element, Branch? stale) SearchDownForTopMostElementAt(
				Point posRelToParent,
				UIElement element,
				Predicate<UIElement>? isStale = null,
				UIElement? excludedChild = null)
			{
				var stale = default(Branch?);
				var elementHitTestVisibility = (HitTestVisibility)element.GetValue(HitTestVisibilityProperty);

#if TRACE_HIT_TESTING
				using var _ = BEGIN_TRACE(element);
				TRACE($"- hit test visibility: {elementHitTestVisibility}");
#endif

				// If the element is not hit testable, do not even try to validate it nor its children.
				if (elementHitTestVisibility == HitTestVisibility.Collapsed)
				{
					// Even if collapsed, if the element is stale, we search down for the real stale leaf
					if (isStale?.Invoke(element) ?? false)
					{
						stale = SearchDownForStaleBranch(element, isStale);
					}

					TRACE($"> NOT FOUND (Element is HitTestVisibility.Collapsed) | stale branch: {stale?.ToString() ?? "-- none --"}");
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
					Matrix3x2.Invert(renderTransform.MatrixCore, out var parentToElement);

					TRACE($"- renderTransform: [{parentToElement.M11:F2},{parentToElement.M12:F2} / {parentToElement.M21:F2},{parentToElement.M22:F2} / {parentToElement.M31:F2},{parentToElement.M32:F2}]");

					posRelToElement = parentToElement.Transform(posRelToElement);
					renderingBounds = parentToElement.Transform(renderingBounds);
				}

				if (element is ScrollViewer sv) // TODO: ScrollContentPresenter ?
				{
					var zoom = sv.ZoomFactor;

					TRACE($"- scroller: x={sv.HorizontalOffset} | y={sv.VerticalOffset} | zoom={zoom}");

					posRelToElement.X /= zoom;
					posRelToElement.Y /= zoom;

					// No needs to adjust the position:
					// On Skia the scrolling is achieved using a RenderTransform on the content of the ScrollContentPresenter,
					// so it will already be taken in consideration by the case above.
					//posRelToElement.X += sv.HorizontalOffset;
					//posRelToElement.Y += sv.VerticalOffset;

					renderingBounds = new Rect(renderingBounds.Location, new Size(sv.ExtentWidth, sv.ExtentHeight));
				}

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
				var children = excludedChild is null ? element.GetChildren() : element.GetChildren().Except(excludedChild);
				using var child = children.Reverse().GetEnumerator();
				var isChildStale = isStale;
				while (child.MoveNext())
				{
					var childResult = SearchDownForTopMostElementAt(posRelToElement, child.Current, isChildStale);

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
									stale = SearchDownForStaleBranch(child.Current, isChildStale);
									break;
								}
							}
						}

						TRACE($"> found child: {childResult.element.GetDebugName()} | stale branch: {stale?.ToString() ?? "-- none --"}");
						return (childResult.element, stale);
					}
				}

				// We didn't find any child at the given position, validate that element can be touched (i.e. not HitTestVisibility.Invisible),
				// and the position is in actual bounds (which might be different than the clipping bounds)
				if (elementHitTestVisibility == HitTestVisibility.Visible && renderingBounds.Contains(posRelToElement))
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

					TRACE($"> NOT FOUND (HitTestVisibility.Invisible or out of the **render** bounds) | stale branch: {stale?.ToString() ?? "-- none --"}");
					return (default, stale);
				}
			}

			private static Branch SearchDownForStaleBranch(UIElement staleRoot, Predicate<UIElement> isStale)
				=> new Branch(staleRoot, SearchDownForStaleLeaf(staleRoot, isStale));

			private static UIElement SearchDownForStaleLeaf(UIElement staleRoot, Predicate<UIElement> isStale)
			{
				foreach (var child in staleRoot.GetChildren().Reverse())
				{
					if (isStale(child))
					{
						return SearchDownForStaleLeaf(child, isStale);
					}
				}

				return staleRoot;
			}
		}

		// TODO Should be per CoreWindow
		private static PointerManager _pointerManager;

		partial void InitializePointersPartial()
		{
			if (_pointerManager == null)
			{
				_pointerManager = new PointerManager();
			}
		}

		partial void AddPointerHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo)
		{

		}


#region HitTestVisibility
		internal void UpdateHitTest()
		{
			this.CoerceValue(HitTestVisibilityProperty);
		}

		private enum HitTestVisibility
		{
			/// <summary>
			/// The element and its children can't be targeted by hit-testing.
			/// </summary>
			/// <remarks>
			/// This occurs when IsHitTestVisible="False", IsEnabled="False", or Visibility="Collapsed".
			/// </remarks>
			Collapsed,

			/// <summary>
			/// The element can't be targeted by hit-testing, but it's children might be.
			/// </summary>
			/// <remarks>
			/// This usually occurs if an element doesn't have a Background/Fill.
			/// </remarks>
			Invisible,

			/// <summary>
			/// The element can be targeted by hit-testing.
			/// </summary>
			Visible,
		}

		/// <summary>
		/// Represents the final calculated hit-test visibility of the element.
		/// </summary>
		/// <remarks>
		/// This property should never be directly set, and its value should always be calculated through coercion (see <see cref="CoerceHitTestVisibility(DependencyObject, object, bool)"/>.
		/// </remarks>
		private static readonly DependencyProperty HitTestVisibilityProperty =
			DependencyProperty.Register(
				"HitTestVisibility",
				typeof(HitTestVisibility),
				typeof(UIElement),
				new FrameworkPropertyMetadata(
					HitTestVisibility.Visible,
					FrameworkPropertyMetadataOptions.Inherits,
					coerceValueCallback: CoerceHitTestVisibility,
					propertyChangedCallback: OnHitTestVisibilityChanged
				)
			);

		/// <summary>
		/// This calculates the final hit-test visibility of an element.
		/// </summary>
		/// <returns></returns>
		private static object CoerceHitTestVisibility(DependencyObject dependencyObject, object baseValue)
		{
			var element = (UIElement)dependencyObject;

			// The HitTestVisibilityProperty is never set directly. This means that baseValue is always the result of the parent's CoerceHitTestVisibility.
			var baseHitTestVisibility = (HitTestVisibility)baseValue;

			// If the parent is collapsed, we should be collapsed as well. This takes priority over everything else, even if we would be visible otherwise.
			if (baseHitTestVisibility == HitTestVisibility.Collapsed)
			{
				return HitTestVisibility.Collapsed;
			}

			// If we're not locally hit-test visible, visible, or enabled, we should be collapsed. Our children will be collapsed as well.
			if (!element.IsLoaded || !element.IsHitTestVisible || element.Visibility != Visibility.Visible || !element.IsEnabledOverride())
			{
				return HitTestVisibility.Collapsed;
			}

			// If we're not hit (usually means we don't have a Background/Fill), we're invisible. Our children will be visible or not, depending on their state.
			if (!element.IsViewHit())
			{
				return HitTestVisibility.Invisible;
			}

			// If we're not collapsed or invisible, we can be targeted by hit-testing. This means that we can be the source of pointer events.
			return HitTestVisibility.Visible;
		}

		private static void OnHitTestVisibilityChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
		}
#endregion

	}
}
