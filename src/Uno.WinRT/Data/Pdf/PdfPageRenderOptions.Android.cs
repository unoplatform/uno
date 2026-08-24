using Windows.Foundation;
using Windows.UI;

namespace Windows.Data.Pdf;

public sealed partial class PdfPageRenderOptions
{
	public Rect SourceRect { get; set; }

	public uint DestinationWidth { get; set; }

	public uint DestinationHeight { get; set; }

	public Color BackgroundColor { get; set; } = Colors.White;
}
