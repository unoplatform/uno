#nullable enable

using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Input;
using Uno.Foundation.Extensibility;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// Don't interact with this class directly. Instead, use <see cref="CoreDragDropManager"/>, which has passthrough
	/// methods to this class. For more details, see the comments in <see cref="CoreDragDropManager.IDragDropManager"/>
	/// </summary>
	internal sealed class DragDropManager : CoreDragDropManager.IDragDropManager
	{
		private readonly InputManager _inputManager;
		private readonly List<DragOperation> _dragOperations = new List<DragOperation>();
		private readonly IDragDropExtension? _hostExtension;

		private bool _areWindowEventsRegistered;

		public DragDropManager(InputManager inputManager)
		{
			_inputManager = inputManager;

			if (ApiExtensibility.CreateInstance<IDragDropExtension>(this, out var extension))
			{
				_hostExtension = extension;
			}
		}

		internal ContentRoot ContentRoot => _inputManager.ContentRoot;

		/// <inheritdoc />
		public bool AreConcurrentOperationsEnabled { get; set; }

		/// <inheritdoc />
		public void BeginDragAndDrop(CoreDragInfo info, ICoreDropOperationTarget? target = null)
		{
			if (_inputManager.ContentRoot.Dispatcher is { } dispatcher &&
				!dispatcher.HasThreadAccess)
			{
				_ = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BeginDragAndDrop(info, target));
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

			var op = new DragOperation(_inputManager, _hostExtension, info, target);

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
		public DataPackageOperation ProcessReleased(IDragEventSource src)
			=> FindOperation(src)?.Released(src) ?? DataPackageOperation.None;

		/// <summary>
		/// This method is expected to be invoked when pointer involved in a drag operation
		/// is lost for operation initiated by the current app,
		/// or left the window (a.k.a. the "virtual pointer" is lost) for operation initiated by an other app.
		/// </summary>
		/// <returns>
		/// The last accepted operation.
		/// Be aware that due to the async processing of dragging in UWP, this might not be the up to date.
		/// </returns>
		public DataPackageOperation ProcessAborted(long pointerId)
			=> FindOperation(pointerId)?.Aborted() ?? DataPackageOperation.None;

		public DragOperation? FindOperation(IDragEventSource src)
			=> _dragOperations.FirstOrDefault(drag => drag.Info.SourceId == src.Id);

		private DragOperation? FindOperation(long pointerId)
			=> _dragOperations.FirstOrDefault(drag => drag.Info.SourceId == pointerId);

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

			var root = _inputManager.ContentRoot.VisualTree.RootElement;
			root.AddHandler(UIElement.PointerEnteredEvent, new PointerEventHandler(OnPointerMoved), handledEventsToo: true);
			root.AddHandler(UIElement.PointerExitedEvent, new PointerEventHandler(OnPointerMoved), handledEventsToo: true);
			root.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(OnPointerMoved), handledEventsToo: true);
			root.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(OnPointerReleased), handledEventsToo: true);
			root.AddHandler(UIElement.PointerCanceledEvent, new PointerEventHandler(OnPointerCanceled), handledEventsToo: true);

			_areWindowEventsRegistered = true;
		}

		private void OnPointerMoved(object snd, PointerRoutedEventArgs e) => ProcessMoved(e);

		private void OnPointerReleased(object snd, PointerRoutedEventArgs e) => ProcessReleased(e);

		private void OnPointerCanceled(object snd, PointerRoutedEventArgs e) => ProcessAborted(e.Pointer.PointerId);
	}
}
