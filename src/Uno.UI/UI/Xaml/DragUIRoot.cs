#nullable enable

using System;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml
{
	internal class DragUIRoot : Control
	{
		private readonly DragDropManager _manager;

		public DragUIRoot(DragDropManager manager)
		{
			_manager = manager;
		}

		public int PendingDragCount => 0;

		public void Show(DragView view)
		{
		}

		public void Hide(DragView view)
		{
		}

		/// <inheritdoc />
		protected override void OnPointerEntered(PointerRoutedEventArgs e)
		{
			_manager.ProcessPointerEnter(e);
			e.Handled = true;
			base.OnPointerEntered(e);
		}

		/// <inheritdoc />
		protected override void OnPointerExited(PointerRoutedEventArgs e)
		{
			_manager.ProcessPointerExited(e);
			e.Handled = true;
			base.OnPointerExited(e);
		}

		/// <inheritdoc />
		protected override void OnPointerMoved(PointerRoutedEventArgs e)
		{
			_manager.ProcessPointerMoved(e);
			e.Handled = true;
			base.OnPointerMoved(e);
		}

		/// <inheritdoc />
		protected override void OnPointerReleased(PointerRoutedEventArgs e)
		{
			_manager.ProcessPointerReleased(e);
			e.Handled = true;
			base.OnPointerReleased(e);
		}

		/// <inheritdoc />
		protected override void OnPointerCanceled(PointerRoutedEventArgs e)
		{
			_manager.ProcessPointerCanceled(e);
			e.Handled = true;
			base.OnPointerCanceled(e);
		}
	}
}
