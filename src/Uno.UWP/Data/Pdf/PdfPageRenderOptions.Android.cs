#if __ANDROID__
using System;
using Windows.Foundation;
using Windows.UI;

namespace Windows.Data.Pdf
{
	public partial class PdfPageRenderOptions
	{
		public Rect SourceRect { get; set; }

		public bool IsIgnoringHighContrast { get; set; }

		public uint DestinationWidth { get; set; }

		public uint DestinationHeight { get; set; }

		public Guid BitmapEncoderId { get; set; }

		public Color BackgroundColor { get; set; }
	}
}
#endif
