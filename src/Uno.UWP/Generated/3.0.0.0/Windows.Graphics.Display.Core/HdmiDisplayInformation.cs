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
			throw new global::System.NotImplementedException("The member IReadOnlyList<HdmiDisplayMode> HdmiDisplayInformation.GetSupportedDisplayModes() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CHdmiDisplayMode%3E%20HdmiDisplayInformation.GetSupportedDisplayModes%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Display.Core.HdmiDisplayMode GetCurrentDisplayMode()
		{
			throw new global::System.NotImplementedException("The member HdmiDisplayMode HdmiDisplayInformation.GetCurrentDisplayMode() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=HdmiDisplayMode%20HdmiDisplayInformation.GetCurrentDisplayMode%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SetDefaultDisplayModeAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction HdmiDisplayInformation.SetDefaultDisplayModeAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20HdmiDisplayInformation.SetDefaultDisplayModeAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> RequestSetCurrentDisplayModeAsync( global::Windows.Graphics.Display.Core.HdmiDisplayMode mode)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> HdmiDisplayInformation.RequestSetCurrentDisplayModeAsync(HdmiDisplayMode mode) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20HdmiDisplayInformation.RequestSetCurrentDisplayModeAsync%28HdmiDisplayMode%20mode%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> RequestSetCurrentDisplayModeAsync( global::Windows.Graphics.Display.Core.HdmiDisplayMode mode,  global::Windows.Graphics.Display.Core.HdmiDisplayHdrOption hdrOption)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> HdmiDisplayInformation.RequestSetCurrentDisplayModeAsync(HdmiDisplayMode mode, HdmiDisplayHdrOption hdrOption) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20HdmiDisplayInformation.RequestSetCurrentDisplayModeAsync%28HdmiDisplayMode%20mode%2C%20HdmiDisplayHdrOption%20hdrOption%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> RequestSetCurrentDisplayModeAsync( global::Windows.Graphics.Display.Core.HdmiDisplayMode mode,  global::Windows.Graphics.Display.Core.HdmiDisplayHdrOption hdrOption,  global::Windows.Graphics.Display.Core.HdmiDisplayHdr2086Metadata hdrMetadata)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> HdmiDisplayInformation.RequestSetCurrentDisplayModeAsync(HdmiDisplayMode mode, HdmiDisplayHdrOption hdrOption, HdmiDisplayHdr2086Metadata hdrMetadata) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20HdmiDisplayInformation.RequestSetCurrentDisplayModeAsync%28HdmiDisplayMode%20mode%2C%20HdmiDisplayHdrOption%20hdrOption%2C%20HdmiDisplayHdr2086Metadata%20hdrMetadata%29");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Display.Core.HdmiDisplayInformation.DisplayModesChanged.add
		// Forced skipping of method Windows.Graphics.Display.Core.HdmiDisplayInformation.DisplayModesChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Graphics.Display.Core.HdmiDisplayInformation GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member HdmiDisplayInformation HdmiDisplayInformation.GetForCurrentView() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=HdmiDisplayInformation%20HdmiDisplayInformation.GetForCurrentView%28%29");
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
