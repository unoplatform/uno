#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HdcpSession : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public HdcpSession() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.HdcpSession", "HdcpSession.HdcpSession()");
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.HdcpSession.HdcpSession()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsEffectiveProtectionAtLeast( global::Windows.Media.Protection.HdcpProtection protection)
		{
			throw new global::System.NotImplementedException("The member bool HdcpSession.IsEffectiveProtectionAtLeast(HdcpProtection protection) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20HdcpSession.IsEffectiveProtectionAtLeast%28HdcpProtection%20protection%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Protection.HdcpProtection? GetEffectiveProtection()
		{
			throw new global::System.NotImplementedException("The member HdcpProtection? HdcpSession.GetEffectiveProtection() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=HdcpProtection%3F%20HdcpSession.GetEffectiveProtection%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Protection.HdcpSetProtectionResult> SetDesiredMinProtectionAsync( global::Windows.Media.Protection.HdcpProtection protection)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<HdcpSetProtectionResult> HdcpSession.SetDesiredMinProtectionAsync(HdcpProtection protection) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CHdcpSetProtectionResult%3E%20HdcpSession.SetDesiredMinProtectionAsync%28HdcpProtection%20protection%29");
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.HdcpSession.ProtectionChanged.add
		// Forced skipping of method Windows.Media.Protection.HdcpSession.ProtectionChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.HdcpSession", "void HdcpSession.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Protection.HdcpSession, object> ProtectionChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.HdcpSession", "event TypedEventHandler<HdcpSession, object> HdcpSession.ProtectionChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.HdcpSession", "event TypedEventHandler<HdcpSession, object> HdcpSession.ProtectionChanged");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
