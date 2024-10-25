#nullable enable

using System;
using System.Numerics;
using System.Threading;
using AppKit;
using CoreGraphics;
using Foundation;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Point = Windows.Foundation.Point;
using UIElement = Windows.UI.Xaml.UIElement;
using ObjCRuntime;
using NSDraggingInfo = AppKit.INSDraggingInfo;
using Uno.UI.Xaml.Controls;

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	internal class MacOSDragDropExtension : IDragDropExtension
	{
		private readonly long _fakePointerId = Pointer.CreateUniqueIdForUnknownPointer();
		private readonly DragDropManager _manager;
		private Uno.UI.Controls.Window _window;

		public MacOSDragDropExtension(DragDropManager owner)
		{
			_manager = owner;
			_window = (Uno.UI.Controls.Window)NativeWindowWrapper.Instance.NativeWindow;

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
			_ = _manager.ProcessAborted(_fakePointerId);
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
			=> _ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.High, async () =>
			{
				try
				{
					var sourceEvent = new NSEvent();
					var source = new NSDraggingSource2();

					source.DraggingSessionEndedAction =
						(NSImage image, CGPoint screenPoint, NSDragOperation operation) =>
						{
							// The drop was completed externally
							_manager.ProcessDropped(new DragEventSource(info.SourceId, _window));
							return;
						};

					if (_window.ContentView != null)
					{
						_window.ContentView.BeginDraggingSession(
							await DataPackage.CreateNativeDragDropData(info.Data, info.GetPosition(null)),
							sourceEvent,
							source);
					}
					else
					{
						this.Log().Error("Failed to start native Drag and Drop (Window.ContentView is null)");
					}
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

		/// <summary>
		/// Represents the source of a native macOS dragging operation.
		/// </summary>
		private class NSDraggingSource2 : NSDraggingSource
		{
			public delegate void DraggingSessionUpdatedHandler(NSImage image, CGPoint screenPoint);
			public delegate void DraggingSessionEndedHandler(NSImage image, CGPoint screenPoint, NSDragOperation operation);

			/* The following currently documented methods do not seem to work.
			 * These methods also don't exist in the Xamarin macOS implementation.
			 *   https://developer.apple.com/documentation/appkit/nsdraggingsource
			 *
			 *   [Export("draggingSession:willBeginAt:")]
			 *   public void DraggingSessionWillBegin(NSDraggingSession session, CGPoint screenPoint)
			 *
			 *   [Export("draggingSession:movedTo:")]
			 *   public void DraggingSessionMoved(NSDraggingSession session, CGPoint screenPoint)
			 *
			 *   [Export("draggingSession:endedAt:operation:")]
			 *   public void DraggingSessionEnded(NSDraggingSession session, CGPoint screenPoint, NSDragOperation operation)
			 *
			 * Instead, what does work are the old methods - still present for backwards compatibility.
			 * The signatures corresponding to the legacy docs here:
			 *   https://developer.apple.com/library/archive/documentation/Cocoa/Conceptual/DragandDrop/Concepts/dragsource.html
			 */

			/// <summary>
			/// Method invoked when a dragging session starts.
			/// </summary>
			/// <param name="image">The image being dragged.</param>
			/// <param name="screenPoint">he origin point of the dragging image in screen coordinates.</param>
			[Export("draggedImage:beganAt:")] // Do not remove
			public virtual void DraggingSessionWillBegin(NSImage image, CGPoint screenPoint)
			{
				try
				{
					DraggingSessionWillBeginAction.Invoke(image, screenPoint);
				}
				catch
				{
					// Simply return if an error occurred, it is unrecoverable
				}

				return;
			}

			/// <summary>
			/// Code to execute when the <see cref="DraggingSessionWillBegin(NSImage, CGPoint)"/> method is invoked.
			/// </summary>
			public DraggingSessionUpdatedHandler DraggingSessionWillBeginAction { get; set; } =
				(NSImage image, CGPoint screenPoint) =>
				{
					// Available for use
				};

			/// <summary>
			/// Method invoked when the pointer moves during a dragging session.
			/// </summary>
			/// <param name="image">The image being dragged.</param>
			/// <param name="screenPoint">The origin point of the dragging image in screen coordinates.</param>
			[Export("draggedImage:movedTo:")] // Do not remove
			public virtual void DraggingSessionMoved(NSImage image, CGPoint screenPoint)
			{
				try
				{
					DraggingSessionMovedAction.Invoke(image, screenPoint);
				}
				catch
				{
					// Simply return if an error occurred, it is unrecoverable
				}

				return;
			}

			/// <summary>
			/// Code to execute when the <see cref="DraggingSessionMoved(NSImage, CGPoint)"/> method is invoked.
			/// </summary>
			public DraggingSessionUpdatedHandler DraggingSessionMovedAction { get; set; } =
				(NSImage image, CGPoint screenPoint) =>
				{
					// Available for use
				};

			/// <summary>
			/// Method invoked when a dragging session ends.
			/// </summary>
			/// <param name="image">The image being dragged.</param>
			/// <param name="screenPoint">The origin point of the dragging image in screen coordinates.</param>
			/// <param name="operation">The completed drag operation.</param>
			[Export("draggedImage:endedAt:operation:")] // Do not remove
			public virtual void DraggingSessionEnded(NSImage image, CGPoint screenPoint, NSDragOperation operation)
			{
				try
				{
					DraggingSessionEndedAction.Invoke(image, screenPoint, operation);
				}
				catch
				{
					// Simply return if an error occurred, it is unrecoverable
				}

				return;
			}

			/// <summary>
			/// Code to execute when the <see cref="DraggingSessionEnded(NSImage, CGPoint, NSDragOperation)"/> method is invoked.
			/// </summary>
			public DraggingSessionEndedHandler DraggingSessionEndedAction { get; set; } =
				(NSImage image, CGPoint screenPoint, NSDragOperation operation) =>
				{
					// Available for use
				};
		}

		private class DragEventSource : IDragEventSource
		{
			private static long _nextFrameId;
			private readonly NSDraggingInfo? _macOSDraggingInfo;
			private readonly NSWindow _window;

			public DragEventSource(long pointerId, NSWindow window)
			{
				Id = pointerId;

				_macOSDraggingInfo = null;
				_window = window;
			}

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
				CGPoint windowLocation = new CGPoint(0.0, 0.0);
				if (_macOSDraggingInfo != null && _window.ContentView is { } contentView)
				{
					windowLocation = contentView.ConvertPointFromView(_macOSDraggingInfo.DraggingLocation, null);
				}
				var location = new Windows.Foundation.Point(windowLocation.X, windowLocation.Y);

				// macOS requires access to NSEvent.ModifierFlags to determine key press.
				// This isn't available from the dragging info.
				var mods = DragDropModifiers.None;

				return (location, mods);
			}

			/// <inheritdoc />
			public Point GetPosition(object? relativeTo)
			{
				CGPoint windowLocation = new CGPoint(0.0, 0.0);
				if (_macOSDraggingInfo != null && _window.ContentView is { } contentView)
				{
					windowLocation = contentView.ConvertPointFromView(_macOSDraggingInfo.DraggingLocation, null);
				}
				var rawPosition = new Point(windowLocation.X, windowLocation.Y);

				if (relativeTo is null)
				{
					return rawPosition;
				}

				if (relativeTo is UIElement elt)
				{
					var eltToRoot = UIElement.GetTransform(elt, null);
					var rootToElt = eltToRoot.Inverse();

					return rootToElt.Transform(rawPosition);
				}

				throw new InvalidOperationException("The relative to must be a UIElement.");
			}
		}
	}
}
