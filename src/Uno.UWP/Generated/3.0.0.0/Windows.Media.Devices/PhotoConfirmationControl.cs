#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PhotoConfirmationControl 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.MediaPixelFormat PixelFormat
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPixelFormat PhotoConfirmationControl.PixelFormat is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=MediaPixelFormat%20PhotoConfirmationControl.PixelFormat");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.PhotoConfirmationControl", "MediaPixelFormat PhotoConfirmationControl.PixelFormat");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Enabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PhotoConfirmationControl.Enabled is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20PhotoConfirmationControl.Enabled");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.PhotoConfirmationControl", "bool PhotoConfirmationControl.Enabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Supported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PhotoConfirmationControl.Supported is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20PhotoConfirmationControl.Supported");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.PhotoConfirmationControl.Supported.get
		// Forced skipping of method Windows.Media.Devices.PhotoConfirmationControl.Enabled.get
		// Forced skipping of method Windows.Media.Devices.PhotoConfirmationControl.Enabled.set
		// Forced skipping of method Windows.Media.Devices.PhotoConfirmationControl.PixelFormat.get
		// Forced skipping of method Windows.Media.Devices.PhotoConfirmationControl.PixelFormat.set
	}
}
