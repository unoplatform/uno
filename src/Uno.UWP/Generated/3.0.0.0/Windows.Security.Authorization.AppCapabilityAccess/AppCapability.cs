#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authorization.AppCapabilityAccess
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppCapability 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string CapabilityName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppCapability.CapabilityName is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20AppCapability.CapabilityName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User AppCapability.User is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=User%20AppCapability.User");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.Authorization.AppCapabilityAccess.AppCapability.CapabilityName.get
		// Forced skipping of method Windows.Security.Authorization.AppCapabilityAccess.AppCapability.User.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authorization.AppCapabilityAccess.AppCapabilityAccessStatus> RequestAccessAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AppCapabilityAccessStatus> AppCapability.RequestAccessAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CAppCapabilityAccessStatus%3E%20AppCapability.RequestAccessAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authorization.AppCapabilityAccess.AppCapabilityAccessStatus CheckAccess()
		{
			throw new global::System.NotImplementedException("The member AppCapabilityAccessStatus AppCapability.CheckAccess() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AppCapabilityAccessStatus%20AppCapability.CheckAccess%28%29");
		}
		#endif
		// Forced skipping of method Windows.Security.Authorization.AppCapabilityAccess.AppCapability.AccessChanged.add
		// Forced skipping of method Windows.Security.Authorization.AppCapabilityAccess.AppCapability.AccessChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyDictionary<string, global::Windows.Security.Authorization.AppCapabilityAccess.AppCapabilityAccessStatus>> RequestAccessForCapabilitiesAsync( global::System.Collections.Generic.IEnumerable<string> capabilityNames)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyDictionary<string, AppCapabilityAccessStatus>> AppCapability.RequestAccessForCapabilitiesAsync(IEnumerable<string> capabilityNames) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CIReadOnlyDictionary%3Cstring%2C%20AppCapabilityAccessStatus%3E%3E%20AppCapability.RequestAccessForCapabilitiesAsync%28IEnumerable%3Cstring%3E%20capabilityNames%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyDictionary<string, global::Windows.Security.Authorization.AppCapabilityAccess.AppCapabilityAccessStatus>> RequestAccessForCapabilitiesForUserAsync( global::Windows.System.User user,  global::System.Collections.Generic.IEnumerable<string> capabilityNames)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyDictionary<string, AppCapabilityAccessStatus>> AppCapability.RequestAccessForCapabilitiesForUserAsync(User user, IEnumerable<string> capabilityNames) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CIReadOnlyDictionary%3Cstring%2C%20AppCapabilityAccessStatus%3E%3E%20AppCapability.RequestAccessForCapabilitiesForUserAsync%28User%20user%2C%20IEnumerable%3Cstring%3E%20capabilityNames%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Security.Authorization.AppCapabilityAccess.AppCapability Create( string capabilityName)
		{
			throw new global::System.NotImplementedException("The member AppCapability AppCapability.Create(string capabilityName) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AppCapability%20AppCapability.Create%28string%20capabilityName%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Security.Authorization.AppCapabilityAccess.AppCapability CreateWithProcessIdForUser( global::Windows.System.User user,  string capabilityName,  uint pid)
		{
			throw new global::System.NotImplementedException("The member AppCapability AppCapability.CreateWithProcessIdForUser(User user, string capabilityName, uint pid) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AppCapability%20AppCapability.CreateWithProcessIdForUser%28User%20user%2C%20string%20capabilityName%2C%20uint%20pid%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Security.Authorization.AppCapabilityAccess.AppCapability, global::Windows.Security.Authorization.AppCapabilityAccess.AppCapabilityAccessChangedEventArgs> AccessChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authorization.AppCapabilityAccess.AppCapability", "event TypedEventHandler<AppCapability, AppCapabilityAccessChangedEventArgs> AppCapability.AccessChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authorization.AppCapabilityAccess.AppCapability", "event TypedEventHandler<AppCapability, AppCapabilityAccessChangedEventArgs> AppCapability.AccessChanged");
			}
		}
		#endif
	}
}
