#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SystemFunctionLockChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SystemFunctionLockChangedEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.SystemFunctionLockChangedEventArgs", "bool SystemFunctionLockChangedEventArgs.Handled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsLocked
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SystemFunctionLockChangedEventArgs.IsLocked is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong SystemFunctionLockChangedEventArgs.Timestamp is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.SystemFunctionLockChangedEventArgs.Timestamp.get
		// Forced skipping of method Windows.UI.Input.SystemFunctionLockChangedEventArgs.IsLocked.get
		// Forced skipping of method Windows.UI.Input.SystemFunctionLockChangedEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Input.SystemFunctionLockChangedEventArgs.Handled.set
	}
}
