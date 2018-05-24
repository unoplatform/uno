#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial struct RectInt32 
	{
		// Forced skipping of method Windows.Graphics.RectInt32.RectInt32()
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		public  int X;
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		public  int Y;
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		public  int Width;
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		public  int Height;
		#endif
	}
}
