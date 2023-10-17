#nullable enable

using System;
using System.Linq;
using Uno.UI.Extensions;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Input;

using PointerIdentifierPool = Windows.Devices.Input.PointerIdentifierPool; // internal type (should be in Uno namespace)

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Uno.UI.Xaml.Core;

/// <summary>
/// Contains Uno-specific root element logic shared across RootVisual and XamlIsland.
/// </summary>
internal class UnoRootElementLogic
{
	private bool _canUnFocusOnNextLeftPointerRelease = true;

	private readonly Panel _rootElement;

	public UnoRootElementLogic(Panel rootElement)
	{
		_rootElement = rootElement;
		//Uno specific - flag as VisualTreeRoot for interop with existing logic
		rootElement.IsVisualTreeRoot = true;
#if __WASM__
		//Uno WASM specific - set tabindex to 0 so the RootVisual is "native focusable"
		rootElement.SetAttribute("tabindex", "0");

		rootElement.AddHandler(
			UIElement.PointerCanceledEvent,
			new PointerEventHandler((snd, args) => ProcessPointerCancelledWasm(args)),
			handledEventsToo: true);
#endif

		rootElement.AddHandler(
			UIElement.PointerPressedEvent,
			new PointerEventHandler((snd, args) => ProcessPointerDown(args)),
			handledEventsToo: true);
		rootElement.AddHandler(
			UIElement.PointerReleasedEvent,
			new PointerEventHandler((snd, args) => ProcessPointerUp(args, false)),
			handledEventsToo: true);
	}

	// As focus event are either async or cancellable,
	// the FocusManager will explicitly notify us instead of listing to its events
	internal void NotifyFocusChanged()
		=> _canUnFocusOnNextLeftPointerRelease = false;

	private void ProcessPointerDown(PointerRoutedEventArgs args)
	{
		if (!args.Handled
			&& args.GetCurrentPoint(null).Properties.PointerUpdateKind is PointerUpdateKind.LeftButtonPressed)
		{
			_canUnFocusOnNextLeftPointerRelease = true;
		}
	}

#if __WASM__
	private static void ProcessPointerCancelledWasm(PointerRoutedEventArgs args)
		=> PointerIdentifierPool.ReleaseManaged(args.Pointer.UniqueId);
#endif

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

#if __ANDROID__ || __IOS__ // Not needed on WASM as we do have native support of the exit event
		// On iOS we use the RootVisual to raise the UWP-only exit event (in managed only)
		// Note: This is useless for managed pointers where the Exit is raised properly

		if (args.Pointer.PointerDeviceType is PointerDeviceType.Touch
			&& args.OriginalSource is UIElement src)
		{
			// It's acceptable to use only the OriginalSource on Android and iOS:
			// since those platforms have "implicit capture" and captures are propagated to the OS,
			// the OriginalSource will be the element that has capture (if any).

			src.RedispatchPointerExited(args.Reset(canBubbleNatively: false));
		}
#endif

		// Uno specific: To ensure focus is properly lost when clicking "outside" app's content,
		// we set focus here. In case UWP, focus is set to the root ScrollViewer instead,
		// but Uno does not have it on all targets yet.
		var focusedElement = _rootElement.XamlRoot is null ?
			FocusManager.GetFocusedElement() :
			FocusManager.GetFocusedElement(_rootElement.XamlRoot);
		if (!isHandled // so isAfterHandledUp is false!
			&& _canUnFocusOnNextLeftPointerRelease
			&& args.GetCurrentPoint(null).Properties.PointerUpdateKind is PointerUpdateKind.LeftButtonReleased
			&& !PointerCapture.TryGet(args.Pointer, out _)
			&& focusedElement is UIElement uiElement)
		{
			uiElement.Unfocus();
			args.Handled = true;
		}

		ReleaseCaptures(args.Reset(canBubbleNatively: false));

#if __WASM__
		PointerIdentifierPool.ReleaseManaged(args.Pointer.UniqueId);
#endif
	}

	private static void ReleaseCaptures(PointerRoutedEventArgs routedArgs)
	{
		if (PointerCapture.TryGet(routedArgs.Pointer, out var capture))
		{
			foreach (var target in capture.Targets.ToList())
			{
				target.Element.ReleasePointerCapture(capture.Pointer.UniqueId, kinds: PointerCaptureKind.Any);
			}
		}
	}
}
