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
using Microsoft.UI.Xaml.Input;
using WebKit;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml;
using Uno.UI.Controls;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Core.Preview;
using System.Threading.Tasks;
using Uno.UI.Runtime.MacOS;
using ObjCRuntime;
using NSDraggingInfo = AppKit.INSDraggingInfo;
using Microsoft.UI.Windowing;
using Uno.UI.Xaml.Controls;

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

		private readonly InputPane _inputPane;
		private WeakReference<NSScrollView> _scrollViewModifiedForKeyboard;

		internal event TypedEventHandler<Window, SendEventArgs> OnSendEvent;

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
				// Note: Currently we have only one subscriber (PointerManager) and long term we expect to have only one **per type of event**
				//		 so we don't check for the Handled flag between handlers.
				var args = SendEventArgs.GetInstance(evt);
				OnSendEvent?.Invoke(this, args);
				if (!args.Handled)
				{
					base.SendEvent(evt);
				}
			}
			catch (Exception e)
			{
				Application.Current?.RaiseRecoverableUnhandledException(e);
			}
		}

		internal Rect VisibleFrame => ContentRectFor(Frame);

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
		internal virtual bool PrepareForDragOperation(NSDraggingInfo draggingInfo)
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

		internal class SendEventArgs
		{
			private static readonly SendEventArgs _instance = new();
			internal static SendEventArgs GetInstance(NSEvent @event)
			{
				_instance.Event = @event;
				_instance.Handled = false;

				return _instance;
			}

			public NSEvent Event { get; private set; }

			public bool Handled { get; set; }
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
				NativeWindowWrapper.Instance.OnNativeVisibilityChanged(false);
				NativeWindowWrapper.Instance.OnNativeActivated(CoreWindowActivationState.Deactivated);
				Windows.UI.Xaml.Application.Current?.RaiseEnteredBackground(null);
			}

			public override void DidDeminiaturize(NSNotification notification)
			{
				Windows.UI.Xaml.Application.Current?.RaiseLeavingBackground(null);
				NativeWindowWrapper.Instance.OnNativeVisibilityChanged(true);
				NativeWindowWrapper.Instance.OnNativeActivated(CoreWindowActivationState.CodeActivated);
			}

			public override void DidBecomeKey(NSNotification notification)
			{
				NativeWindowWrapper.Instance.OnNativeActivated(CoreWindowActivationState.CodeActivated);
			}

			public override void DidResignKey(NSNotification notification)
			{
				NativeWindowWrapper.Instance.OnNativeActivated(CoreWindowActivationState.Deactivated);
			}

			public override async void DidBecomeMain(NSNotification notification)
			{
				ApplicationView.GetForCurrentView().SyncTitleWithWindow(_window);
				// Ensure custom cursor is reset after window activation.
				// Artificial delay is necessary due to the fact that setting cursor
				// immediately after window becoming main does not have any effect.
				await Task.Delay(100);
				(Windows.UI.Core.CoreWindow.GetForCurrentThread()?.PointersSource as MacOSPointerInputSource)?.RefreshCursor();
			}

			public override bool WindowShouldClose(NSObject sender)
			{
				var closingArgs = NativeWindowWrapper.Instance.OnNativeClosing();
				if (closingArgs.Cancel)
				{
					return false;
				}

				var manager = SystemNavigationManagerPreview.GetForCurrentView();
				if (!manager.HasConfirmedClose)
				{
					if (!manager.RequestAppClose())
					{
						return false;
					}
				}

				// Closing should continue, perform suspension.
				if (!Application.Current.IsSuspended)
				{
					Application.Current.RaiseSuspending();
					return Application.Current.IsSuspended;
				}

				// All prerequisites passed, can safely close.
				return true;
			}

			public override void WillClose(NSNotification notification)
			{
				base.WillClose(notification);
				NativeWindowWrapper.Instance.OnNativeClosed();
			}
		}
	}
}
