using System;

namespace Windows.UI.Xaml.Controls
{
	partial class ScrollViewer
	{
		private partial void OnLoadedPartial() { }

		private partial void OnUnloadedPartial() { }

		private bool ChangeViewNative(double? horizontalOffset, double? verticalOffset, float? zoomFactor, bool disableAnimation)
		{
			throw new NotImplementedException();
		}
	}
}
