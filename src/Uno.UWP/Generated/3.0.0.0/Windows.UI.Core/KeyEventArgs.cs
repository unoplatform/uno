#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class KeyEventArgs : global::Windows.UI.Core.ICoreWindowEventArgs
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool KeyEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.KeyEventArgs", "bool KeyEventArgs.Handled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Core.CorePhysicalKeyStatus KeyStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member CorePhysicalKeyStatus KeyEventArgs.KeyStatus is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.VirtualKey VirtualKey
		{
			get
			{
				throw new global::System.NotImplementedException("The member VirtualKey KeyEventArgs.VirtualKey is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string KeyEventArgs.DeviceId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Core.KeyEventArgs.VirtualKey.get
		// Forced skipping of method Windows.UI.Core.KeyEventArgs.KeyStatus.get
		// Forced skipping of method Windows.UI.Core.KeyEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Core.KeyEventArgs.Handled.set
		// Forced skipping of method Windows.UI.Core.KeyEventArgs.DeviceId.get
		// Processing: Windows.UI.Core.ICoreWindowEventArgs
	}
}
