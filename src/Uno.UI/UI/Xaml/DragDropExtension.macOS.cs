#if __MACOS__
#nullable enable

using System;
using System.Numerics;
using System.Threading;
using AppKit;
using Foundation;
using MobileCoreServices;
using Uno.Extensions;
using Uno.Logging;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Point = Windows.Foundation.Point;
using UIElement = Windows.UI.Xaml.UIElement;

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	internal class MacOSDragDropExtension : IDragDropExtension
	{
		private readonly DragDropManager _manager;
		private NSWindow _nativeWindowHost;
		private Uno.UI.Controls.Window _window;

		public MacOSDragDropExtension(DragDropManager owner)
		{
			_manager = (DragDropManager)owner;

			_nativeWindowHost = AppKit.NSApplication.SharedApplication.DangerousWindows[0];
			_window = (Uno.UI.Controls.Window)CoreWindow.GetForCurrentThread()!._window;

			_window.DraggingEnteredAction = OnDraggingEnteredEvent;
			_window.DraggingUpdatedAction = OnDraggingUpdated;
			_window.DraggingExitedAction = OnDraggingExited;
			_window.PerformDragOperationAction = OnPerformDragOperation;
		}

		private NSDragOperation OnDraggingEnteredEvent(NSDraggingInfo draggingInfo)
		{
			return NSDragOperation.Copy;
		}

		private NSDragOperation OnDraggingUpdated(NSDraggingInfo draggingInfo)
		{
			return NSDragOperation.Copy;
		}

		private void OnDraggingExited(NSDraggingInfo draggingInfo)
		{
			return;
		}

		private bool OnPerformDragOperation(NSDraggingInfo draggingInfo)
		{
			return true;
		}

		public void StartNativeDrag(CoreDragInfo info)
			=> CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.High, async () =>
			{
				try
				{
					NSDraggingSource source = new NSDraggingSource();
					NSEvent sourceEvent = new NSEvent();

					var text = await info.Data.GetTextAsync();

					var draggingItem = new NSDraggingItem((NSString)text);
					draggingItem.DraggingFrame = new CoreGraphics.CGRect(0, 0, 1, 1);

					//_nativeView?.BeginDraggingSession(new[] { draggingItem }, sourceEvent, source);
				}
				catch (Exception e)
				{
					this.Log().Error("Failed to start native Drag and Drop.", e);
				}
			});

		private class DragEventSource : IDragEventSource
		{
			private static long _nextFrameId;

			public DragEventSource(long pointerId)
			{
				Id = pointerId;
			}

			public long Id { get; }

			public uint FrameId { get; } = (uint)Interlocked.Increment(ref _nextFrameId);

			/// <inheritdoc />
			public (Point location, DragDropModifiers modifier) GetState()
			{
				return (new Windows.Foundation.Point(0, 0), DragDropModifiers.None);
			}

			/// <inheritdoc />
			public Point GetPosition(object? relativeTo)
			{
				return new Point(0, 0);
			}
		}
	}
}

#endif
