using System;
using System.Drawing;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Extensions;
using System.Linq;
using Windows.UI.Core;
using Uno.Diagnostics.Eventing;
using Foundation;
using AppKit;
using CoreGraphics;
using Uno.Collections;
using Windows.UI.Xaml.Input;
using WebKit;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml;
using Uno.UI.Controls;
using Uno.Logging;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Core.Preview;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Input;
using Point = Windows.Foundation.Point;

namespace Uno.UI.Controls
{
	/// <summary>
	/// A UIWindow which handle the focus item.
	/// </summary>
	public partial class Window : NSWindow
	{
		internal delegate NSDragOperation DraggingStage1Handler(NSDraggingInfo draggingInfo);
		internal delegate void DraggingStage2Handler(NSDraggingInfo draggingInfo);
		internal delegate bool PerformDragOperationHandler(NSDraggingInfo draggingInfo);

		private static readonly WeakAttachedDictionary<NSView, string> _attachedProperties = new WeakAttachedDictionary<NSView, string>();
		private const string NeedsKeyboardAttachedPropertyKey = "NeedsKeyboard";

		private readonly InputPane _inputPane;
		private WeakReference<NSScrollView> _scrollViewModifiedForKeyboard;

		/// <summary>
		/// ctor.
		/// </summary>
		public Window(CGRect contentRect, NSWindowStyle aStyle, NSBackingStore bufferingType, bool deferCreation)
			: base(contentRect, aStyle, bufferingType, deferCreation)
		{
			Delegate = new WindowDelegate(this);
			_inputPane = InputPane.GetForCurrentView();
			_inputPane.Window = this;

			// Register the types that can be dragging into the window (effectively 'AllowDrop').
			// This must be done before dragging enters the window or dragging methods won't be invoked.
			// We register for all possible types and then confirm whether they
			// are actually supported within the draggingEntered and draggingUpdated methods.
			// This has a minor side effect in the standard UI (zoom effect) but is considered acceptable.
			RegisterForDraggedTypes(new string[]
				{
					NSPasteboard.NSPasteboardTypeUrl,
					NSPasteboard.NSPasteboardTypeTIFF,
					NSPasteboard.NSPasteboardTypePNG,
					NSPasteboard.NSPasteboardTypeHTML,
					NSPasteboard.NSPasteboardTypeRTF,
					NSPasteboard.NSPasteboardTypeRTFD,
					NSPasteboard.NSPasteboardTypeFileUrl,
					NSPasteboard.NSPasteboardTypeString

					/* For future use
					UTType.URL,
					UTType.Image,
					UTType.HTML,
					UTType.RTF,
					UTType.FileURL,
					UTType.PlainText,
					*/
				});
		}

		/// <inheritdoc />
		public override void SendEvent(NSEvent evt)
		{
			try
			{
				// The effective location in top/left coordinates.
				var posInWindow = new Point(evt.LocationInWindow.X, VisibleFrame.Height - evt.LocationInWindow.Y);
				if (posInWindow.Y < 0)
				{
					// We are in the titlebar, let send the event to native code ... so close button will continue to work
					base.SendEvent(evt);
					return;
				}

				switch (evt.Type)
				{
					case NSEventType.MouseEntered:
						CoreWindowEvents?.RaisePointerEntered(ToPointerArgs(evt, posInWindow));
						break;

					case NSEventType.MouseExited:
						CoreWindowEvents?.RaisePointerExited(ToPointerArgs(evt, posInWindow));
						break;

					case NSEventType.LeftMouseDown:
					case NSEventType.OtherMouseDown:
					case NSEventType.RightMouseDown:
						CoreWindowEvents?.RaisePointerPressed(ToPointerArgs(evt, posInWindow));
						break;

					case NSEventType.LeftMouseUp:
					case NSEventType.OtherMouseUp:
					case NSEventType.RightMouseUp:
						CoreWindowEvents?.RaisePointerReleased(ToPointerArgs(evt, posInWindow));
						break;

					case NSEventType.MouseMoved:
					case NSEventType.LeftMouseDragged:
					case NSEventType.OtherMouseDragged:
					case NSEventType.RightMouseDragged:
					case NSEventType.TabletPoint:
					case NSEventType.TabletProximity:
					case NSEventType.DirectTouch:
						CoreWindowEvents?.RaisePointerMoved(ToPointerArgs(evt, posInWindow));
						break;

					case NSEventType.ScrollWheel:
						CoreWindowEvents?.RaisePointerWheelChanged(ToPointerArgs(evt, posInWindow));
						break;

					default:
						base.SendEvent(evt);
						break;
				}
			}
			catch (Exception e)
			{
				Application.Current?.RaiseRecoverableUnhandledException(e);
			}
		}

		private Rect VisibleFrame => ContentRectFor(Frame);

		#region Pointers
		private const int TabletPointEventSubtype = 1;
		private const int TabletProximityEventSubtype = 2;

		private const int LeftMouseButtonMask = 1;
		private const int RightMouseButtonMask = 2;

		internal ICoreWindowEvents CoreWindowEvents { private get; set; }

		private static PointerEventArgs ToPointerArgs(NSEvent nativeEvent, Point posInWindow)
		{
			var point = GetPointerPoint(nativeEvent, posInWindow);
			var modifiers = GetVirtualKeyModifiers(nativeEvent);
			var args = new PointerEventArgs(point, modifiers);

			return args;
		}

		private static PointerPoint GetPointerPoint(NSEvent nativeEvent, Point posInWindow)
		{
			var frameId = ToFrameId(nativeEvent.Timestamp);
			var timestamp = ToTimestamp(nativeEvent.Timestamp);
			var pointerDeviceType = GetPointerDeviceType(nativeEvent);
			var pointerDevice = PointerDevice.For(pointerDeviceType);
			var pointerId = pointerDeviceType == PointerDeviceType.Pen
				? (uint)nativeEvent.PointingDeviceID()
				: (uint)1;
			var isInContact = GetIsInContact(nativeEvent);
			var properties = GetPointerProperties(nativeEvent, pointerDeviceType);

			return new PointerPoint(frameId, timestamp, pointerDevice, pointerId, posInWindow, posInWindow, isInContact, properties);
		}

		private static PointerPointProperties GetPointerProperties(NSEvent nativeEvent, PointerDeviceType pointerType)
		{
			var properties = new PointerPointProperties()
			{
				IsInRange = true,
				IsPrimary = true,
				IsLeftButtonPressed = ((int)NSEvent.CurrentPressedMouseButtons & LeftMouseButtonMask) == LeftMouseButtonMask,
				IsRightButtonPressed = ((int)NSEvent.CurrentPressedMouseButtons & RightMouseButtonMask) == RightMouseButtonMask,
			};

			if (pointerType == PointerDeviceType.Pen)
			{
				properties.XTilt = (float)nativeEvent.Tilt.X;
				properties.YTilt = (float)nativeEvent.Tilt.Y;
				properties.Pressure = (float)nativeEvent.Pressure;
			}

			if (nativeEvent.Type == NSEventType.ScrollWheel)
			{
				var y = (int)nativeEvent.ScrollingDeltaY;
				if (y == 0)
				{
					// Note: if X and Y are != 0, we should raise 2 events!
					properties.IsHorizontalMouseWheel = true;
					properties.MouseWheelDelta = (int)nativeEvent.ScrollingDeltaX;
				}
				else
				{
					properties.MouseWheelDelta = -y;
				}
			}

			return properties;
		}

		#region Misc static helpers
		private static long? _bootTime;

		private static bool GetIsInContact(NSEvent nativeEvent)
			=> nativeEvent.Type == NSEventType.LeftMouseDown
				|| nativeEvent.Type == NSEventType.LeftMouseDragged
				|| nativeEvent.Type == NSEventType.RightMouseDown
				|| nativeEvent.Type == NSEventType.RightMouseDragged
				|| nativeEvent.Type == NSEventType.OtherMouseDown
				|| nativeEvent.Type == NSEventType.OtherMouseDragged;

		private static PointerDeviceType GetPointerDeviceType(NSEvent nativeEvent)
		{
			if (nativeEvent.Type == NSEventType.DirectTouch)
			{
				return PointerDeviceType.Touch;
			}
			if (IsTabletPointingEvent(nativeEvent))
			{
				return PointerDeviceType.Pen;
			}
			return PointerDeviceType.Mouse;
		}

		private static VirtualKeyModifiers GetVirtualKeyModifiers(NSEvent nativeEvent)
		{
			var modifiers = VirtualKeyModifiers.None;

			if (nativeEvent.ModifierFlags.HasFlag(NSEventModifierMask.AlphaShiftKeyMask) ||
				nativeEvent.ModifierFlags.HasFlag(NSEventModifierMask.ShiftKeyMask))
			{
				modifiers |= VirtualKeyModifiers.Shift;
			}

			if (nativeEvent.ModifierFlags.HasFlag(NSEventModifierMask.AlternateKeyMask))
			{
				modifiers |= VirtualKeyModifiers.Menu;
			}

			if (nativeEvent.ModifierFlags.HasFlag(NSEventModifierMask.CommandKeyMask))
			{
				modifiers |= VirtualKeyModifiers.Windows;
			}

			if (nativeEvent.ModifierFlags.HasFlag(NSEventModifierMask.ControlKeyMask))
			{
				modifiers |= VirtualKeyModifiers.Control;
			}

			return modifiers;
		}

		private static ulong ToTimestamp(double timestamp)
		{
			if (!_bootTime.HasValue)
			{
				_bootTime = DateTime.UtcNow.Ticks - (long)(TimeSpan.TicksPerSecond * new NSProcessInfo().SystemUptime);
			}

			return (ulong)_bootTime.Value + (ulong)(TimeSpan.TicksPerSecond * timestamp);
		}

		private static uint ToFrameId(double timestamp)
		{
			// The precision of the frameId is 10 frame per ms ... which should be enough
			return (uint)(timestamp * 1000.0 * 10.0);
		}

		/// <summary>
		/// Taken from <see cref="https://github.com/xamarin/xamarin-macios/blob/bc492585d137d8c3d3a2ffc827db3cdaae3cc869/src/AppKit/NSEvent.cs#L127" />
		/// </summary>
		/// <param name="nativeEvent">Native event</param>
		/// <returns>Value indicating whether the event is recognized as a "mouse" event.</returns>
		private static bool IsMouseEvent(NSEvent nativeEvent)
		{
			switch (nativeEvent.Type)
			{
				case NSEventType.LeftMouseDown:
				case NSEventType.LeftMouseUp:
				case NSEventType.RightMouseDown:
				case NSEventType.RightMouseUp:
				case NSEventType.MouseMoved:
				case NSEventType.LeftMouseDragged:
				case NSEventType.RightMouseDragged:
				case NSEventType.MouseEntered:
				case NSEventType.MouseExited:
				case NSEventType.OtherMouseDown:
				case NSEventType.OtherMouseUp:
				case NSEventType.OtherMouseDragged:
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Inspiration from <see cref="https://github.com/xamarin/xamarin-macios/blob/bc492585d137d8c3d3a2ffc827db3cdaae3cc869/src/AppKit/NSEvent.cs#L148"/>
		/// with some modifications.
		/// </summary>
		/// <param name="nativeEvent">Native event</param>
		/// <returns>Value indicating whether the event is in fact coming from a tablet device.</returns>
		private static bool IsTabletPointingEvent(NSEvent nativeEvent)
		{
			//limitation - mouse entered event currently throws for Subtype
			//(selector not working, although it should, according to docs)
			if (IsMouseEvent(nativeEvent) &&
				nativeEvent.Type != NSEventType.MouseEntered &&
				nativeEvent.Type != NSEventType.MouseExited)
			{
				//Xamarin debugger proxy for NSEvent incorrectly says Subtype
				//works only for Custom events, but that is not the case
				return
					nativeEvent.Subtype == TabletPointEventSubtype ||
					nativeEvent.Subtype == TabletProximityEventSubtype;
			}
			return nativeEvent.Type == NSEventType.TabletPoint;
		}

		#endregion
		#endregion

		#region Drag and drop
		/// <summary>
		/// Method invoked when a drag operation enters the <see cref="NSWindow"/>.
		/// </summary>
		/// <remarks>
		/// 
		/// While it is never documented directly, DraggingEntered can be added to NSWindow just like NSView.
		/// Key docs telling this story include:
		/// 
		///  (1) NSWindow documentation does not include most NSView drag/drop methods
		///      https://developer.apple.com/documentation/appkit/nswindow
		///  (2) Since NSWindow documentation doesn't reference them, they also aren't included in Xamarin
		///      https://docs.microsoft.com/en-us/dotnet/api/appkit.nswindow?view=xamarin-mac-sdk-14
		///  (3) However, the 'draggingEntered' method docs reference 'Window' at the very end
		///      "Invoked when the mouse pointer enters the destination’s bounds rectangle (if it is a view object)
		///       or its frame rectangle (if it is a window object). "
		///      https://developer.apple.com/documentation/appkit/nsdraggingdestination/1416019-draggingentered
		///  (4) In legacy macOS docs this is a little more explicitly stated:
		///      https://developer.apple.com/library/archive/documentation/Cocoa/Conceptual/DragandDrop/Concepts/dragdestination.html#//apple_ref/doc/uid/20000977-BAJBJFBG
		///  (5) Other macOS drag/drop articles and blogs refer to dragging/dropping directly from Window - although
		///      give no examples.
		/// 
		/// The Export "method_name" attribute is fundamentally important to make this work, removing it will
		/// break functionality and the method will never be called by macOS. Again, this seemingly is because
		/// Xamarin.macOS is not aware of it.
		/// 
		/// </remarks>
		/// <param name="info">Information about the dragging session from the sender.</param>
		/// <returns>The accepted drag operation(s).</returns>
		[Export("draggingEntered:")] // Do not remove
		internal virtual NSDragOperation DraggingEntered(NSDraggingInfo draggingInfo)
		{
			try
			{
				return DraggingEnteredAction.Invoke(draggingInfo);
			}
			catch
			{
				return NSDragOperation.None;
			}
		}

		/// <summary>
		/// Code to execute when the <see cref="DraggingEntered(NSDraggingInfo)"/> method is invoked.
		/// This can be used in a similar way to <see cref="UIElement.DragEnter"/>.
		/// </summary>
		internal DraggingStage1Handler DraggingEnteredAction { get; set; } =
			(NSDraggingInfo draggingInfo) =>
			{
				return NSDragOperation.None;
			};

		/// <summary>
		/// Method invoked when a drag operation updates over the <see cref="NSWindow"/>.
		/// This is periodically called when the pointer is moved.
		/// </summary>
		/// <remarks>
		/// See remarks in <see cref="DraggingEntered(NSDraggingInfo)"/>
		/// </remarks>
		/// <param name="draggingInfo">Information about the dragging session from the sender.</param>
		[Export("draggingUpdated:")] // Do not remove
		internal virtual NSDragOperation DraggingUpdated(NSDraggingInfo draggingInfo)
		{
			try
			{
				return DraggingUpdatedAction.Invoke(draggingInfo);
			}
			catch
			{
				return NSDragOperation.None;
			}
		}

		/// <summary>
		/// Code to execute when the <see cref="DraggingUpdated(NSDraggingInfo)"/> method is invoked.
		/// This can be used in a similar way to <see cref="UIElement.DragOver"/>.
		/// </summary>
		internal DraggingStage1Handler DraggingUpdatedAction { get; set; } =
			(NSDraggingInfo draggingInfo) =>
			{
				return NSDragOperation.None;
			};

		/// <summary>
		/// Method invoked when a drag operation ends.
		/// </summary>
		/// <remarks>
		/// See remarks in <see cref="DraggingEntered(NSDraggingInfo)"/>
		/// </remarks>
		/// <param name="draggingInfo">Information about the dragging session from the sender.</param>
		[Export("draggingEnded:")] // Do not remove
		internal virtual void DraggingEnded(NSDraggingInfo draggingInfo)
		{
			try
			{
				DraggingEndedAction.Invoke(draggingInfo);
			}
			catch
			{
				// Simply return if an error occurred, it is unrecoverable
			}

			return;
		}

		/// <summary>
		/// Code to execute when the <see cref="DraggingEnded(NSDraggingInfo)"/> method is invoked.
		/// This can be used in a similar way to <see cref="UIElement.DropCompleted"/>.
		/// </summary>
		internal DraggingStage2Handler DraggingEndedAction { get; set; } =
			(NSDraggingInfo draggingInfo) =>
			{
				// Available for use
			};

		/// <summary>
		/// Method invoked when a drag operation exited the <see cref="NSWindow"/>.
		/// </summary>
		/// <remarks>
		/// See remarks in <see cref="DraggingEntered(NSDraggingInfo)"/>
		/// </remarks>
		/// <param name="draggingInfo">Information about the dragging session from the sender.</param>
		[Export("draggingExited:")] // Do not remove
		internal virtual void DraggingExited(NSDraggingInfo draggingInfo)
		{
			try
			{
				DraggingExitedAction.Invoke(draggingInfo);
			}
			catch
			{
				// Simply return if an error occurred, it is unrecoverable
			}

			return;
		}

		/// <summary>
		/// Code to execute when the <see cref="DraggingExited(NSDraggingInfo)"/> method is invoked.
		/// This can be used in a similar way to <see cref="UIElement.DragLeave"/>.
		/// </summary>
		internal DraggingStage2Handler DraggingExitedAction { get; set; } =
			(NSDraggingInfo draggingInfo) =>
			{
				// Available for use
			};

		/// <summary>
		/// Method invoked when the pointer is released over the <see cref="NSWindow"/> during a drag operation
		/// This is the last chance to reject a drop.
		/// </summary>
		/// <param name="draggingInfo">Information about the dragging session from the sender.</param>
		/// <returns>True if the destination accepts the drag operation; otherwise, false. </returns>
		[Export("prepareForDragOperation:")] // Do not remove
		internal virtual bool PrepareForDragOperation(AppKit.NSDraggingInfo draggingInfo)
		{
			// Always return true as UWP doesn't really have an equivalent step.
			// Drop is accepted within DraggingEntered (drag entered event in UWP).
			return true;
		}

		/// <summary>
		/// Method invoked when the pointer is released and data is dropped over the the <see cref="NSWindow"/>.
		/// </summary>
		/// <param name="draggingInfo">Information about the dragging session from the sender.</param>
		/// <returns>True if the destination accepts the data; otherwise, false.</returns>
		[Export("performDragOperation:")] // Do not remove
		internal virtual bool PerformDragOperation(NSDraggingInfo draggingInfo)
		{
			try
			{
				return PerformDragOperationAction.Invoke(draggingInfo);
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Code to execute when the <see cref="PerformDragOperation(NSDraggingInfo)"/> method is invoked.
		/// This can be used in a similar way to <see cref="UIElement.Drop"/>.
		/// </summary>
		internal PerformDragOperationHandler PerformDragOperationAction { get; set; } =
			(NSDraggingInfo draggingInfo) =>
			{
				return false;
			};
		#endregion

		#region Keyboard
		public static void SetNeedsKeyboard(NSView view, bool needsKeyboard)
		{
			if (view != null)
			{
				_attachedProperties.SetValue(view, NeedsKeyboardAttachedPropertyKey, (bool?)needsKeyboard);
			}
		}

		private static bool GetNeedsKeyboard(NSView view)
		{
			var superViews = view.FindSuperviews().ToList();
			superViews.Insert(0, view);

			return superViews.Any(superView => _attachedProperties.GetValue(superView, NeedsKeyboardAttachedPropertyKey, () => default(bool?)).GetValueOrDefault());
		}

		private static bool NeedsKeyboard(NSView view)
		{
			return view is NSTextView
				|| view is NSTextField
				|| GetNeedsKeyboard(view);
		}
		#endregion

		public BringIntoViewMode? FocusedViewBringIntoViewOnKeyboardOpensMode { get; set; }


		internal void MakeVisible(NSView view, BringIntoViewMode? bringIntoViewMode, bool useForcedAnimation = false)
		{
			if (view == null)
			{
				return;
			}

			if (bringIntoViewMode == null)
			{
				return;
			}

			var scrollView = view.FindSuperviewsOfType<NSScrollView>().LastOrDefault();
			_scrollViewModifiedForKeyboard = new WeakReference<NSScrollView>(scrollView);

			if (scrollView == null)
			{
				this.Log().Warn("Keyboard will show, but we cannot find any ScrollView with enough space for the currently focused view, so it's impossible to ensure that it's visible.");
				return;
			}


			var scrollViewRectInWindow = scrollView.ConvertRectFromView(scrollView.Bounds, scrollView);

			var keyboardTop = (nfloat)_inputPane.OccludedRect.Top;

			var keyboardOverlap = scrollViewRectInWindow.Bottom - keyboardTop;
			if (keyboardOverlap > 0)
			{
				scrollView.ContentInsets = new NSEdgeInsets(0, 0, keyboardOverlap, 0);
			}

			var viewRectInScrollView = CGRect.Empty;

			if (!(view is TextBox))
			{
				// We want to scroll to the textbox and not the inner textview.
				view = view.FindFirstParent<TextBox>() ?? view;
			}

			viewRectInScrollView = scrollView.ConvertRectFromView(view.Bounds, view);

			scrollView.ScrollRectToVisible(viewRectInScrollView);
		}

		private class WindowDelegate : NSWindowDelegate
		{
			private readonly NSWindow _window;

			public WindowDelegate(NSWindow window)
			{
				_window = window;
			}

			public override void DidMiniaturize(NSNotification notification)
			{
				Windows.UI.Xaml.Window.Current?.OnVisibilityChanged(false);
				Windows.UI.Xaml.Window.Current?.OnActivated(CoreWindowActivationState.Deactivated);
				Windows.UI.Xaml.Application.Current?.OnEnteredBackground();
			}

			public override void DidDeminiaturize(NSNotification notification)
			{
				Windows.UI.Xaml.Application.Current?.OnLeavingBackground();
				Windows.UI.Xaml.Window.Current?.OnVisibilityChanged(true);
				Windows.UI.Xaml.Window.Current?.OnActivated(CoreWindowActivationState.CodeActivated);
			}

			public override void DidBecomeKey(NSNotification notification)
			{
				Windows.UI.Xaml.Window.Current?.OnActivated(CoreWindowActivationState.CodeActivated);
			}

			public override void DidResignKey(NSNotification notification)
			{
				Windows.UI.Xaml.Window.Current?.OnActivated(CoreWindowActivationState.Deactivated);
			}

			public override async void DidBecomeMain(NSNotification notification)
			{
				ApplicationView.GetForCurrentView().SyncTitleWithWindow(_window);
				// Ensure custom cursor is reset after window activation.
				// Artificial delay is necessary due to the fact that setting cursor
				// immediately after window becoming main does not have any effect.
				await Task.Delay(100);
				Windows.UI.Core.CoreWindow.GetForCurrentThread().RefreshCursor();
			}

			public override bool WindowShouldClose(NSObject sender)
			{
				var manager = SystemNavigationManagerPreview.GetForCurrentView();
				if (!manager.HasConfirmedClose)
				{
					manager.OnCloseRequested();
					if (!manager.HasConfirmedClose)
					{
						// Close was either deferred or canceled synchronously.
						return false;
					}
				}

				// closing should continue, perform suspension

				if (!Application.Current.Suspended)
				{
					Application.Current.OnSuspending();
					return Application.Current.Suspended;
				}

				// all prerequisites passed, can safely close
				return true;
			}
		}
	}
}
