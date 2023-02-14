#if UNO_HAS_MANAGED_POINTERS
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Input;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using static Microsoft.UI.Xaml.UIElement;

namespace Uno.UI.Xaml.Core;

internal partial class InputManager
{
	internal void RaisePointerEntered(PointerEventArgs args)
		=> _pointerManager.OnPointerEntered(args);

	internal void RaisePointerExited(PointerEventArgs args)
		=> _pointerManager.OnPointerExited(args);

	internal void RaisePointerMoved(PointerEventArgs args)
		=> _pointerManager.OnPointerMoved(args);

	internal void RaisePointerPressed(PointerEventArgs args)
		=> _pointerManager.OnPointerPressed(args);

	internal void RaisePointerReleased(PointerEventArgs args)
		=> _pointerManager.OnPointerReleased(args);

	internal void RaisePointerWheelChanged(PointerEventArgs args)
		=> _pointerManager.OnPointerWheelChanged(args);

	internal void RaisePointerCancelled(PointerEventArgs args)
		=> _pointerManager.OnPointerCancelled(args);

	internal void SetPointerCapture(PointerIdentifier identifier)
		=> _pointerManager.SetPointerCapture(identifier);

	internal void ReleasePointerCapture(PointerIdentifier identifier)
		=> _pointerManager.ReleasePointerCapture(identifier);

	private PointerManager _pointerManager = null!;

	partial void InitializeManagedPointers()
	{
		_pointerManager = new PointerManager(this);
	}

	private class PointerManager
	{
		private static IPointerExtension? _pointerExtension;

		// TODO: Use pointer ID for the predicates
		private static readonly Predicate<UIElement> _isOver = e => e.IsPointerOver;

		private readonly Dictionary<Pointer, UIElement> _pressedElements = new();

		private readonly InputManager _inputManager;

		public PointerManager(InputManager inputManager)
		{
			if (_pointerExtension is null)
			{
				ApiExtensibility.CreateInstance(typeof(PointerManager), out _pointerExtension); // TODO: Add IPointerExtension implementation to all Skia targets and create instance per XamlRoot https://github.com/unoplatform/uno/issues/8978
			}
			_inputManager = inputManager;

			if (_inputManager._contentRoot.Type == ContentRootType.CoreWindow)
			{
				Microsoft.UI.Xaml.Window.Current.CoreWindow.PointerMoved += (c, e) => OnPointerMoved(e);
				Microsoft.UI.Xaml.Window.Current.CoreWindow.PointerEntered += (c, e) => OnPointerEntered(e);
				Microsoft.UI.Xaml.Window.Current.CoreWindow.PointerExited += (c, e) => OnPointerExited(e);
				Microsoft.UI.Xaml.Window.Current.CoreWindow.PointerPressed += (c, e) => OnPointerPressed(e);
				Microsoft.UI.Xaml.Window.Current.CoreWindow.PointerReleased += (c, e) => OnPointerReleased(e);
				Microsoft.UI.Xaml.Window.Current.CoreWindow.PointerWheelChanged += (c, e) => OnPointerWheelChanged(e);
				Microsoft.UI.Xaml.Window.Current.CoreWindow.PointerCancelled += (c, e) => OnPointerCancelled(e);
			}
		}

		internal void OnPointerWheelChanged(Windows.UI.Core.PointerEventArgs args)
		{
			var (originalSource, _) = HitTest(args);

			// Even if impossible for the Release, we are fallbacking on the RootElement for safety
			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= Microsoft.UI.Xaml.Window.Current.Content;

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

		private (UIElement?, VisualTreeHelper.Branch?) HitTest(PointerEventArgs args, Predicate<UIElement>? isStale = null)
		{
			if (_inputManager._contentRoot?.XamlRoot is null)
			{
				throw new InvalidOperationException("The XamlRoot must be properly initialized for hit testng.");
			}

			return VisualTreeHelper.HitTest(args.CurrentPoint.Position, _inputManager._contentRoot.XamlRoot, isStale: isStale);
		}

		internal void OnPointerEntered(Windows.UI.Core.PointerEventArgs args)
		{
			var (originalSource, _) = HitTest(args);

			if (originalSource is ImplicitTextBlock)
			{
				global::System.Diagnostics.Debug.WriteLine("Entered");
			}
			// Even if impossible for the Enter, we are fallbacking on the RootElement for safety
			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= Microsoft.UI.Xaml.Window.Current.Content;

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

		internal void OnPointerExited(Windows.UI.Core.PointerEventArgs args)
		{
			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			var originalSource = Microsoft.UI.Xaml.Window.Current.Content;
			if (originalSource == null)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerExited ({args.CurrentPoint.Position}) Called before window content set.");
				}

				return;
			}

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
			if (!args.CurrentPoint.IsInContact && (PointerDeviceType)args.CurrentPoint.Pointer.Type == PointerDeviceType.Touch)
			{
				// We release the captures on exit when pointer if not pressed
				// Note: for a "Tap" with a finger the sequence is Up / Exited / Lost, so the lost cannot be raised on Up
				ReleaseCaptures(routedArgs);
			}
		}

		internal void OnPointerPressed(Windows.UI.Core.PointerEventArgs args)
		{
			var (originalSource, _) = HitTest(args);

			// Even if impossible for the Pressed, we are fallbacking on the RootElement for safety
			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= Microsoft.UI.Xaml.Window.Current.Content;

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

		internal void OnPointerReleased(Windows.UI.Core.PointerEventArgs args)
		{
			var (originalSource, _) = HitTest(args);

			var isOutOfWindow = originalSource is null;

			// Even if impossible for the Release, we are fallbacking on the RootElement for safety
			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= Microsoft.UI.Xaml.Window.Current.Content;

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
			if (isOutOfWindow || (PointerDeviceType)args.CurrentPoint.Pointer.Type != PointerDeviceType.Touch)
			{
				// We release the captures on up but only after the released event and processed the gesture
				// Note: For a "Tap" with a finger the sequence is Up / Exited / Lost, so we let the Exit raise the capture lost
				ReleaseCaptures(routedArgs);
			}
			ClearPressedState(routedArgs);
		}

		internal void OnPointerMoved(Windows.UI.Core.PointerEventArgs args)
		{
			var (originalSource, staleBranch) = HitTest(args, _isOver);

			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= Microsoft.UI.Xaml.Window.Current.Content;

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

		internal void OnPointerCancelled(Windows.UI.Core.PointerEventArgs args)
		{
			var (originalSource, _) = HitTest(args);

			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that is another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= Microsoft.UI.Xaml.Window.Current.Content;

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
			// Note: No ReleaseCaptures(routedArgs);, the cancel automatically raise it
			ClearPressedState(routedArgs);
		}

		internal void SetPointerCapture(PointerIdentifier uniqueId)
		{
			if (_pointerExtension is not null)
			{
				_pointerExtension?.SetPointerCapture(uniqueId, _inputManager._contentRoot.XamlRoot);
			}
			else
			{
				CoreWindow.GetForCurrentThread()!.SetPointerCapture(uniqueId);
			}
		}

		internal void ReleasePointerCapture(PointerIdentifier uniqueId)
		{
			if (_pointerExtension is not null)
			{
				_pointerExtension?.ReleasePointerCapture(uniqueId, _inputManager._contentRoot.XamlRoot);
			}
			else
			{
				CoreWindow.GetForCurrentThread()!.ReleasePointerCapture(uniqueId);
			}
		}

		private void ReleaseCaptures(PointerRoutedEventArgs routedArgs)
		{
			if (PointerCapture.TryGet(routedArgs.Pointer, out var capture))
			{
				foreach (var target in capture.Targets)
				{
					target.Element.ReleasePointerCapture(capture.Pointer.UniqueId, kinds: PointerCaptureKind.Any);
				}
			}
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
				var ctx = new BubblingContext { IsInternal = true };
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
					var explicitTarget = targets.Find(c => c.Kind.HasFlag(PointerCaptureKind.Explicit))!;

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

}
#endif
