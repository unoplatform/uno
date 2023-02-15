#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LineDisplayStoredBitmap 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string EscapeSequence
		{
			get
			{
				throw new global::System.NotImplementedException("The member string LineDisplayStoredBitmap.EscapeSequence is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20LineDisplayStoredBitmap.EscapeSequence");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.LineDisplayStoredBitmap.EscapeSequence.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryDeleteAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> LineDisplayStoredBitmap.TryDeleteAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20LineDisplayStoredBitmap.TryDeleteAsync%28%29");
		}
		#endif
	}
}
