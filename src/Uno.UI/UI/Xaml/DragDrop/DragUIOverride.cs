#nullable enable

using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Windows.UI.Xaml
{
	public partial class DragUIOverride 
	{
		private readonly CoreDragUIOverride _core;

		internal DragUIOverride(CoreDragUIOverride core)
		{
			_core = core;
		}

		public bool IsGlyphVisible { get; set; }
		public bool IsContentVisible { get; set; }
		public bool IsCaptionVisible { get; set; }
		public string Caption { get; set; } = string.Empty;

		internal ImageSource? Content { get; set; }
		internal Point ContentAnchor { get; set; }

		public void SetContentFromBitmapImage(BitmapImage bitmapImage)
		{
			Content = bitmapImage;
		}

		public void SetContentFromBitmapImage(BitmapImage bitmapImage, Point anchorPoint)
		{
			Content = bitmapImage;
			ContentAnchor = anchorPoint;
		}

		public void Clear()
		{
			IsGlyphVisible = _core.IsGlyphVisible;
			IsContentVisible = _core.IsContentVisible;
			IsCaptionVisible = _core.IsCaptionVisible;
			Caption = _core.Caption;
		}
	}
}
