#nullable enable

using Windows.Foundation;
using Windows.UI.Xaml.Media.Imaging;

namespace Windows.UI.Xaml
{
	public  partial class DragUIOverride 
	{
		public bool IsGlyphVisible { get; set; }

		public bool IsContentVisible { get; set; }

		public bool IsCaptionVisible { get; set; }

		public string Caption { get; set; }

		public void SetContentFromBitmapImage(BitmapImage bitmapImage)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.DragUIOverride", "void DragUIOverride.SetContentFromBitmapImage(BitmapImage bitmapImage)");
		}

		public void SetContentFromBitmapImage(BitmapImage bitmapImage, Point anchorPoint)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.DragUIOverride", "void DragUIOverride.SetContentFromBitmapImage(BitmapImage bitmapImage, Point anchorPoint)");
		}

		public void Clear()
		{
		}
	}
}
