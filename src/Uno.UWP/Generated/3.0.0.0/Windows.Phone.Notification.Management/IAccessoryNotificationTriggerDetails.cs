#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.Notification.Management
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IAccessoryNotificationTriggerDetails 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Phone.Notification.Management.AccessoryNotificationType AccessoryNotificationType
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string AppDisplayName
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string AppId
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool StartedProcessing
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.DateTimeOffset TimeCreated
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Phone.Notification.Management.IAccessoryNotificationTriggerDetails.TimeCreated.get
		// Forced skipping of method Windows.Phone.Notification.Management.IAccessoryNotificationTriggerDetails.AppDisplayName.get
		// Forced skipping of method Windows.Phone.Notification.Management.IAccessoryNotificationTriggerDetails.AppId.get
		// Forced skipping of method Windows.Phone.Notification.Management.IAccessoryNotificationTriggerDetails.AccessoryNotificationType.get
		// Forced skipping of method Windows.Phone.Notification.Management.IAccessoryNotificationTriggerDetails.StartedProcessing.get
		// Forced skipping of method Windows.Phone.Notification.Management.IAccessoryNotificationTriggerDetails.StartedProcessing.set
	}
}
