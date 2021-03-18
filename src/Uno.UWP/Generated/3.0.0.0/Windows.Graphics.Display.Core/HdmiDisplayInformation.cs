#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Display.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HdmiDisplayInformation 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Graphics.Display.Core.HdmiDisplayMode> GetSupportedDisplayModes()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<HdmiDisplayMode> HdmiDisplayInformation.GetSupportedDisplayModes() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Display.Core.HdmiDisplayMode GetCurrentDisplayMode()
		{
			throw new global::System.NotImplementedException("The member HdmiDisplayMode HdmiDisplayInformation.GetCurrentDisplayMode() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SetDefaultDisplayModeAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction HdmiDisplayInformation.SetDefaultDisplayModeAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> RequestSetCurrentDisplayModeAsync( global::Windows.Graphics.Display.Core.HdmiDisplayMode mode)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> HdmiDisplayInformation.RequestSetCurrentDisplayModeAsync(HdmiDisplayMode mode) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> RequestSetCurrentDisplayModeAsync( global::Windows.Graphics.Display.Core.HdmiDisplayMode mode,  global::Windows.Graphics.Display.Core.HdmiDisplayHdrOption hdrOption)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> HdmiDisplayInformation.RequestSetCurrentDisplayModeAsync(HdmiDisplayMode mode, HdmiDisplayHdrOption hdrOption) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> RequestSetCurrentDisplayModeAsync( global::Windows.Graphics.Display.Core.HdmiDisplayMode mode,  global::Windows.Graphics.Display.Core.HdmiDisplayHdrOption hdrOption,  global::Windows.Graphics.Display.Core.HdmiDisplayHdr2086Metadata hdrMetadata)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> HdmiDisplayInformation.RequestSetCurrentDisplayModeAsync(HdmiDisplayMode mode, HdmiDisplayHdrOption hdrOption, HdmiDisplayHdr2086Metadata hdrMetadata) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Display.Core.HdmiDisplayInformation.DisplayModesChanged.add
		// Forced skipping of method Windows.Graphics.Display.Core.HdmiDisplayInformation.DisplayModesChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Graphics.Display.Core.HdmiDisplayInformation GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member HdmiDisplayInformation HdmiDisplayInformation.GetForCurrentView() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Display.Core.HdmiDisplayInformation, object> DisplayModesChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.Core.HdmiDisplayInformation", "event TypedEventHandler<HdmiDisplayInformation, object> HdmiDisplayInformation.DisplayModesChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.Core.HdmiDisplayInformation", "event TypedEventHandler<HdmiDisplayInformation, object> HdmiDisplayInformation.DisplayModesChanged");
			}
		}
		#endif
	}
}
