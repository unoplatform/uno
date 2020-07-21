#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.Notification.Management
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpeedDialEntry 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ContactName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SpeedDialEntry.ContactName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string NumberType
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SpeedDialEntry.NumberType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string PhoneNumber
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SpeedDialEntry.PhoneNumber is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Phone.Notification.Management.SpeedDialEntry.PhoneNumber.get
		// Forced skipping of method Windows.Phone.Notification.Management.SpeedDialEntry.NumberType.get
		// Forced skipping of method Windows.Phone.Notification.Management.SpeedDialEntry.ContactName.get
	}
}
