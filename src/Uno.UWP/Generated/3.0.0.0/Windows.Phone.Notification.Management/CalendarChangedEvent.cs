#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.Notification.Management
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CalendarChangedEvent 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LostEvents,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AppointmentAdded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AppointmentChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AppointmentDeleted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CalendarAdded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CalendarChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CalendarDeleted,
		#endif
	}
	#endif
}
