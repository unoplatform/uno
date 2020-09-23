#nullable enable

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	public  partial class CoreDragUIOverride 
	{
		public bool IsGlyphVisible { get; set; }
		public bool IsContentVisible { get; set; }
		public bool IsCaptionVisible { get; set; }
		public string Caption { get; set; } = string.Empty;

		public void Clear()
		{
		}
	}
}
