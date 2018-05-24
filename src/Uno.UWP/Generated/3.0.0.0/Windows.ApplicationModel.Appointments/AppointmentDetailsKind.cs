#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Appointments
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AppointmentDetailsKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PlainText,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Html,
		#endif
	}
	#endif
}
