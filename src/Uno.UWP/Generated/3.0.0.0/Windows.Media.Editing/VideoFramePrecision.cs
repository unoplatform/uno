#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Editing
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum VideoFramePrecision 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NearestFrame,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NearestKeyFrame,
		#endif
	}
	#endif
}
