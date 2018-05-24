#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Appointments
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AppointmentStoreChangeType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AppointmentCreated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AppointmentModified,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AppointmentDeleted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ChangeTrackingLost,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CalendarCreated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CalendarModified,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CalendarDeleted,
		#endif
	}
	#endif
}
