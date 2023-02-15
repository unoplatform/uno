#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Appointments
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppointmentException 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Appointments.Appointment Appointment
		{
			get
			{
				throw new global::System.NotImplementedException("The member Appointment AppointmentException.Appointment is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Appointment%20AppointmentException.Appointment");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<string> ExceptionProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<string> AppointmentException.ExceptionProperties is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3Cstring%3E%20AppointmentException.ExceptionProperties");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsDeleted
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool AppointmentException.IsDeleted is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20AppointmentException.IsDeleted");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Appointments.AppointmentException.Appointment.get
		// Forced skipping of method Windows.ApplicationModel.Appointments.AppointmentException.ExceptionProperties.get
		// Forced skipping of method Windows.ApplicationModel.Appointments.AppointmentException.IsDeleted.get
	}
}
