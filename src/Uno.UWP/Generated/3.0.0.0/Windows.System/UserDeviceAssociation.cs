#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserDeviceAssociation 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.User FindUserFromDeviceId( string deviceId)
		{
			throw new global::System.NotImplementedException("The member User UserDeviceAssociation.FindUserFromDeviceId(string deviceId) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.System.UserDeviceAssociation.UserDeviceAssociationChanged.add
		// Forced skipping of method Windows.System.UserDeviceAssociation.UserDeviceAssociationChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<global::Windows.System.UserDeviceAssociationChangedEventArgs> UserDeviceAssociationChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.UserDeviceAssociation", "event EventHandler<UserDeviceAssociationChangedEventArgs> UserDeviceAssociation.UserDeviceAssociationChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.UserDeviceAssociation", "event EventHandler<UserDeviceAssociationChangedEventArgs> UserDeviceAssociation.UserDeviceAssociationChanged");
			}
		}
		#endif
	}
}
