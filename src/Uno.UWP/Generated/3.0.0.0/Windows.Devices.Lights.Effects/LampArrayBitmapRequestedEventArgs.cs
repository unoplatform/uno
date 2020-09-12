#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Lights.Effects
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LampArrayBitmapRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan SinceStarted
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan LampArrayBitmapRequestedEventArgs.SinceStarted is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Lights.Effects.LampArrayBitmapRequestedEventArgs.SinceStarted.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void UpdateBitmap( global::Windows.Graphics.Imaging.SoftwareBitmap bitmap)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Lights.Effects.LampArrayBitmapRequestedEventArgs", "void LampArrayBitmapRequestedEventArgs.UpdateBitmap(SoftwareBitmap bitmap)");
		}
		#endif
	}
}
