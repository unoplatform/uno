#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppUriHandlerRegistration 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Name
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppUriHandlerRegistration.Name is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20AppUriHandlerRegistration.Name");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User AppUriHandlerRegistration.User is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=User%20AppUriHandlerRegistration.User");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string PackageFamilyName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppUriHandlerRegistration.PackageFamilyName is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20AppUriHandlerRegistration.PackageFamilyName");
			}
		}
		#endif
		// Forced skipping of method Windows.System.AppUriHandlerRegistration.Name.get
		// Forced skipping of method Windows.System.AppUriHandlerRegistration.User.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IList<global::Windows.System.AppUriHandlerHost>> GetAppAddedHostsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IList<AppUriHandlerHost>> AppUriHandlerRegistration.GetAppAddedHostsAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CIList%3CAppUriHandlerHost%3E%3E%20AppUriHandlerRegistration.GetAppAddedHostsAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SetAppAddedHostsAsync( global::System.Collections.Generic.IEnumerable<global::Windows.System.AppUriHandlerHost> hosts)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction AppUriHandlerRegistration.SetAppAddedHostsAsync(IEnumerable<AppUriHandlerHost> hosts) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20AppUriHandlerRegistration.SetAppAddedHostsAsync%28IEnumerable%3CAppUriHandlerHost%3E%20hosts%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.System.AppUriHandlerHost> GetAllHosts()
		{
			throw new global::System.NotImplementedException("The member IList<AppUriHandlerHost> AppUriHandlerRegistration.GetAllHosts() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IList%3CAppUriHandlerHost%3E%20AppUriHandlerRegistration.GetAllHosts%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void UpdateHosts( global::System.Collections.Generic.IEnumerable<global::Windows.System.AppUriHandlerHost> hosts)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.AppUriHandlerRegistration", "void AppUriHandlerRegistration.UpdateHosts(IEnumerable<AppUriHandlerHost> hosts)");
		}
		#endif
		// Forced skipping of method Windows.System.AppUriHandlerRegistration.PackageFamilyName.get
	}
}
