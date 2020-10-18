#if __MACOS__
#nullable enable

using System;
using System.Numerics;
using System.Threading;
using AppKit;
using Uno.Extensions;
using Uno.Logging;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Point = Windows.Foundation.Point;
using UIElement = Windows.UI.Xaml.UIElement;

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	internal class MacOSDragDropExtension : IDragDropExtension
	{
		private readonly long _fakePointerId = Pointer.CreateUniqueIdForUnknownPointer();
		private readonly DragDropManager _manager;
		private Uno.UI.Controls.Window _window;

		public MacOSDragDropExtension(DragDropManager owner)
		{
			_manager = (DragDropManager)owner;
			_window = (Uno.UI.Controls.Window)CoreWindow.GetForCurrentThread()!._window;

			_window.DraggingEnteredAction = OnDraggingEnteredEvent;
			_window.DraggingUpdatedAction = OnDraggingUpdated;
			_window.DraggingExitedAction = OnDraggingExited;
			_window.PerformDragOperationAction = OnPerformDragOperation;
			_window.DraggingEndedAction = OnDraggingEnded;
		}

		private NSDragOperation OnDraggingEnteredEvent(NSDraggingInfo draggingInfo)
		{
			var source = new DragEventSource(_fakePointerId, draggingInfo, _window);
			var data = DataPackage.CreateFromNativeDragDropData(draggingInfo);
			var allowedOperations = DataPackageOperation.Copy; // Should match with return value
			var info = new CoreDragInfo(source, data, allowedOperations);

			CoreDragDropManager.GetForCurrentView()?.DragStarted(info);

			// Note: No need to _manager.ProcessMove, the DragStarted will actually have the same effect

			// To allow the macOS drag to continue, we must assume drop is supported here.
			// This will immediately be updated within the OnDraggingUpdated method.
			return NSDragOperation.Copy;
		}

		private NSDragOperation OnDraggingUpdated(NSDraggingInfo draggingInfo)
		{
			var operation = _manager.ProcessMoved(new DragEventSource(_fakePointerId, draggingInfo, _window));
			return ToNSDragOperation(operation);
		}

		private void OnDraggingExited(NSDraggingInfo draggingInfo)
		{
			_ = _manager.ProcessAborted(new DragEventSource(_fakePointerId, draggingInfo, _window));
			return;
		}

		private bool OnPerformDragOperation(NSDraggingInfo draggingInfo)
		{
			var operation = _manager.ProcessDropped(new DragEventSource(_fakePointerId, draggingInfo, _window));
			return (operation != DataPackageOperation.None);
		}

		private void OnDraggingEnded(NSDraggingInfo draggingInfo)
		{
			return;
		}

		public void StartNativeDrag(CoreDragInfo info)
			=> CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.High, async () =>
			{
				try
				{
					NSDraggingSource source = new NSDraggingSource();
					NSEvent sourceEvent = new NSEvent();

					_window.ContentView.BeginDraggingSession(
						await DataPackage.CreateNativeDragDropData(info.Data, info.GetPosition(null)),
						sourceEvent,
						source);
				}
				catch (Exception e)
				{
					this.Log().Error("Failed to start native Drag and Drop.", e);
				}
			});

		/// <summary>
		/// Converts the given UWP <see cref="DataPackageOperation"/> into the equivalent macOS
		/// <see cref="NSDragOperation"/>.
		/// </summary>
		/// <param name="uwpOperation">The UWP <see cref="DataPackageOperation"/> to convert.</param>
		/// <returns>The equivalent macOS <see cref="NSDragOperation"/>; otherwise, <see cref="NSDragOperation.None"/>.</returns>
		private static NSDragOperation ToNSDragOperation(DataPackageOperation uwpOperation)
		{
			NSDragOperation result = NSDragOperation.None;

			if (uwpOperation.HasFlag(DataPackageOperation.Copy))
			{
				result |= NSDragOperation.Copy;
			}

			if (uwpOperation.HasFlag(DataPackageOperation.Link))
			{
				result |= NSDragOperation.Link;
			}

			if (uwpOperation.HasFlag(DataPackageOperation.Move))
			{
				result |= NSDragOperation.Move;
			}

			return result;
		}

		private class DragEventSource : IDragEventSource
		{
			private static long _nextFrameId;
			private readonly NSDraggingInfo _macOSDraggingInfo;
			private	readonly NSWindow _window;

			public DragEventSource(long pointerId, NSDraggingInfo draggingInfo, NSWindow window)
			{
				Id = pointerId;

				_macOSDraggingInfo = draggingInfo;
				_window = window;
			}

			public long Id { get; }

			public uint FrameId { get; } = (uint)Interlocked.Increment(ref _nextFrameId);

			/// <inheritdoc />
			public (Point location, DragDropModifiers modifier) GetState()
			{
				var windowLocation = _window.ContentView.ConvertPointFromView(_macOSDraggingInfo.DraggingLocation, null);
				var location = new Windows.Foundation.Point(windowLocation.X, windowLocation.Y);

				// macOS requires access to NSEvent.ModifierFlags to determine key press.
				// This isn't available from the dragging info.
				var mods = DragDropModifiers.None;

				return (location, mods);
			}

			/// <inheritdoc />
			public Point GetPosition(object? relativeTo)
			{
				var windowLocation = _window.ContentView.ConvertPointFromView(_macOSDraggingInfo.DraggingLocation, null);
				var rawPosition = new Point(windowLocation.X, windowLocation.Y);

				if (relativeTo is null)
				{
					return rawPosition;
				}

				if (relativeTo is UIElement elt)
				{
					var eltToRoot = UIElement.GetTransform(elt, null);
					Matrix3x2.Invert(eltToRoot, out var rootToElt);

					return rootToElt.Transform(rawPosition);
				}

				throw new InvalidOperationException("The relative to must be a UIElement.");
			}
		}
	}
}

#endif
