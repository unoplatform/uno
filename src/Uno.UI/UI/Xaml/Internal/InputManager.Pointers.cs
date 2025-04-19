#nullable enable
using System;
using System.Linq;
using Windows.Devices.Input;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using PointerIdentifierPool = Windows.Devices.Input.PointerIdentifierPool; // internal type (should be in Uno namespace)
using static Microsoft.UI.Xaml.UIElement;

#if HAS_UNO_WINUI
using PointerUpdateKind = Microsoft.UI.Input.PointerUpdateKind;
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

namespace Uno.UI.Xaml.Core;

partial class InputManager
{
	internal PointerManager Pointers { get; private set; } = default!;

	partial void ConstructPointerManager()
	{
		Pointers = new PointerManager(this);

		ConstructPointerManager_Managed();
	}

	private void InitializePointers(object host)
	{
		InitializePointers_Managed(host);
	}

	partial void ConstructPointerManager_Managed();
	partial void InitializePointers_Managed(object host);

	#region IInputInjectorTarget
	void IInputInjectorTarget.InjectPointerAdded(PointerEventArgs args) => InjectPointerAdded(args);
	partial void InjectPointerAdded(PointerEventArgs args);

	void IInputInjectorTarget.InjectPointerUpdated(PointerEventArgs args) => InjectPointerUpdated(args);
	partial void InjectPointerUpdated(PointerEventArgs args);

	void IInputInjectorTarget.InjectPointerRemoved(PointerEventArgs args) => InjectPointerRemoved(args);
	partial void InjectPointerRemoved(PointerEventArgs args);
	#endregion

	internal partial class PointerManager
	{
		private static readonly Logger _log = LogExtensionPoint.Log(typeof(PointerManager));
		private static readonly bool _trace = _log.IsEnabled(LogLevel.Trace);

		private readonly InputManager _inputManager;

		public PointerManager(InputManager inputManager)
		{
			_inputManager = inputManager;

			var rootElement = inputManager.ContentRoot.VisualTree.RootElement;
			rootElement.AddHandler(
				UIElement.PointerPressedEvent,
				new PointerEventHandler((snd, args) => ProcessPointerDown(args)),
				handledEventsToo: true);
			rootElement.AddHandler(
				UIElement.PointerReleasedEvent,
				new PointerEventHandler((snd, args) => ProcessPointerUp(args, false)),
				handledEventsToo: true);
		}

		#region Re-routing (Flyout.OverlayInputPassThroughElement)
		private ReRouted? _reRouted; // Note: On non managed pointers, only the pointer down can be re-routed.
		private readonly record struct ReRouted(PointerRoutedEventArgs Args, UIElement From, UIElement To);

		/// <summary>
		/// Re-route the given event args (cf. <see cref="FlyoutBase.OverlayInputPassThroughElement"/>).
		/// </summary>
		public void ReRoute(PointerRoutedEventArgs routedArgs, UIElement from, UIElement to)
		{
#if UNO_HAS_MANAGED_POINTERS
			if (Current != routedArgs)
			{
				throw new InvalidOperationException("Cannot reroute a pointer event args that is not currently being dispatched.");
			}
#endif

			if (_reRouted is not null)
			{
				throw new InvalidOperationException("Pointer event args can be re-routed only once per bubbling.");
			}

			_reRouted = new ReRouted(routedArgs, from, to);

#if HAS_NATIVE_IMPLICIT_POINTER_CAPTURE
			UIElement.ReRoutePointerSequenceTo(to);
#endif
		}
		#endregion

		#region Uno specific root element pointer handling
		// Code below is listening to some pointer events at the root level of the app
		// to be able to complete the support of pointers events sequences.
		// This class is expected to be initialized on all platforms and on all visual tree.

		[ThreadStatic]
		private static bool _canUnFocusOnNextLeftPointerRelease = true;

		internal void ProcessPointerDown(PointerRoutedEventArgs args)
		{
#if !UNO_HAS_MANAGED_POINTERS
			if (_reRouted is { } reRouted)
			{
				_reRouted = null;
				if (reRouted.Args == args)
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
						this.Log().Debug($"Re-routing pointer event from {reRouted.From.GetDebugName()} to {reRouted.To.GetDebugName()}");

					// Make sure to change the OriginalSource to reflect the updated target.
					args.OriginalSource = reRouted.To;

					// First make sure to have a logical state regarding current over check use to determine if events are relevant or not.
					args.Reset(canBubbleNatively: false);
					reRouted.To.OnPointerEnter(args);

					// Raise the event to the target
					args.Reset(canBubbleNatively: false);
					reRouted.To.OnPointerDown(args);
#if __APPLE_UIKIT__
					// Also as the FlyoutPopupPanel is being removed from the UI tree, we won't get any ProcessPointerUp, so we are forcefully causing it here.
					args.Reset(canBubbleNatively: false);
					reRouted.To.OnPointerUp(args);
#endif

					args.Handled = true; // Make sure the event is flagged as handled so it won't be bubbled by native code to us again from the FlyoutPopupPanel.
					return; // The event already came back to us (due to reRouted.To.OnPointerDown(args)).
				}
			}
#endif

			if (!args.Handled && args.GetCurrentPoint(null).Properties.PointerUpdateKind is PointerUpdateKind.LeftButtonPressed)
			{
				_canUnFocusOnNextLeftPointerRelease = true;
			}
		}

		internal void ProcessPointerUp(PointerRoutedEventArgs args, bool isAfterHandledUp)
		{
			// We don't want handled events raised on RootVisual,
			// instead we wait for the element that handled it to directly forward it to us,
			// but only ** AFTER ** the up has been fully processed (with isAfterHandledUp = true).
			// This is required to be sure that element process gestures and manipulations before we raise the exit
			// (e.g. the 'tapped' event on a Button would be fired after the 'exit').
			var isHandled = args.Handled; // Capture here as args might be reset before checked for focus
			var isUpFullyDispatched = isAfterHandledUp || !isHandled;
			if (!isUpFullyDispatched)
			{
				return;
			}

#if __ANDROID__ || __APPLE_UIKIT__ // Not needed on WASM as we do have native support of the exit event
			// On iOS we use the RootVisual to raise the UWP-only exit event (in managed only)
			// Note: This is useless for managed pointers where the Exit is raised properly

			if (args.Pointer.PointerDeviceType is PointerDeviceType.Touch
				&& args.OriginalSource is UIElement src)
			{
				// It's acceptable to use only the OriginalSource on Android and iOS:
				// since those platforms have "implicit capture" and captures are propagated to the OS,
				// the OriginalSource will be the element that has capture (if any).

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Re-dispatching pointer {args.Pointer} to {src.GetDebugName()} to inject exit event.");
				}

				src.RedispatchPointerExited(args.Reset(canBubbleNatively: false));
			}
#endif

			// Uno specific: To ensure focus is properly lost when clicking "outside" the app's content,
			// we set focus here. In the case of UWP, the focus is set to the root ScrollViewer instead,
			// but Uno does not have it on all targets yet.
			var focusedElement = _inputManager.ContentRoot.XamlRoot is { } xamlRoot
				? FocusManager.GetFocusedElement(xamlRoot)
				: null;
			if (!isHandled // so isAfterHandledUp is false!
				&& _canUnFocusOnNextLeftPointerRelease
				&& args.GetCurrentPoint(null).Properties.PointerUpdateKind is PointerUpdateKind.LeftButtonReleased
				&& !PointerCapture.TryGet(args.Pointer, out _)
				&& focusedElement is UIElement uiElement)
			{
				uiElement.Unfocus();
			}

			ReleaseCaptures(args.Reset(canBubbleNatively: false));

			// At the end of our "up" processing, we reset the flag to make sure that the native handler (iOS, Android and WASM)
			// won't try to sent it to us again (if not already the case ^^).
			// (This could be the case if the args was flagged as handled in the ReleaseCaptures call above, like in RatingControl).
			args.Handled = isAfterHandledUp;
		}

		// As focus event are either async or cancellable,
		// the FocusManager will explicitly notify us instead of listing to its events
		internal void NotifyFocusChanged()
			=> _canUnFocusOnNextLeftPointerRelease = false;
		#endregion

		#region Misc helpers
		private PointerEventDispatchResult ReleaseCaptures(PointerRoutedEventArgs routedArgs)
		{
			var result = default(PointerEventDispatchResult);
			UIElement.BeginPointerEventDispatch();
			try
			{
				if (PointerCapture.TryGet(routedArgs.Pointer, out var capture))
				{
					foreach (var target in capture.Targets.ToList())
					{
						target.Element.ReleasePointerCapture(capture.Pointer.UniqueId, kinds: PointerCaptureKind.Any);

					}
				}
			}
			finally
			{
				result += UIElement.EndPointerEventDispatch();
			}

			return result;
		}
		#endregion
	}
}
