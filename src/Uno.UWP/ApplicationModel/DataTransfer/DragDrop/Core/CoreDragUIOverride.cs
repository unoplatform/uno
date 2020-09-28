using Windows.Foundation;

#nullable enable

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	public  partial class CoreDragUIOverride
	{
		public bool IsGlyphVisible { get; set; }
		public bool IsContentVisible { get; set; }
		public bool IsCaptionVisible { get; set; }
		public string Caption { get; set; } = string.Empty;

		/// <summary>
		/// A Windows.UI.Xaml.Media.ImageSource to override the default Content of the DragUI
		/// </summary>
		internal object? Content { get; set; }
		internal Point ContentAnchor { get; set; }

		public CoreDragUIOverride()
		{
			Clear();
		}

		public void Clear()
		{
			IsGlyphVisible = true;
			IsContentVisible = true;
			IsCaptionVisible = true;
			Caption = string.Empty;
		}
	}
}
