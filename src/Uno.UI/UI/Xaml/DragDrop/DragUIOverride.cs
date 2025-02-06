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

		public bool IsGlyphVisible
		{
			get => _core.IsGlyphVisible;
			set => _core.IsGlyphVisible = value;
		}
		public bool IsContentVisible
		{
			get => _core.IsContentVisible;
			set => _core.IsContentVisible = value;
		}
		public bool IsCaptionVisible
		{
			get => _core.IsCaptionVisible;
			set => _core.IsCaptionVisible = value;
		}
		public string Caption
		{
			get => _core.Caption;
			set => _core.Caption = value;
		}

		public void SetContentFromBitmapImage(BitmapImage bitmapImage)
		{
			_core.Content = bitmapImage;
		}

		public void SetContentFromBitmapImage(BitmapImage bitmapImage, Point anchorPoint)
		{
			_core.Content = bitmapImage;
			_core.ContentAnchor = anchorPoint;
		}

		public void Clear()
		{
			_core.Clear();
		}
	}
}
