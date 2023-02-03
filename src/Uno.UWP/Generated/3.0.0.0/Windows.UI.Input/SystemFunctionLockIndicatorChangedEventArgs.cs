#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SystemFunctionLockIndicatorChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SystemFunctionLockIndicatorChangedEventArgs.Handled is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20SystemFunctionLockIndicatorChangedEventArgs.Handled");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.SystemFunctionLockIndicatorChangedEventArgs", "bool SystemFunctionLockIndicatorChangedEventArgs.Handled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsIndicatorOn
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SystemFunctionLockIndicatorChangedEventArgs.IsIndicatorOn is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20SystemFunctionLockIndicatorChangedEventArgs.IsIndicatorOn");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong SystemFunctionLockIndicatorChangedEventArgs.Timestamp is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ulong%20SystemFunctionLockIndicatorChangedEventArgs.Timestamp");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.SystemFunctionLockIndicatorChangedEventArgs.Timestamp.get
		// Forced skipping of method Windows.UI.Input.SystemFunctionLockIndicatorChangedEventArgs.IsIndicatorOn.get
		// Forced skipping of method Windows.UI.Input.SystemFunctionLockIndicatorChangedEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Input.SystemFunctionLockIndicatorChangedEventArgs.Handled.set
	}
}
