#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial struct PackageVersion 
	{
		// Forced skipping of method Windows.ApplicationModel.PackageVersion.PackageVersion()
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		public  ushort Major;
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		public  ushort Minor;
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		public  ushort Build;
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		public  ushort Revision;
		#endif
	}
}
