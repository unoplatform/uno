#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InputEnabledEventArgs : global::Windows.UI.Core.ICoreWindowEventArgs
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool InputEnabledEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.InputEnabledEventArgs", "bool InputEnabledEventArgs.Handled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool InputEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool InputEnabledEventArgs.InputEnabled is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Core.InputEnabledEventArgs.InputEnabled.get
		// Forced skipping of method Windows.UI.Core.InputEnabledEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Core.InputEnabledEventArgs.Handled.set
		// Processing: Windows.UI.Core.ICoreWindowEventArgs
	}
}
