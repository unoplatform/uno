#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Appointments
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AppointmentBusyStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Busy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Tentative,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Free,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OutOfOffice,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WorkingElsewhere,
		#endif
	}
	#endif
}
