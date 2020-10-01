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
				var source = VisualTreeHelper.HitTest(args.CurrentPoint.Position);
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
				var source = VisualTreeHelper.HitTest(args.CurrentPoint.Position);
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
				var source = VisualTreeHelper.HitTest(args.CurrentPoint.Position);
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
				var source = VisualTreeHelper.HitTest(args.CurrentPoint.Position, isStale: _isOver);
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

					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"Exiting branch from (root) {root.GetDebugName()} to (leaf) {stale.GetDebugName()}\r\n");
					}

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
			if (!element.IsLoaded || !element.IsHitTestVisible || element.Visibility != Visibility.Visible || !element.IsEnabledOverride())
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
	}
}
