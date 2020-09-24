#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.UI.Core;
using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml
{
	internal class DragDropManager : CoreDragDropManager.IDragDropManager
	{
		private readonly Window _window;
		private readonly List<DragOperation> _dragOperations = new List<DragOperation>();

		public DragDropManager(Window window)
		{
			_window = window;
		}

		/// <inheritdoc />
		public void BeginDragAndDrop(CoreDragInfo info, ICoreDropOperationTarget? target = null)
		{
			if (!_window.Dispatcher.HasThreadAccess)
			{
				_window.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BeginDragAndDrop(info, target));
				return;
			}

			var op = new DragOperation(_window, info, target);

			info.RegisterCompletedCallback(_ => _dragOperations.Remove(op));
			_dragOperations.Add(op);
		}

		private DragOperation? FindOperation(PointerRoutedEventArgs args)
		{
			var pointer = args.Pointer!;
			var op = _dragOperations.FirstOrDefault(drag => pointer.Equals(drag.Pointer))
				?? _dragOperations.FirstOrDefault(drag => drag.Pointer == null);

			return op;
		}

		public void ProcessPointerEnter(PointerRoutedEventArgs args)
			=> FindOperation(args)?.Entered(args);

		public void ProcessPointerExited(PointerRoutedEventArgs args)
			=> FindOperation(args)?.Exited(args);

		public void ProcessPointerMoved(PointerRoutedEventArgs args)
			=> FindOperation(args)?.Moved(args);

		public void ProcessPointerReleased(PointerRoutedEventArgs args)
			=> FindOperation(args)?.Dropped(args);

		public void ProcessPointerCanceled(PointerRoutedEventArgs args)
			=> FindOperation(args)?.Aborted(args);
	}
}
