#if UNO_HAS_MANAGED_POINTERS
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Input;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using static Microsoft.UI.Xaml.UIElement;

namespace Uno.UI.Xaml.Core;

internal partial class InputManager
{
	internal PointerManager Pointers { get; private set; } = default!;

	partial void ConstructPointerManager()
	{
		Pointers = new PointerManager(this);

		// Injector supports only pointers for now, so configure only in by managed pointer
		// (should be moved to the InputManager ctor once the injector supports other input types)
		InputInjector.SetTargetForCurrentThread(this);
	}

	partial void InitializeManagedPointers(object host)
		=> Pointers.Init(host);

	partial void InjectPointerAdded(PointerEventArgs args)
		=> Pointers.InjectPointerAdded(args);

	partial void InjectPointerUpdated(PointerEventArgs args)
		=> Pointers.InjectPointerUpdated(args);

	partial void InjectPointerRemoved(PointerEventArgs args)
		=> Pointers.InjectPointerRemoved(args);

	internal class PointerManager
	{
		private static readonly Logger _log = LogExtensionPoint.Log(typeof(PointerManager));
		private static readonly bool _trace = _log.IsEnabled(LogLevel.Trace);

		// TODO: Use pointer ID for the predicates
		private static readonly StalePredicate _isOver = new(e => e.IsPointerOver, "IsPointerOver");

		private readonly Dictionary<Pointer, UIElement> _pressedElements = new();

		private readonly InputManager _inputManager;
		private IUnoCorePointerInputSource? _source;

		public PointerManager(InputManager inputManager)
		{
			_inputManager = inputManager;
		}

		/// <summary>
		/// Initialize the InputManager.
		/// This has to be invoked only once the host of the owning ContentRoot has been set.
		/// </summary>
		public void Init(object host)
		{
			if (!ApiExtensibility.CreateInstance(host, out _source))
			{
				throw new InvalidOperationException("Failed to initialize the PointerManager: cannot resolve the IUnoCorePointerInputSource.");
			}

			if (_inputManager.ContentRoot.Type == ContentRootType.CoreWindow)
			{
				CoreWindow.IShouldntUseGetForCurrentThread()?.SetPointerInputSource(_source);
			}

			_source.PointerMoved += (c, e) => OnPointerMoved(e);
			_source.PointerEntered += (c, e) => OnPointerEntered(e);
			_source.PointerExited += (c, e) => OnPointerExited(e);
			_source.PointerPressed += (c, e) => OnPointerPressed(e);
			_source.PointerReleased += (c, e) => OnPointerReleased(e);
			_source.PointerWheelChanged += (c, e) => OnPointerWheelChanged(e);
			_source.PointerCancelled += (c, e) => OnPointerCancelled(e);
		}

		private void UpdateLastInputType(PointerEventArgs e)
		{
			_inputManager.LastInputDeviceType = e.CurrentPoint?.PointerDeviceType switch
			{
				PointerDeviceType.Touch => InputDeviceType.Touch,
				PointerDeviceType.Pen => InputDeviceType.Pen,
				PointerDeviceType.Mouse => InputDeviceType.Mouse,
				_ => _inputManager.LastInputDeviceType
			};
		}

		private void OnPointerWheelChanged(Windows.UI.Core.PointerEventArgs args)
		{
			var (originalSource, _) = HitTest(args);

			// Even if impossible for the Release, we are fallbacking on the RootElement for safety
			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;

			if (originalSource is null)
			{
				if (_trace)
				{
					Trace($"PointerWheel ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (_trace)
			{
				Trace($"PointerWheelChanged [{originalSource.GetDebugName()}]");
			}

			UpdateLastInputType(args);

			var routedArgs = new PointerRoutedEventArgs(args, originalSource);

			// First raise the event, either on the OriginalSource or on the capture owners if any
			RaiseUsingCaptures(Wheel, originalSource, routedArgs);

			// Scrolling can change the element underneath the pointer, so we need to update
			(originalSource, var staleBranch) = HitTest(args, caller: "OnPointerWheelChanged_post_wheel", isStale: _isOver);
			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;

			// Second raise the PointerExited events on the stale branch
			if (staleBranch.HasValue)
			{
				if (Raise(Leave, staleBranch.Value, routedArgs) is { VisualTreeAltered: true })
				{
					// The visual tree has been modified in a way that requires performing a new hit test.
					originalSource = HitTest(args, caller: "OnPointerWheelChanged_post_leave").element ?? _inputManager.ContentRoot.VisualTree.RootElement;
				}
			}

			// Third (try to) raise the PointerEnter on the OriginalSource
			// Note: This won't do anything if already over.
			Raise(Enter, originalSource!, routedArgs);
		}

		private void OnPointerEntered(Windows.UI.Core.PointerEventArgs args)
		{
			var (originalSource, _) = HitTest(args);

			if (originalSource is ImplicitTextBlock)
			{
				global::System.Diagnostics.Debug.WriteLine("Entered");
			}
			// Even if impossible for the Enter, we are fallbacking on the RootElement for safety
			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;

			if (originalSource is null)
			{
				if (_trace)
				{
					Trace($"PointerEntered ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (_trace)
			{
				Trace($"PointerEntered [{originalSource.GetDebugName()}]");
			}

			UpdateLastInputType(args);

			var routedArgs = new PointerRoutedEventArgs(args, originalSource);

			Raise(Enter, originalSource, routedArgs);
		}

		private void OnPointerExited(Windows.UI.Core.PointerEventArgs args)
		{
			// This is how UWP behaves: when out of the bounds of the Window, the root element is used.
			var originalSource = _inputManager.ContentRoot.VisualTree.RootElement;
			if (originalSource is null)
			{
				if (_trace)
				{
					Trace($"PointerExited ({args.CurrentPoint.Position}) Called before window content set.");
				}

				return;
			}

			var overBranchLeaf = VisualTreeHelper.SearchDownForLeaf(originalSource, _isOver);
			if (overBranchLeaf is null)
			{
				if (_trace)
				{
					Trace($"PointerExited ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (_trace)
			{
				Trace($"PointerExited [{overBranchLeaf.GetDebugName()}]");
			}

			UpdateLastInputType(args);

			var routedArgs = new PointerRoutedEventArgs(args, originalSource);

			Raise(Leave, overBranchLeaf, routedArgs);
			if (!args.CurrentPoint.IsInContact && (PointerDeviceType)args.CurrentPoint.Pointer.Type == PointerDeviceType.Touch)
			{
				// We release the captures on exit when pointer if not pressed
				// Note: for a "Tap" with a finger the sequence is Up / Exited / Lost, so the lost cannot be raised on Up
				ReleaseCaptures(routedArgs);
			}
		}

		private void OnPointerPressed(Windows.UI.Core.PointerEventArgs args)
		{
			var (originalSource, _) = HitTest(args);

			// Even if impossible for the Pressed, we are fallbacking on the RootElement for safety
			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;

			if (originalSource is null)
			{
				if (_trace)
				{
					Trace($"PointerPressed ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (_trace)
			{
				Trace($"PointerPressed [{originalSource.GetDebugName()}]");
			}

			UpdateLastInputType(args);

			var routedArgs = new PointerRoutedEventArgs(args, originalSource);

			_pressedElements[routedArgs.Pointer] = originalSource;
			Raise(Pressed, originalSource, routedArgs);
		}

		private void OnPointerReleased(Windows.UI.Core.PointerEventArgs args)
		{
			var (originalSource, _) = HitTest(args);

			var isOutOfWindow = originalSource is null;

			// Even if impossible for the Release, we are fallbacking on the RootElement for safety
			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;

			if (originalSource is null)
			{
				if (_trace)
				{
					Trace($"PointerReleased ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (_trace)
			{
				Trace($"PointerReleased [{originalSource.GetDebugName()}]");
			}

			UpdateLastInputType(args);

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

		private void OnPointerMoved(Windows.UI.Core.PointerEventArgs args)
		{
			var (originalSource, staleBranch) = HitTest(args, _isOver);

			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;

			if (originalSource is null)
			{
				if (_trace)
				{
					Trace($"PointerMoved ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (_trace)
			{
				Trace($"PointerMoved [{originalSource.GetDebugName()}]");
			}

			UpdateLastInputType(args);

			var routedArgs = new PointerRoutedEventArgs(args, originalSource);

			// First raise the PointerExited events on the stale branch
			if (staleBranch.HasValue)
			{
				if (Raise(Leave, staleBranch.Value, routedArgs) is { VisualTreeAltered: true })
				{
					// The visual tree has been modified in a way that requires performing a new hit test.
					originalSource = HitTest(args, caller: "OnPointerMoved_post_leave").element ?? _inputManager.ContentRoot.VisualTree.RootElement;
				}
			}

			// Second (try to) raise the PointerEnter on the OriginalSource
			// Note: This won't do anything if already over.
			if (Raise(Enter, originalSource, routedArgs) is { VisualTreeAltered: true })
			{
				// The visual tree has been modified in a way that requires performing a new hit test.
				originalSource = HitTest(args, caller: "OnPointerMoved_post_enter").element ?? _inputManager.ContentRoot.VisualTree.RootElement;
			}

			// Finally raise the event, either on the OriginalSource or on the capture owners if any
			RaiseUsingCaptures(Move, originalSource, routedArgs);
		}

		private void OnPointerCancelled(Windows.UI.Core.PointerEventArgs args)
		{
			var (originalSource, _) = HitTest(args);

			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that is another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;

			if (originalSource is null)
			{
				if (_trace)
				{
					Trace($"PointerCancelled ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (_trace)
			{
				Trace($"PointerCancelled [{originalSource.GetDebugName()}]");
			}

			UpdateLastInputType(args);

			var routedArgs = new PointerRoutedEventArgs(args, originalSource);

			RaiseUsingCaptures(Cancelled, originalSource, routedArgs);
			// Note: No ReleaseCaptures(routedArgs);, the cancel automatically raise it
			ClearPressedState(routedArgs);
		}

		#region Captures
		internal void SetPointerCapture(PointerIdentifier uniqueId)
		{
			_source?.SetPointerCapture(uniqueId);
		}

		internal void ReleasePointerCapture(PointerIdentifier uniqueId)
		{
			_source?.ReleasePointerCapture(uniqueId);
		}

		private void ReleaseCaptures(PointerRoutedEventArgs routedArgs)
		{
			if (PointerCapture.TryGet(routedArgs.Pointer, out var capture))
			{
				foreach (var target in capture.Targets.ToList())
				{
					target.Element.ReleasePointerCapture(capture.Pointer.UniqueId, kinds: PointerCaptureKind.Any);
				}
			}
		}
		#endregion

		#region Pointer injection
		internal void InjectPointerAdded(PointerEventArgs args)
			=> OnPointerEntered(args);

		internal void InjectPointerRemoved(PointerEventArgs args)
			=> OnPointerExited(args);

		internal void InjectPointerUpdated(PointerEventArgs args)
		{
			var kind = args.CurrentPoint.Properties.PointerUpdateKind;

			if (args.CurrentPoint.Properties.IsCanceled)
			{
				OnPointerCancelled(args);
			}
			else if (args.CurrentPoint.Properties.MouseWheelDelta is not 0)
			{
				OnPointerWheelChanged(args);
			}
			else if (kind is PointerUpdateKind.Other)
			{
				OnPointerMoved(args);
			}
			else if (((int)kind & 1) == 1)
			{
				OnPointerPressed(args);
			}
			else
			{
				OnPointerReleased(args);
			}
		}
		#endregion

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
		private (UIElement? element, VisualTreeHelper.Branch? stale) HitTest(PointerEventArgs args, StalePredicate? isStale = null, [CallerMemberName] string caller = "")
		{
			if (_inputManager.ContentRoot.XamlRoot is null)
			{
				throw new InvalidOperationException("The XamlRoot must be properly initialized for hit testing.");
			}

			return VisualTreeHelper.HitTest(args.CurrentPoint.Position, _inputManager.ContentRoot.XamlRoot, isStale: isStale);
		}

		private delegate void RaisePointerEventArgs(UIElement element, PointerRoutedEventArgs args, BubblingContext ctx);
		private readonly record struct PointerEvent(RaisePointerEventArgs Invoke, [CallerMemberName] string Name = "");

		private static readonly PointerEvent Wheel = new((elt, args, ctx) => elt.OnPointerWheel(args, ctx));
		private static readonly PointerEvent Enter = new((elt, args, ctx) => elt.OnPointerEnter(args, ctx));
		private static readonly PointerEvent Leave = new((elt, args, ctx) => elt.OnPointerExited(args, ctx));
		private static readonly PointerEvent Pressed = new((elt, args, ctx) => elt.OnPointerDown(args, ctx));
		private static readonly PointerEvent Released = new((elt, args, ctx) => elt.OnPointerUp(args, ctx));
		private static readonly PointerEvent Move = new((elt, args, ctx) => elt.OnPointerMove(args, ctx));
		private static readonly PointerEvent Cancelled = new((elt, args, ctx) => elt.OnPointerCancel(args, ctx));

		private PointerEventDispatchResult Raise(PointerEvent evt, UIElement originalSource, PointerRoutedEventArgs routedArgs)
		{
			if (_trace)
			{
				Trace($"[Ignoring captures] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to original source [{originalSource.GetDebugName()}]");
			}

			routedArgs.Handled = false;
			UIElement.BeginPointerEventDispatch();

			evt.Invoke(originalSource, routedArgs, BubblingContext.Bubble);

			return EndPointerEventDispatch();
		}

		private PointerEventDispatchResult Raise(PointerEvent evt, VisualTreeHelper.Branch branch, PointerRoutedEventArgs routedArgs)
		{
			if (_trace)
			{
				Trace($"[Ignoring captures] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to branch [{branch}]");
			}

			routedArgs.Handled = false;
			UIElement.BeginPointerEventDispatch();

			evt.Invoke(branch.Leaf, routedArgs, BubblingContext.BubbleUpTo(branch.Root));

			return UIElement.EndPointerEventDispatch();
		}

		private PointerEventDispatchResult RaiseUsingCaptures(PointerEvent evt, UIElement originalSource, PointerRoutedEventArgs routedArgs)
		{
			routedArgs.Handled = false;
			UIElement.BeginPointerEventDispatch();

			if (PointerCapture.TryGet(routedArgs.Pointer, out var capture))
			{
				var targets = capture.Targets.ToList();
				if (capture.IsImplicitOnly)
				{
					if (_trace)
					{
						Trace($"[Implicit capture] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to original source first [{originalSource.GetDebugName()}]");
					}

					evt.Invoke(originalSource, routedArgs, BubblingContext.Bubble);

					foreach (var target in targets)
					{
						if (_trace)
						{
							Trace($"[Implicit capture] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to capture target [{originalSource.GetDebugName()}] (-- no bubbling--)");
						}

						routedArgs.Handled = false;
						evt.Invoke(target.Element, routedArgs, BubblingContext.NoBubbling);
					}
				}
				else
				{
					var explicitTarget = targets.Find(c => c.Kind.HasFlag(PointerCaptureKind.Explicit))!;

					if (_trace)
					{
						Trace($"[Explicit capture] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to capture target [{explicitTarget.Element.GetDebugName()}]");
					}

					evt.Invoke(explicitTarget.Element, routedArgs, BubblingContext.Bubble);

					foreach (var target in targets)
					{
						if (target == explicitTarget)
						{
							continue;
						}

						if (_trace)
						{
							Trace($"[Explicit capture] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to alternative (implicit) target [{explicitTarget.Element.GetDebugName()}] (-- no bubbling--)");
						}

						routedArgs.Handled = false;
						evt.Invoke(target.Element, routedArgs, BubblingContext.NoBubbling);
					}
				}
			}
			else
			{
				if (_trace)
				{
					Trace($"[No capture] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to original source [{originalSource.GetDebugName()}]");
				}

				evt.Invoke(originalSource, routedArgs, BubblingContext.Bubble);
			}

			return UIElement.EndPointerEventDispatch();
		}

		private static void Trace(string text)
		{
			_log.Trace(text);
		}
		#endregion
	}
}
#endif
