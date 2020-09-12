#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.FaceAnalysis
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DetectedFace 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Imaging.BitmapBounds FaceBox
		{
			get
			{
				throw new global::System.NotImplementedException("The member BitmapBounds DetectedFace.FaceBox is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.FaceAnalysis.DetectedFace.FaceBox.get
	}
}
