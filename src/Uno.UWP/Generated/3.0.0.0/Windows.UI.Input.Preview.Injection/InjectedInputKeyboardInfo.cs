#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Preview.Injection
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InjectedInputKeyboardInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort VirtualKey
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort InjectedInputKeyboardInfo.VirtualKey is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Preview.Injection.InjectedInputKeyboardInfo", "ushort InjectedInputKeyboardInfo.VirtualKey");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort ScanCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort InjectedInputKeyboardInfo.ScanCode is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Preview.Injection.InjectedInputKeyboardInfo", "ushort InjectedInputKeyboardInfo.ScanCode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Preview.Injection.InjectedInputKeyOptions KeyOptions
		{
			get
			{
				throw new global::System.NotImplementedException("The member InjectedInputKeyOptions InjectedInputKeyboardInfo.KeyOptions is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Preview.Injection.InjectedInputKeyboardInfo", "InjectedInputKeyOptions InjectedInputKeyboardInfo.KeyOptions");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public InjectedInputKeyboardInfo() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Preview.Injection.InjectedInputKeyboardInfo", "InjectedInputKeyboardInfo.InjectedInputKeyboardInfo()");
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Preview.Injection.InjectedInputKeyboardInfo.InjectedInputKeyboardInfo()
		// Forced skipping of method Windows.UI.Input.Preview.Injection.InjectedInputKeyboardInfo.KeyOptions.get
		// Forced skipping of method Windows.UI.Input.Preview.Injection.InjectedInputKeyboardInfo.KeyOptions.set
		// Forced skipping of method Windows.UI.Input.Preview.Injection.InjectedInputKeyboardInfo.ScanCode.get
		// Forced skipping of method Windows.UI.Input.Preview.Injection.InjectedInputKeyboardInfo.ScanCode.set
		// Forced skipping of method Windows.UI.Input.Preview.Injection.InjectedInputKeyboardInfo.VirtualKey.get
		// Forced skipping of method Windows.UI.Input.Preview.Injection.InjectedInputKeyboardInfo.VirtualKey.set
	}
}
