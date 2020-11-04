#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.UI.Core;
using Windows.UI.Xaml.Input;
using Uno.Foundation.Extensibility;

namespace Windows.UI.Xaml
{
	internal sealed class DragDropManager : CoreDragDropManager.IDragDropManager
	{
		private readonly Window _window;
		private readonly List<DragOperation> _dragOperations = new List<DragOperation>();
		private readonly IDragDropExtension? _hostExtension;

		private bool _areWindowEventsRegistered;

		public DragDropManager(Window window)
		{
			_window = window;

#if __MACOS__
			// Dependency injection not currently supported on macOS
			_hostExtension = new MacOSDragDropExtension(this);
#else
			if (ApiExtensibility.CreateInstance<IDragDropExtension>(this, out var extension))
			{
				_hostExtension = extension;
			}
#endif
		}

		/// <inheritdoc />
		public bool AreConcurrentOperationsEnabled { get; set; } = false;

		/// <inheritdoc />
		public void BeginDragAndDrop(CoreDragInfo info, ICoreDropOperationTarget? target = null)
		{
			if (
#if __WASM__
				_window.Dispatcher.IsThreadingSupported &&
#endif
				!_window.Dispatcher.HasThreadAccess)
			{
				_window.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BeginDragAndDrop(info, target));
				return;
			}

			if (!AreConcurrentOperationsEnabled)
			{
				foreach (var pending in _dragOperations.ToArray())
				{
					pending.Abort();
				}
			}

			RegisterWindowHandlers();

			var op = new DragOperation(_window, _hostExtension, info, target);

			_dragOperations.Add(op);
			info.RegisterCompletedCallback(_ => _dragOperations.Remove(op));
		}

		/// <summary>
		/// This method is expected to be invoked each time a pointer involved in a drag operation is moved,
		/// no matter if the drag operation has been initiated from this app or from an external app.
		/// </summary>
		/// <returns>
		/// The last accepted operation.
		/// Be aware that due to the async processing of dragging in UWP, this might not be the up to date.
		/// </returns>
		public DataPackageOperation ProcessMoved(IDragEventSource src)
			=> FindOperation(src)?.Moved(src) ?? DataPackageOperation.None;

		/// <summary>
		/// This method is expected to be invoked when pointer involved in a drag operation is released,
		/// no matter if the drag operation has been initiated from this app or from an external app.
		/// </summary>
		/// <returns>
		/// The last accepted operation.
		/// Be aware that due to the async processing of dragging in UWP, this might not be the up to date.
		/// </returns>
		public DataPackageOperation ProcessDropped(IDragEventSource src)
			=> FindOperation(src)?.Dropped(src) ?? DataPackageOperation.None;

		/// <summary>
		/// This method is expected to be invoked when pointer involved in a drag operation
		/// is lost for operation initiated by the current app,
		/// or left the window (a.k.a. the "virtual pointer" is lost) for operation initiated by an other app.
		/// </summary>
		/// <returns>
		/// The last accepted operation.
		/// Be aware that due to the async processing of dragging in UWP, this might not be the up to date.
		/// </returns>
		public DataPackageOperation ProcessAborted(IDragEventSource src)
			=> FindOperation(src)?.Aborted(src) ?? DataPackageOperation.None;

		private DragOperation? FindOperation(IDragEventSource src)
			=> _dragOperations.FirstOrDefault(drag =>  drag.Info.SourceId == src.Id);

		private void RegisterWindowHandlers()
		{
			if (_areWindowEventsRegistered)
			{
				return;
			}

			// Those events are subscribed for safety, but they are usually useless as:
			//
			// # for internally initiated drag operation:
			//		the pointer is (implicitly) captured by the GestureRecognizer when a Drag manipulation is detected;
			//
			// # for externally initiated drag operation:
			//		the current app does not receive any pointer event, but instead receive platform specific drag events,
			//		that are expected to be interpreted by the IDragDropExtension and forwarded to this manager using the Process* methods.

			var root = _window.RootElement;
			root.AddHandler(UIElement.PointerEnteredEvent, new PointerEventHandler(OnPointerMoved), handledEventsToo: true);
			root.AddHandler(UIElement.PointerExitedEvent, new PointerEventHandler(OnPointerMoved), handledEventsToo: true);
			root.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(OnPointerMoved), handledEventsToo: true);
			root.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(OnPointerReleased), handledEventsToo: true);
			root.AddHandler(UIElement.PointerCanceledEvent, new PointerEventHandler(OnPointerCanceled), handledEventsToo: true);

			_areWindowEventsRegistered = true;
		}

		private static void OnPointerMoved(object snd, PointerRoutedEventArgs e)
			=> Window.Current.DragDrop.ProcessMoved(e);

		private static void OnPointerReleased(object snd, PointerRoutedEventArgs e)
			=> Window.Current.DragDrop.ProcessDropped(e);

		private static void OnPointerCanceled(object snd, PointerRoutedEventArgs e)
			=> Window.Current.DragDrop.ProcessAborted(e);
	}
}
