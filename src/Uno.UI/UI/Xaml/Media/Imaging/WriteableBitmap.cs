using Windows.Foundation;
using Windows.Storage.Streams;

namespace Windows.UI.Xaml.Media.Imaging
{
	public partial class WriteableBitmap : global::Windows.UI.Xaml.Media.Imaging.BitmapSource
	{
		public global::Windows.Storage.Streams.IBuffer PixelBuffer { get; private set; }

		public WriteableBitmap(int pixelWidth, int pixelHeight) : base()
		{
			PixelBuffer = new InMemoryBuffer(pixelWidth * pixelHeight * 4);

			PixelWidth = pixelWidth;
			PixelHeight = pixelHeight;
		}

		public void Invalidate()
		{

		}
	}
}
