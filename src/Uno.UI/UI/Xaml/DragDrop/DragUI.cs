#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml
{
	public partial class DragUI
	{
		public DragUI()
		{
		}

		internal DragUI(PointerDeviceType pointerDeviceType)
		{
			PointerDeviceType = pointerDeviceType;
		}

		internal PointerDeviceType PointerDeviceType { get; }

		internal ImageSource? Content { get; set; }

		internal Point? Anchor { get; set; }

		internal bool IsExternalContent { get; set; }

		public void SetContentFromBitmapImage(BitmapImage bitmapImage)
			=> SetContentFromBitmapImage(bitmapImage, default);

		public void SetContentFromBitmapImage(BitmapImage bitmapImage, Point anchorPoint)
		{
			Content = bitmapImage;
			Anchor = anchorPoint;
		}

		internal void SetContentFromExternalBitmapImage(BitmapImage bitmapImage)
		{
			Content = bitmapImage;
			IsExternalContent = true;
		}
	}
}
