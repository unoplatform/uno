#if __ANDROID__
using Windows.Foundation;

namespace Windows.Data.Pdf
{
	public partial class PdfPageDimensions
	{
		public Rect ArtBox { get; }
		public Rect BleedBox { get; }
		public Rect CropBox { get; }
		public Rect MediaBox { get; }
		public Rect TrimBox { get; }
	}
}
#endif
