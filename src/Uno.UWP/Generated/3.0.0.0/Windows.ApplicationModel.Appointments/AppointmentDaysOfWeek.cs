#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Appointments
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AppointmentDaysOfWeek 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Sunday,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Monday,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Tuesday,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Wednesday,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Thursday,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Friday,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Saturday,
		#endif
	}
	#endif
}
