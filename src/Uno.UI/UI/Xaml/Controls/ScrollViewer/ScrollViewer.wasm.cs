using System.Collections.Generic;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	partial class ScrollViewer
	{
		internal Size ScrollBarSize => (_sv as ScrollContentPresenter)?.ScrollBarSize ?? default;

		private void UpdateZoomedContentAlignment()
		{
		}
	}
}
