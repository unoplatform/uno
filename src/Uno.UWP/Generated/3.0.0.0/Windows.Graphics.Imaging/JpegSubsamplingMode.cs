#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Imaging
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum JpegSubsamplingMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Default,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Y4Cb2Cr0,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Y4Cb2Cr2,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Y4Cb4Cr4,
		#endif
	}
	#endif
}
