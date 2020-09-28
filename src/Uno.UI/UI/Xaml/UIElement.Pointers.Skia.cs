#nullable enable
//#define TRACE_HIT_TESTING

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.DataBinding;
using Uno.UI.Extensions;
using Windows.UI.Core;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI;

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

				public readonly UIElement Root;
				public readonly UIElement Leaf;

				public void Deconstruct(out UIElement root, out UIElement leaf)
				{
					root = Root;
					leaf = Leaf;
				}

				public override string ToString() => $"Root={Root.GetDebugName()} | Leaf={Leaf.GetDebugName()}";
			}

			// TODO: Use pointer ID for the predicates
			private static readonly Predicate<UIElement> _isOver = e => e.IsPointerOver;

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
				var source = FindOriginalSource(args);
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
					this.Log().Trace($"CoreWindow_PointerPressed [{source.element.GetDebugName()}");
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
				var source = FindOriginalSource(args);
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
					this.Log().Trace($"CoreWindow_PointerPressed [{source.element.GetDebugName()}");
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
				var source = FindOriginalSource(args);
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
					this.Log().Trace($"CoreWindow_PointerPressed [{source.element.GetDebugName()}");
				}

				var routedArgs = new PointerRoutedEventArgs(args, source.element);

				source.element.OnNativePointerDown(routedArgs);
			}

			private void CoreWindow_PointerMoved(CoreWindow sender, PointerEventArgs args)
			{
				var source = FindOriginalSource(args, isStale: _isOver);
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
					this.Log().Trace($"CoreWindow_PointerMoved [{source.element.GetDebugName()}");
				}

				var routedArgs = new PointerRoutedEventArgs(args, source.element);

				// First raise the PointerExited events on the stale branch
				if (source.stale.HasValue)
				{
					routedArgs.CanBubbleNatively = true; // TODO: UGLY HACK TO AVOID BUBBLING: we should be able to request to bubble only up to a the root
					var (root, stale) = source.stale.Value;

					while (stale is { })
					{
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
				// Note: This won't do anything if already over.
				routedArgs.Handled = false;
				source.element.OnNativePointerEnter(routedArgs);

				// Finally raise the event, either on the OriginalSource or on the capture owners if any
				if (PointerCapture.TryGet(routedArgs.Pointer, out var capture))
				{
					foreach (var target in capture.Targets.ToArray())
					{
						routedArgs.Handled = false;
						target.Element.OnNativePointerMove(routedArgs);
					}
				}
				else
				{
					// Note: We prefer to use the "WithOverCheck" overload as we already know that the pointer is effectively over
					routedArgs.Handled = false;
					source.element.OnNativePointerMoveWithOverCheck(routedArgs, isOver: true);
				}
			}

			private (UIElement? element, Branch? stale) FindOriginalSource(
				PointerEventArgs args,
				Predicate<UIElement>? isStale = null
#if TRACE_HIT_TESTING
				, [CallerMemberName] string? caller = null)
			{
				using var _ = BEGIN_TRACE();
				TRACE($"[{caller!.Replace("CoreWindow_Pointer", "").ToUpperInvariant()}] @{args.CurrentPoint.Position.ToDebugString()} (args: {args.GetHashCode():X8})");
#else
			) {
#endif
				if (Window.Current.RootElement is UIElement root)
				{
					return SearchDownForTopMostElementAt(args.CurrentPoint.Position, root, isStale);
				}

				this.Log().Warn("The root element not set yet, impossible to find the original source.");

				return default;
			}

			private static (UIElement? element, Branch? stale) SearchDownForTopMostElementAt(
				Point posRelToParent,
				UIElement element,
				Predicate<UIElement>? isStale = null,
				Func<IEnumerable<UIElement>, IEnumerable<UIElement>>? childrenFilter = null)
			{
				var stale = default(Branch?);
				var elementHitTestVisibility = (HitTestVisibility)element.GetValue(HitTestVisibilityProperty);

#if TRACE_HIT_TESTING
				using var _ = SET_TRACE_SUBJECT(element);
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

				if (element is ScrollViewer sv)
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
				var children = childrenFilter is null ? element.GetChildren() : childrenFilter(element.GetChildren());
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
