#nullable enable

using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml
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
