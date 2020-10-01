#nullable enable

using System;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml
{
	internal partial class DragRoot : Canvas
	{
		private readonly DragDropManager _manager;

		public DragRoot(DragDropManager manager)
		{
			_manager = manager;

			VerticalAlignment = VerticalAlignment.Stretch;
			HorizontalAlignment = HorizontalAlignment.Stretch;
			Background = new SolidColorBrush(Colors.Transparent);

			//PointerEntered += OnPointerEntered;
			//PointerExited += OnPointerExited;
			//PointerMoved += OnPointerMoved;
			//PointerReleased += OnPointerReleased;
			//PointerCanceled += OnPointerCanceled;
		}

		public int PendingDragCount => Children.Count;

		public void Show(DragView view)
		{
			view.IsHitTestVisible = false;
			Children.Add(view);
		}

		public void Hide(DragView view)
		{
			Children.Remove(view);
		}

		private static void OnPointerEntered(object snd, PointerRoutedEventArgs e)
		{
			((DragRoot)snd)._manager.ProcessPointerEnteredWindow(e);
			e.Handled = true;
		}

		private static void OnPointerExited(object snd, PointerRoutedEventArgs e)
		{
			((DragRoot)snd)._manager.ProcessPointerExitedWindow(e);
			e.Handled = true;
		}

		private static void OnPointerMoved(object snd, PointerRoutedEventArgs e)
		{
			((DragRoot)snd)._manager.ProcessPointerMovedOverWindow(e);
			e.Handled = true;
		}

		private static void OnPointerReleased(object snd, PointerRoutedEventArgs e)
		{
			((DragRoot)snd)._manager.ProcessPointerReleased(e);
			e.Handled = true;
		}

		private static void OnPointerCanceled(object snd, PointerRoutedEventArgs e)
		{
			((DragRoot)snd)._manager.ProcessPointerCanceled(e);
			e.Handled = true;
		}
	}
}
