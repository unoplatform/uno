#if UNO_HAS_MANAGED_POINTERS
#nullable enable

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
using Windows.UI.Xaml.Media;
using Uno.UI;

namespace Windows.UI.Xaml
{
	partial class UIElement
	{
		private class PointerManager
		{
			// TODO: Use pointer ID for the predicates
			private static readonly Predicate<UIElement> _isOver = e => e.IsPointerOver;

			private readonly Dictionary<Pointer, UIElement> _pressedElements = new Dictionary<Pointer, UIElement>();

			public PointerManager()
			{
				Windows.UI.Xaml.Window.Current.CoreWindow.PointerMoved += CoreWindow_PointerMoved;
				Windows.UI.Xaml.Window.Current.CoreWindow.PointerEntered += CoreWindow_PointerEntered;
				Windows.UI.Xaml.Window.Current.CoreWindow.PointerExited += CoreWindow_PointerExited;
				Windows.UI.Xaml.Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
				Windows.UI.Xaml.Window.Current.CoreWindow.PointerReleased += CoreWindow_PointerReleased;
				Windows.UI.Xaml.Window.Current.CoreWindow.PointerWheelChanged += CoreWindow_PointerWheelChanged;
				Windows.UI.Xaml.Window.Current.CoreWindow.PointerCancelled += CoreWindow_PointerCancelled;
			}

			private void CoreWindow_PointerWheelChanged(CoreWindow sender, PointerEventArgs args)
			{
				var (originalSource, _) = VisualTreeHelper.HitTest(args.CurrentPoint.Position);

				// Even if impossible for the Release, we are fallbacking on the RootElement for safety
				// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
				// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
				originalSource ??= Windows.UI.Xaml.Window.Current.Content;

				if (originalSource is null)
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"CoreWindow_PointerPressed ({args.CurrentPoint.Position}) **undispatched**");
					}

					return;
				}

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerPressed [{originalSource.GetDebugName()}");
				}

				var routedArgs = new PointerRoutedEventArgs(args, originalSource);

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
					originalSource.OnNativePointerWheel(routedArgs);
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

			private void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs args)
			{
				var (originalSource, _) = VisualTreeHelper.HitTest(args.CurrentPoint.Position);

				// Even if impossible for the Pressed, we are fallbacking on the RootElement for safety
				// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
				// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
				originalSource ??= Windows.UI.Xaml.Window.Current.Content;

				if (originalSource is null)
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"CoreWindow_PointerPressed ({args.CurrentPoint.Position}) **undispatched**");
					}

					return;
				}

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerPressed [{originalSource.GetDebugName()}");
				}

				var routedArgs = new PointerRoutedEventArgs(args, originalSource);

				_pressedElements[routedArgs.Pointer] = originalSource;
				originalSource.OnNativePointerDown(routedArgs);
			}

			private void CoreWindow_PointerReleased(CoreWindow sender, PointerEventArgs args)
			{
				var (originalSource, _) = VisualTreeHelper.HitTest(args.CurrentPoint.Position);

				// Even if impossible for the Release, we are fallbacking on the RootElement for safety
				// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
				// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
				originalSource ??= Windows.UI.Xaml.Window.Current.Content;

				if (originalSource is null)
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"CoreWindow_PointerPressed ({args.CurrentPoint.Position}) **undispatched**");
					}

					return;
				}

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerPressed [{originalSource.GetDebugName()}");
				}

				var routedArgs = new PointerRoutedEventArgs(args, originalSource);

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
					originalSource.OnNativePointerUp(routedArgs);
				}

				if (_pressedElements.TryGetValue(routedArgs.Pointer, out var pressedLeaf))
				{
					// We must make sure to clear the pressed state on all elements that was flagged as pressed.
					// This is required as the current originalSource might not be the same as when we pressed (pointer moved),
					// ** OR ** the pointer has been captured by a parent element so we didn't raised to released on the sub elements.

					_pressedElements.Remove(routedArgs.Pointer);
					ClearPointerState(routedArgs, root: null, pressedLeaf, clearOver: false);
				}
			}


			private void CoreWindow_PointerMoved(CoreWindow sender, PointerEventArgs args)
			{
				var (originalSource, staleBranch) = VisualTreeHelper.HitTest(args.CurrentPoint.Position, isStale: _isOver);

				// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
				// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
				originalSource ??= Windows.UI.Xaml.Window.Current.Content;

				if (originalSource is null)
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"CoreWindow_PointerMoved ({args.CurrentPoint.Position}) **undispatched**");
					}

					return;
				}

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerMoved [{originalSource.GetDebugName()}");
				}

				var routedArgs = new PointerRoutedEventArgs(args, originalSource);

				// First raise the PointerExited events on the stale branch
				if (staleBranch.HasValue)
				{
					var (root, leaf) = staleBranch.Value;

					ClearPointerState(routedArgs, root, leaf);
				}

				// Second (try to) raise the PointerEnter on the OriginalSource
				// Note: This won't do anything if already over.
				routedArgs.Handled = false;
				originalSource.OnNativePointerEnter(routedArgs);

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
					originalSource.OnNativePointerMoveWithOverCheck(routedArgs, isOver: true);
				}
			}

			private void CoreWindow_PointerCancelled(CoreWindow sender, PointerEventArgs args)
			{
				var (originalSource, _) = VisualTreeHelper.HitTest(args.CurrentPoint.Position);

				// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
				// Note that is another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
				originalSource ??= Windows.UI.Xaml.Window.Current.Content;

				if (originalSource is null)
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"CoreWindow_PointerCancelled ({args.CurrentPoint.Position}) **undispatched**");
					}

					return;
				}

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerCancelled [{originalSource.GetDebugName()}");
				}

				var routedArgs = new PointerRoutedEventArgs(args, originalSource);

				// Second raise the event, either on the OriginalSource or on the capture owners if any
				if (PointerCapture.TryGet(routedArgs.Pointer, out var capture))
				{
					foreach (var target in capture.Targets.ToArray())
					{
						target.Element.OnNativePointerCancel(routedArgs, isSwallowedBySystem: false);
					}
				}
				else
				{
					originalSource.OnNativePointerCancel(routedArgs, isSwallowedBySystem: false);
				}

				if (_pressedElements.TryGetValue(routedArgs.Pointer, out var pressedLeaf))
				{
					// We must make sure to clear the pressed state on all elements that was flagged as pressed.
					// This is required as the current originalSource might not be the same as when we pressed (pointer moved),
					// ** OR ** the pointer has been captured by a parent element so we didn't raised to released on the sub elements.

					_pressedElements.Remove(routedArgs.Pointer);
					ClearPointerState(routedArgs, root: null, pressedLeaf, clearOver: false);
				}
			}

			// Clears the pointer state (over and pressed) only for a part of the visual tree
			private void ClearPointerState(PointerRoutedEventArgs routedArgs, UIElement? root, UIElement leaf, bool clearOver = true)
			{
				var element = leaf;

				routedArgs.CanBubbleNatively = true; // TODO: UGLY HACK TO AVOID BUBBLING: we should be able to request to bubble only up to a the root
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Exiting branch from (root) {root.GetDebugName()} to (leaf) {element.GetDebugName()}\r\n");
				}

				while (element is { })
				{
					routedArgs.Handled = false;
					if (clearOver)
					{
						element.OnNativePointerExited(routedArgs);
					}
					// TODO: This differs of how we behave on iOS, macOS and Android which does have "implicit capture" while pressed.
					//		 It should only impact the "Pressed" visual states of controls.
					element.SetPressed(routedArgs, isPressed: false, muteEvent: true);

					if (element == root)
					{
						break;
					}

					element = element.GetParent() as UIElement;
				}

				routedArgs.CanBubbleNatively = false;
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

		#region HitTestVisibility
		internal void UpdateHitTest()
		{
			this.CoerceValue(HitTestVisibilityProperty);
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
				typeof(HitTestability),
				typeof(UIElement),
				new FrameworkPropertyMetadata(
					HitTestability.Visible,
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
			var baseHitTestVisibility = (HitTestability)baseValue;

			// If the parent is collapsed, we should be collapsed as well. This takes priority over everything else, even if we would be visible otherwise.
			if (baseHitTestVisibility == HitTestability.Collapsed)
			{
				return HitTestability.Collapsed;
			}

			// If we're not locally hit-test visible, visible, or enabled, we should be collapsed. Our children will be collapsed as well.
			if (
#if !__MACOS__
				!element.IsLoaded ||
#endif
				!element.IsHitTestVisible || element.Visibility != Visibility.Visible || !element.IsEnabledOverride())
			{
				return HitTestability.Collapsed;
			}

			// If we're not hit (usually means we don't have a Background/Fill), we're invisible. Our children will be visible or not, depending on their state.
			if (!element.IsViewHit())
			{
				return HitTestability.Invisible;
			}

			// If we're not collapsed or invisible, we can be targeted by hit-testing. This means that we can be the source of pointer events.
			return HitTestability.Visible;
		}

		private static void OnHitTestVisibilityChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
		}
		#endregion

		partial void CapturePointerNative(Pointer pointer)
			=> CoreWindow.GetForCurrentThread()!.SetPointerCapture();

		partial void ReleasePointerNative(Pointer pointer)
			=> CoreWindow.GetForCurrentThread()!.ReleasePointerCapture();
	}
}
#endif
