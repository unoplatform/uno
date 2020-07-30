#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MouseWheelParameters 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Point PageTranslation
		{
			get
			{
				throw new global::System.NotImplementedException("The member Point MouseWheelParameters.PageTranslation is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.MouseWheelParameters", "Point MouseWheelParameters.PageTranslation");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float DeltaScale
		{
			get
			{
				throw new global::System.NotImplementedException("The member float MouseWheelParameters.DeltaScale is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.MouseWheelParameters", "float MouseWheelParameters.DeltaScale");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float DeltaRotationAngle
		{
			get
			{
				throw new global::System.NotImplementedException("The member float MouseWheelParameters.DeltaRotationAngle is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.MouseWheelParameters", "float MouseWheelParameters.DeltaRotationAngle");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Point CharTranslation
		{
			get
			{
				throw new global::System.NotImplementedException("The member Point MouseWheelParameters.CharTranslation is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.MouseWheelParameters", "Point MouseWheelParameters.CharTranslation");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.MouseWheelParameters.CharTranslation.get
		// Forced skipping of method Windows.UI.Input.MouseWheelParameters.CharTranslation.set
		// Forced skipping of method Windows.UI.Input.MouseWheelParameters.DeltaScale.get
		// Forced skipping of method Windows.UI.Input.MouseWheelParameters.DeltaScale.set
		// Forced skipping of method Windows.UI.Input.MouseWheelParameters.DeltaRotationAngle.get
		// Forced skipping of method Windows.UI.Input.MouseWheelParameters.DeltaRotationAngle.set
		// Forced skipping of method Windows.UI.Input.MouseWheelParameters.PageTranslation.get
		// Forced skipping of method Windows.UI.Input.MouseWheelParameters.PageTranslation.set
	}
}
