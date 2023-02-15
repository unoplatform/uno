#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Threading.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SignalNotifier 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Enable()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Threading.Core.SignalNotifier", "void SignalNotifier.Enable()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Terminate()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Threading.Core.SignalNotifier", "void SignalNotifier.Terminate()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.Threading.Core.SignalNotifier AttachToEvent( string name,  global::Windows.System.Threading.Core.SignalHandler handler)
		{
			throw new global::System.NotImplementedException("The member SignalNotifier SignalNotifier.AttachToEvent(string name, SignalHandler handler) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SignalNotifier%20SignalNotifier.AttachToEvent%28string%20name%2C%20SignalHandler%20handler%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.Threading.Core.SignalNotifier AttachToEvent( string name,  global::Windows.System.Threading.Core.SignalHandler handler,  global::System.TimeSpan timeout)
		{
			throw new global::System.NotImplementedException("The member SignalNotifier SignalNotifier.AttachToEvent(string name, SignalHandler handler, TimeSpan timeout) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SignalNotifier%20SignalNotifier.AttachToEvent%28string%20name%2C%20SignalHandler%20handler%2C%20TimeSpan%20timeout%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.Threading.Core.SignalNotifier AttachToSemaphore( string name,  global::Windows.System.Threading.Core.SignalHandler handler)
		{
			throw new global::System.NotImplementedException("The member SignalNotifier SignalNotifier.AttachToSemaphore(string name, SignalHandler handler) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SignalNotifier%20SignalNotifier.AttachToSemaphore%28string%20name%2C%20SignalHandler%20handler%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.Threading.Core.SignalNotifier AttachToSemaphore( string name,  global::Windows.System.Threading.Core.SignalHandler handler,  global::System.TimeSpan timeout)
		{
			throw new global::System.NotImplementedException("The member SignalNotifier SignalNotifier.AttachToSemaphore(string name, SignalHandler handler, TimeSpan timeout) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SignalNotifier%20SignalNotifier.AttachToSemaphore%28string%20name%2C%20SignalHandler%20handler%2C%20TimeSpan%20timeout%29");
		}
		#endif
	}
}
