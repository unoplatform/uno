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
using Uno.UI.Xaml;

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
				RaiseUsingCaptures(Wheel, originalSource, routedArgs);
			}

			private void CoreWindow_PointerEntered(CoreWindow sender, PointerEventArgs args)
			{
				var (originalSource, _) = VisualTreeHelper.HitTest(args.CurrentPoint.Position);

				// Even if impossible for the Enter, we are fallbacking on the RootElement for safety
				// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
				// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
				originalSource ??= Windows.UI.Xaml.Window.Current.Content;

				if (originalSource is null)
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"CoreWindow_PointerEntered ({args.CurrentPoint.Position}) **undispatched**");
					}

					return;
				}

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerEntered [{originalSource.GetDebugName()}");
				}

				var routedArgs = new PointerRoutedEventArgs(args, originalSource);

				Raise(Enter, originalSource, routedArgs);
			}

			private void CoreWindow_PointerExited(CoreWindow sender, PointerEventArgs args)
			{
				// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
				var originalSource = Windows.UI.Xaml.Window.Current.Content;
				var overBranchLeaf = VisualTreeHelper.SearchDownForLeaf(originalSource, _isOver);

				if (overBranchLeaf is null)
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"CoreWindow_PointerExited ({args.CurrentPoint.Position}) **undispatched**");
					}

					return;
				}

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerPressed [{overBranchLeaf.GetDebugName()}");
				}

				var routedArgs = new PointerRoutedEventArgs(args, originalSource);

				Raise(Leave, overBranchLeaf, routedArgs);
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
				Raise(Pressed, originalSource, routedArgs);
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

				RaiseUsingCaptures(Released, originalSource, routedArgs);
				ClearPressedState(routedArgs);
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
					Raise(Leave, staleBranch.Value, routedArgs);
				}

				// Second (try to) raise the PointerEnter on the OriginalSource
				// Note: This won't do anything if already over.
				routedArgs.Handled = false;
				Raise(Enter, originalSource, routedArgs);

				// Finally raise the event, either on the OriginalSource or on the capture owners if any
				routedArgs.Handled = false;
				RaiseUsingCaptures(Move, originalSource, routedArgs);
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

				RaiseUsingCaptures(Cancelled, originalSource, routedArgs);
				ClearPressedState(routedArgs);
			}

			private void ClearPressedState(PointerRoutedEventArgs routedArgs)
			{
				if (_pressedElements.TryGetValue(routedArgs.Pointer, out var pressedLeaf))
				{
					// We must make sure to clear the pressed state on all elements that was flagged as pressed.
					// This is required as the current originalSource might not be the same as when we pressed (pointer moved),
					// ** OR ** the pointer has been captured by a parent element so we didn't raised to released on the sub elements.

					_pressedElements.Remove(routedArgs.Pointer);

					// Note: The event is propagated silently (public events won't be raised) as it's only to clear internal state
					var ctx = new BubblingContext {IsInternal = true};
					pressedLeaf.OnPointerUp(routedArgs, ctx);
				}
			}

#region Helpers
			private delegate void RaisePointerEventArgs(UIElement element, PointerRoutedEventArgs args, BubblingContext ctx);

			private static readonly RaisePointerEventArgs Wheel = (elt, args, ctx) => elt.OnPointerWheel(args, ctx);
			private static readonly RaisePointerEventArgs Enter = (elt, args, ctx) => elt.OnPointerEnter(args, ctx);
			private static readonly RaisePointerEventArgs Leave = (elt, args, ctx) =>
			{
				elt.OnPointerExited(args, ctx);

				// Even if it's not true, when pointer is leaving an element, we propagate a SILENT (a.k.a. internal) up event to clear the pressed state.
				// Note: This is usually limited only to a given branch (cf. Move)
				// Note: This differs of how we behave on iOS, macOS and Android which does have "implicit capture" while pressed.
				//		 It should only impact the "Pressed" visual states of controls.
				ctx.IsInternal = true;
				args.Handled = false;
				elt.OnPointerUp(args, ctx);
			};
			private static readonly RaisePointerEventArgs Pressed = (elt, args, ctx) => elt.OnPointerDown(args, ctx);
			private static readonly RaisePointerEventArgs Released = (elt, args, ctx) => elt.OnPointerUp(args, ctx);
			private static readonly RaisePointerEventArgs Move = (elt, args, ctx) => elt.OnPointerMove(args, ctx);
			private static readonly RaisePointerEventArgs Cancelled = (elt, args, ctx) => elt.OnPointerCancel(args, ctx);

			private static void Raise(RaisePointerEventArgs raise, UIElement originalSource, PointerRoutedEventArgs routedArgs)
				=> raise(originalSource, routedArgs, BubblingContext.Bubble);

			private static void Raise(RaisePointerEventArgs raise, VisualTreeHelper.Branch branch, PointerRoutedEventArgs routedArgs)
				=> raise(branch.Leaf, routedArgs, BubblingContext.BubbleUpTo(branch.Root));

			private static void RaiseUsingCaptures(RaisePointerEventArgs raise, UIElement originalSource, PointerRoutedEventArgs routedArgs)
			{
				if (PointerCapture.TryGet(routedArgs.Pointer, out var capture))
				{
					var targets = capture.Targets.ToList();
					if (capture.IsImplicitOnly)
					{
						raise(originalSource, routedArgs, BubblingContext.Bubble);

						foreach (var target in targets)
						{
							routedArgs.Handled = false;
							raise(target.Element, routedArgs, BubblingContext.NoBubbling);
						}
					}
					else
					{
						var explicitTarget = targets.Find(c => c.Kind == PointerCaptureKind.Explicit)!;

						raise(explicitTarget.Element, routedArgs, BubblingContext.Bubble);

						foreach (var target in targets)
						{
							if (target == explicitTarget)
							{
								continue;
							}

							routedArgs.Handled = false;
							raise(target.Element, routedArgs, BubblingContext.NoBubbling);
						}
					}
				}
				else
				{
					raise(originalSource, routedArgs, BubblingContext.Bubble);
				}
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

		/// <summary>
		/// Represents the final calculated hit-test visibility of the element.
		/// </summary>
		/// <remarks>
		/// This property should never be directly set, and its value should always be calculated through coercion (see <see cref="CoerceHitTestVisibility(DependencyObject, object, bool)"/>.
		/// </remarks>
		[GeneratedDependencyProperty(DefaultValue = HitTestability.Collapsed, CoerceCallback = true, Options = FrameworkPropertyMetadataOptions.Inherits)]
		internal static DependencyProperty HitTestVisibilityProperty { get; } = CreateHitTestVisibilityProperty();

		internal HitTestability HitTestVisibility
		{
			get => GetHitTestVisibilityValue();
			set => SetHitTestVisibilityValue(value);
		}

		/// <summary>
		/// This calculates the final hit-test visibility of an element.
		/// </summary>
		/// <returns></returns>
		private object CoerceHitTestVisibility(object baseValue)
		{
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
				!IsLoaded ||
#endif
				!IsHitTestVisible || Visibility != Visibility.Visible || !IsEnabledOverride())
			{
				return HitTestability.Collapsed;
			}

			// If we're not hit (usually means we don't have a Background/Fill), we're invisible. Our children will be visible or not, depending on their state.
			if (!IsViewHit())
			{
				return HitTestability.Invisible;
			}

			// If we're not collapsed or invisible, we can be targeted by hit-testing. This means that we can be the source of pointer events.
			return HitTestability.Visible;
		}

		internal void SetHitTestVisibilityForRoot()
		{
			// Root element must be visible to hit testing, regardless of the other properties values.
			// The default value of HitTestVisibility is collapsed to avoid spending time coercing to a
			// Collapsed.
			HitTestVisibility = HitTestability.Visible;
		}

		internal void ClearHitTestVisibilityForRoot()
		{
			this.ClearValue(HitTestVisibilityProperty);
		}

#endregion

		partial void CapturePointerNative(Pointer pointer)
			=> CoreWindow.GetForCurrentThread()!.SetPointerCapture();

		partial void ReleasePointerNative(Pointer pointer)
			=> CoreWindow.GetForCurrentThread()!.ReleasePointerCapture();
	}
}
#endif
