#nullable enable

using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Windows.UI.Xaml
{
	public partial class DragUI 
	{
		internal ImageSource? Content { get; private set; }

		internal Point? Anchor { get; private set; }

		public void SetContentFromBitmapImage(BitmapImage bitmapImage)
		{
			Content = bitmapImage;
		}

		public void SetContentFromBitmapImage(BitmapImage bitmapImage, Point anchorPoint)
		{
			Content = bitmapImage;
			Anchor = anchorPoint;
		}
	}
}
