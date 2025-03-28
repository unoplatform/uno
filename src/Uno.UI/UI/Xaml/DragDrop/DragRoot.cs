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
		public DragRoot()
		{
			VerticalAlignment = VerticalAlignment.Stretch;
			HorizontalAlignment = HorizontalAlignment.Stretch;
			Background = new SolidColorBrush(Colors.Transparent);
			IsHitTestVisible = false;
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
	}
}
