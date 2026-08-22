using System;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls
{
	partial class ScrollViewer
	{
		protected override void OnPointerEntered(PointerRoutedEventArgs args)
		{
		}

		protected override void OnPointerMoved(PointerRoutedEventArgs args)
		{
		}

		protected override void OnPointerExited(PointerRoutedEventArgs args)
		{
		}

		protected override void OnGotFocus(RoutedEventArgs args)
		{
		}

		protected override void OnBringIntoViewRequested(BringIntoViewRequestedEventArgs args)
		{
		}

		private partial void OnLoadedPartial() { }

		private partial void OnUnloadedPartial() { }

		private bool ChangeViewNative(double? horizontalOffset, double? verticalOffset, float? zoomFactor, bool disableAnimation)
		{
			throw new NotImplementedException();
		}
	}
}
