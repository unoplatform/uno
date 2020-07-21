#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Display
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DisplayInformation 
	{
		// Skipping already declared property CurrentOrientation
		// Skipping already declared property LogicalDpi
		// Skipping already declared property NativeOrientation
		// Skipping already declared property RawDpiX
		// Skipping already declared property RawDpiY
		// Skipping already declared property ResolutionScale
		// Skipping already declared property StereoEnabled
		// Skipping already declared property RawPixelsPerViewPixel
		// Skipping already declared property DiagonalSizeInInches
		// Skipping already declared property ScreenHeightInRawPixels
		// Skipping already declared property ScreenWidthInRawPixels
		// Skipping already declared property AutoRotationPreferences
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.CurrentOrientation.get
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.NativeOrientation.get
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.OrientationChanged.add
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.OrientationChanged.remove
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.ResolutionScale.get
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.LogicalDpi.get
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.RawDpiX.get
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.RawDpiY.get
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.DpiChanged.add
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.DpiChanged.remove
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.StereoEnabled.get
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.StereoEnabledChanged.add
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.StereoEnabledChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStream> GetColorProfileAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IRandomAccessStream> DisplayInformation.GetColorProfileAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.ColorProfileChanged.add
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.ColorProfileChanged.remove
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.RawPixelsPerViewPixel.get
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.DiagonalSizeInInches.get
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.ScreenWidthInRawPixels.get
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.ScreenHeightInRawPixels.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Display.AdvancedColorInfo GetAdvancedColorInfo()
		{
			throw new global::System.NotImplementedException("The member AdvancedColorInfo DisplayInformation.GetAdvancedColorInfo() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.AdvancedColorInfoChanged.add
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.AdvancedColorInfoChanged.remove
		// Skipping already declared method Windows.Graphics.Display.DisplayInformation.GetForCurrentView()
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.AutoRotationPreferences.get
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.AutoRotationPreferences.set
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.DisplayContentsInvalidated.add
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.DisplayContentsInvalidated.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Display.DisplayInformation, object> ColorProfileChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayInformation", "event TypedEventHandler<DisplayInformation, object> DisplayInformation.ColorProfileChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayInformation", "event TypedEventHandler<DisplayInformation, object> DisplayInformation.ColorProfileChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Display.DisplayInformation, object> DpiChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayInformation", "event TypedEventHandler<DisplayInformation, object> DisplayInformation.DpiChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayInformation", "event TypedEventHandler<DisplayInformation, object> DisplayInformation.DpiChanged");
			}
		}
		#endif
		// Skipping already declared event Windows.Graphics.Display.DisplayInformation.OrientationChanged
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Display.DisplayInformation, object> StereoEnabledChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayInformation", "event TypedEventHandler<DisplayInformation, object> DisplayInformation.StereoEnabledChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayInformation", "event TypedEventHandler<DisplayInformation, object> DisplayInformation.StereoEnabledChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Display.DisplayInformation, object> AdvancedColorInfoChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayInformation", "event TypedEventHandler<DisplayInformation, object> DisplayInformation.AdvancedColorInfoChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayInformation", "event TypedEventHandler<DisplayInformation, object> DisplayInformation.AdvancedColorInfoChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Display.DisplayInformation, object> DisplayContentsInvalidated
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayInformation", "event TypedEventHandler<DisplayInformation, object> DisplayInformation.DisplayContentsInvalidated");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayInformation", "event TypedEventHandler<DisplayInformation, object> DisplayInformation.DisplayContentsInvalidated");
			}
		}
		#endif
	}
}
