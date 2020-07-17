#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Identity.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SecondaryAuthenticationFactorAuthentication 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer DeviceConfigurationData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer SecondaryAuthenticationFactorAuthentication.DeviceConfigurationData is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer DeviceNonce
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer SecondaryAuthenticationFactorAuthentication.DeviceNonce is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer ServiceAuthenticationHmac
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer SecondaryAuthenticationFactorAuthentication.ServiceAuthenticationHmac is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer SessionNonce
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer SecondaryAuthenticationFactorAuthentication.SessionNonce is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Identity.Provider.SecondaryAuthenticationFactorAuthentication.ServiceAuthenticationHmac.get
		// Forced skipping of method Windows.Security.Authentication.Identity.Provider.SecondaryAuthenticationFactorAuthentication.SessionNonce.get
		// Forced skipping of method Windows.Security.Authentication.Identity.Provider.SecondaryAuthenticationFactorAuthentication.DeviceNonce.get
		// Forced skipping of method Windows.Security.Authentication.Identity.Provider.SecondaryAuthenticationFactorAuthentication.DeviceConfigurationData.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Identity.Provider.SecondaryAuthenticationFactorFinishAuthenticationStatus> FinishAuthenticationAsync( global::Windows.Storage.Streams.IBuffer deviceHmac,  global::Windows.Storage.Streams.IBuffer sessionHmac)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SecondaryAuthenticationFactorFinishAuthenticationStatus> SecondaryAuthenticationFactorAuthentication.FinishAuthenticationAsync(IBuffer deviceHmac, IBuffer sessionHmac) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction AbortAuthenticationAsync( string errorLogMessage)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction SecondaryAuthenticationFactorAuthentication.AbortAuthenticationAsync(string errorLogMessage) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction ShowNotificationMessageAsync( string deviceName,  global::Windows.Security.Authentication.Identity.Provider.SecondaryAuthenticationFactorAuthenticationMessage message)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction SecondaryAuthenticationFactorAuthentication.ShowNotificationMessageAsync(string deviceName, SecondaryAuthenticationFactorAuthenticationMessage message) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Identity.Provider.SecondaryAuthenticationFactorAuthenticationResult> StartAuthenticationAsync( string deviceId,  global::Windows.Storage.Streams.IBuffer serviceAuthenticationNonce)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SecondaryAuthenticationFactorAuthenticationResult> SecondaryAuthenticationFactorAuthentication.StartAuthenticationAsync(string deviceId, IBuffer serviceAuthenticationNonce) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Identity.Provider.SecondaryAuthenticationFactorAuthentication.AuthenticationStageChanged.add
		// Forced skipping of method Windows.Security.Authentication.Identity.Provider.SecondaryAuthenticationFactorAuthentication.AuthenticationStageChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Identity.Provider.SecondaryAuthenticationFactorAuthenticationStageInfo> GetAuthenticationStageInfoAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SecondaryAuthenticationFactorAuthenticationStageInfo> SecondaryAuthenticationFactorAuthentication.GetAuthenticationStageInfoAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<global::Windows.Security.Authentication.Identity.Provider.SecondaryAuthenticationFactorAuthenticationStageChangedEventArgs> AuthenticationStageChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Identity.Provider.SecondaryAuthenticationFactorAuthentication", "event EventHandler<SecondaryAuthenticationFactorAuthenticationStageChangedEventArgs> SecondaryAuthenticationFactorAuthentication.AuthenticationStageChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Identity.Provider.SecondaryAuthenticationFactorAuthentication", "event EventHandler<SecondaryAuthenticationFactorAuthenticationStageChangedEventArgs> SecondaryAuthenticationFactorAuthentication.AuthenticationStageChanged");
			}
		}
		#endif
	}
}
