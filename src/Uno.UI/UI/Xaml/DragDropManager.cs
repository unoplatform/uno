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
		public bool AreConcurrentOperationsEnabled { get; set; } = false;

		/// <inheritdoc />
		public void BeginDragAndDrop(CoreDragInfo info, ICoreDropOperationTarget? target = null)
		{
			if (!_window.Dispatcher.HasThreadAccess)
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

			RegisterHandlers();

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

		private bool _registered = false;
		private void RegisterHandlers()
		{
			if (_registered)
			{
				return;
			}

			var root = _window.RootElement;
			root.AddHandler(UIElement.PointerEnteredEvent, new PointerEventHandler(OnPointerEntered), handledEventsToo: true);
			root.AddHandler(UIElement.PointerExitedEvent, new PointerEventHandler(OnPointerExited), handledEventsToo: true);
			root.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(OnPointerMoved), handledEventsToo: true);
			root.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(OnPointerReleased), handledEventsToo: true);
			root.AddHandler(UIElement.PointerCanceledEvent, new PointerEventHandler(OnPointerCanceled), handledEventsToo: true);

			_registered = true;
		}

		private static void OnPointerEntered(object snd, PointerRoutedEventArgs e)
			=> Window.Current.DragDrop.ProcessPointerEnteredWindow(e);

		private static void OnPointerExited(object snd, PointerRoutedEventArgs e)
			=> Window.Current.DragDrop.ProcessPointerExitedWindow(e);

		private static void OnPointerMoved(object snd, PointerRoutedEventArgs e)
			=> Window.Current.DragDrop.ProcessPointerMovedOverWindow(e);

		private static void OnPointerReleased(object snd, PointerRoutedEventArgs e)
			=> Window.Current.DragDrop.ProcessPointerReleased(e);

		private static void OnPointerCanceled(object snd, PointerRoutedEventArgs e)
			=> Window.Current.DragDrop.ProcessPointerCanceled(e);

		public void ProcessPointerEnteredWindow(PointerRoutedEventArgs args)
			=> FindOperation(args)?.Entered(args);

		public void ProcessPointerExitedWindow(PointerRoutedEventArgs args)
			=> FindOperation(args)?.Exited(args);

		public void ProcessPointerMovedOverWindow(PointerRoutedEventArgs args)
			=> FindOperation(args)?.Moved(args);

		public void ProcessPointerReleased(PointerRoutedEventArgs args)
			=> FindOperation(args)?.Dropped(args);

		public void ProcessPointerCanceled(PointerRoutedEventArgs args)
			=> FindOperation(args)?.Aborted(args);
	}
}
