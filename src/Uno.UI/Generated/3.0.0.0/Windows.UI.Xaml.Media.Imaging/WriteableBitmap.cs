#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Imaging
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WriteableBitmap : global::Windows.UI.Xaml.Media.Imaging.BitmapSource
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.Streams.IBuffer PixelBuffer
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer WriteableBitmap.PixelBuffer is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public WriteableBitmap( int pixelWidth,  int pixelHeight) : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Imaging.WriteableBitmap", "WriteableBitmap.WriteableBitmap(int pixelWidth, int pixelHeight)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.WriteableBitmap.WriteableBitmap(int, int)
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.WriteableBitmap.PixelBuffer.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void Invalidate()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Imaging.WriteableBitmap", "void WriteableBitmap.Invalidate()");
		}
		#endif
	}
}
