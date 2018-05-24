#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.Notification.Management
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AccessoryNotificationType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Phone,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Email,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Reminder,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Alarm,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Toast,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AppUninstalled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Dnd,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DrivingMode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BatterySaver,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Media,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CortanaTile,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ToastCleared,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CalendarChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		VolumeChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EmailReadStatusChanged,
		#endif
	}
	#endif
}
