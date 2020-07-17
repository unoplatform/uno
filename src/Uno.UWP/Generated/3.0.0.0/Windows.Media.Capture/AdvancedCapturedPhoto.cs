#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AdvancedCapturedPhoto 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  object Context
		{
			get
			{
				throw new global::System.NotImplementedException("The member object AdvancedCapturedPhoto.Context is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Capture.CapturedFrame Frame
		{
			get
			{
				throw new global::System.NotImplementedException("The member CapturedFrame AdvancedCapturedPhoto.Frame is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.AdvancedPhotoMode Mode
		{
			get
			{
				throw new global::System.NotImplementedException("The member AdvancedPhotoMode AdvancedCapturedPhoto.Mode is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Rect? FrameBoundsRelativeToReferencePhoto
		{
			get
			{
				throw new global::System.NotImplementedException("The member Rect? AdvancedCapturedPhoto.FrameBoundsRelativeToReferencePhoto is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.AdvancedCapturedPhoto.Frame.get
		// Forced skipping of method Windows.Media.Capture.AdvancedCapturedPhoto.Mode.get
		// Forced skipping of method Windows.Media.Capture.AdvancedCapturedPhoto.Context.get
		// Forced skipping of method Windows.Media.Capture.AdvancedCapturedPhoto.FrameBoundsRelativeToReferencePhoto.get
	}
}
