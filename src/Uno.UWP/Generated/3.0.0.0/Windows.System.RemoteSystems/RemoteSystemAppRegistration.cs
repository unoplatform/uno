#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.RemoteSystems
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RemoteSystemAppRegistration 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IDictionary<string, string> Attributes
		{
			get
			{
				throw new global::System.NotImplementedException("The member IDictionary<string, string> RemoteSystemAppRegistration.Attributes is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IDictionary%3Cstring%2C%20string%3E%20RemoteSystemAppRegistration.Attributes");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User RemoteSystemAppRegistration.User is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=User%20RemoteSystemAppRegistration.User");
			}
		}
		#endif
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemAppRegistration.User.get
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemAppRegistration.Attributes.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> SaveAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> RemoteSystemAppRegistration.SaveAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20RemoteSystemAppRegistration.SaveAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.RemoteSystems.RemoteSystemAppRegistration GetDefault()
		{
			throw new global::System.NotImplementedException("The member RemoteSystemAppRegistration RemoteSystemAppRegistration.GetDefault() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=RemoteSystemAppRegistration%20RemoteSystemAppRegistration.GetDefault%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.RemoteSystems.RemoteSystemAppRegistration GetForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member RemoteSystemAppRegistration RemoteSystemAppRegistration.GetForUser(User user) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=RemoteSystemAppRegistration%20RemoteSystemAppRegistration.GetForUser%28User%20user%29");
		}
		#endif
	}
}
