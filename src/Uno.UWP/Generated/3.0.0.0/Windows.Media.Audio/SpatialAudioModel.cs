#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SpatialAudioModel 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ObjectBased,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FoldDown,
		#endif
	}
	#endif
}
