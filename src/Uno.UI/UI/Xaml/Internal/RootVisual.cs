// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// RootVisual.h, RootVisual.cpp

#nullable enable

using System;
using System.Linq;
using Uno.UI.Extensions;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;

using PointerIdentifierPool = Windows.Devices.Input.PointerIdentifierPool; // internal type (should be in Uno namespace)
#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Uno.UI.Xaml.Core
{
	/// <summary>
	/// This class is the type of the invisible root of the tree.
	/// This differs from Canvas in the way it arranges its children - it
	/// ensures that all the children are arranged with the plugin size.
	/// </summary>
	internal partial class RootVisual : Panel
	{
		private readonly CoreServices _coreServices;

		[ThreadStatic]
		private static bool _canUnFocusOnNextLeftPointerRelease = true;

		public RootVisual(CoreServices coreServices)
		{
			_coreServices = coreServices ?? throw new System.ArgumentNullException(nameof(coreServices));
			//Uno specific - flag as VisualTreeRoot for interop with existing logic
			IsVisualTreeRoot = true;
#if __WASM__
			//Uno WASM specific - set tabindex to 0 so the RootVisual is "native focusable"
			SetAttribute("tabindex", "0");

			AddHandler(
				PointerCanceledEvent,
				new PointerEventHandler((snd, args) => ProcessPointerCancelledWasm(args)),
				handledEventsToo: true);
#endif

			AddHandler(
				PointerPressedEvent,
				new PointerEventHandler((snd, args) => ProcessPointerDown(args)),
				handledEventsToo: true);
			AddHandler(
				PointerReleasedEvent,
				new PointerEventHandler((snd, args) => ProcessPointerUp(args)),
				handledEventsToo: true);
		}

		/// <summary>
		/// Gets or sets the Visual Tree.
		/// </summary>
		internal VisualTree? AssociatedVisualTree { get; set; }

		internal PopupRoot? AssociatedPopupRoot =>
			AssociatedVisualTree?.PopupRoot ?? this.GetContext().MainPopupRoot;

		internal UIElement? AssociatedPublicRoot =>
			AssociatedVisualTree?.PublicRootVisual ?? this.GetContext().VisualRoot as UIElement;

		/// <summary>
		/// Updates the color of the background brush.
		/// </summary>
		/// <param name="backgroundColor">Background color.</param>
		internal void SetBackgroundColor(Color backgroundColor) =>
			SetValue(BackgroundProperty, new SolidColorBrush(backgroundColor));

		/// <summary>
		/// Overriding virtual to add specific logic to measure pass.
		/// This behavior is the same as that of the Canvas.
		/// </summary>
		/// <param name="availableSize">Available size.</param>
		/// <returns>Desired size.</returns>
		protected override Size MeasureOverride(Size availableSize)
		{
			foreach (var child in Children)
			{
				if (child != null)
				{
					// Measure child to the plugin size
					child.Measure(availableSize);
				}
			}

			return new Size();
		}

		/// <summary>
		/// Overriding CFrameworkElement virtual to add specific logic to arrange pass.
		/// The root visual always arranges the children with the finalSize. This ensures that
		/// children of the root visual are always arranged at the plugin size.
		/// </summary>
		/// <param name="finalSize">Final arranged size.</param>
		/// <returns>Final size.</returns>
		protected override Size ArrangeOverride(Size finalSize)
		{
			foreach (var child in Children)
			{
				if (child == null)
				{
					continue;
				}

				var x = child.GetOffsetX();
				var y = child.GetOffsetY();

				if (true)//child.GetIsArrangeDirty() || child.GetIsOnArrangeDirtyPath())
				{
					//child.EnsureLayoutStorage();

					var childRect = new Rect(x, y, finalSize.Width, finalSize.Height);

					child.Arrange(childRect);
				}
			}

			return finalSize;
		}

		// As focus event are either async or cancellable,
		// the FocusManager will explicitly notify us instead of listing to its events
		internal static void NotifyFocusChanged()
			=> _canUnFocusOnNextLeftPointerRelease = false;

		private static void ProcessPointerDown(PointerRoutedEventArgs args)
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

		internal void ProcessPointerUp(PointerRoutedEventArgs args, bool isAfterHandledUp = false)
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
			var focusedElement = XamlRoot is null ?
				FocusManager.GetFocusedElement() :
				FocusManager.GetFocusedElement(XamlRoot);
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
}
