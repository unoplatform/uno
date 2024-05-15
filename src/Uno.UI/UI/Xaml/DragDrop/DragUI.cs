#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Microsoft.UI.Xaml
{
	public partial class DragUI
	{
		internal ImageSource? Content { get; set; }

		internal Point? Anchor { get; set; }

		public void SetContentFromBitmapImage(BitmapImage bitmapImage)
			=> SetContentFromBitmapImage(bitmapImage, default);

		public void SetContentFromBitmapImage(BitmapImage bitmapImage, Point anchorPoint)
		{
			Content = bitmapImage;
			Anchor = anchorPoint;
		}
	}
}
