#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial struct PointInt32 
	{
		// Forced skipping of method Windows.Graphics.PointInt32.PointInt32()
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		public  int X;
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		public  int Y;
		#endif
	}
}
