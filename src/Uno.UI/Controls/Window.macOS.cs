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

namespace Uno.UI.Controls
{
	/// <summary>
	/// A UIWindow which handle the focus item.
	/// </summary>
	public partial class Window : NSWindow
	{
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
		public virtual NSDragOperation DraggingEntered(NSDraggingInfo draggingInfo)
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
		public Func<NSDraggingInfo, NSDragOperation> DraggingEnteredAction { get; set; } =
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
		public virtual NSDragOperation DraggingUpdated(NSDraggingInfo draggingInfo)
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
		public Func<NSDraggingInfo, NSDragOperation> DraggingUpdatedAction { get; set; } =
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
		public virtual void DraggingEnded(NSDraggingInfo draggingInfo)
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
		public Action<NSDraggingInfo> DraggingEndedAction { get; set; } =
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
		public virtual void DraggingExited(NSDraggingInfo draggingInfo)
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
		public Action<NSDraggingInfo> DraggingExitedAction { get; set; } =
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
		public virtual bool PrepareForDragOperation(AppKit.NSDraggingInfo draggingInfo)
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
		public virtual bool PerformDragOperation(NSDraggingInfo draggingInfo)
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
		public Func<NSDraggingInfo, bool> PerformDragOperationAction { get; set; } =
			(NSDraggingInfo draggingInfo) =>
			{
				return false;
			};

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

			public override async void DidBecomeMain(NSNotification notification)
			{
				ApplicationView.GetForCurrentView().SyncTitleWithWindow(_window);
				// Ensure custom cursor is reset after window activation.
				// Artificial delay is necessary due to the fact that setting cursor
				// immediately after window becoming main does not have any effect.
				await Task.Delay(100);
				CoreWindow.GetForCurrentThread().RefreshCursor();
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
