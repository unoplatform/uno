#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Appointments
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppointmentOrganizer : global::Windows.ApplicationModel.Appointments.IAppointmentParticipant
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DisplayName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppointmentOrganizer.DisplayName is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Appointments.AppointmentOrganizer", "string AppointmentOrganizer.DisplayName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Address
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppointmentOrganizer.Address is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Appointments.AppointmentOrganizer", "string AppointmentOrganizer.Address");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AppointmentOrganizer() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Appointments.AppointmentOrganizer", "AppointmentOrganizer.AppointmentOrganizer()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Appointments.AppointmentOrganizer.AppointmentOrganizer()
		// Forced skipping of method Windows.ApplicationModel.Appointments.AppointmentOrganizer.DisplayName.get
		// Forced skipping of method Windows.ApplicationModel.Appointments.AppointmentOrganizer.DisplayName.set
		// Forced skipping of method Windows.ApplicationModel.Appointments.AppointmentOrganizer.Address.get
		// Forced skipping of method Windows.ApplicationModel.Appointments.AppointmentOrganizer.Address.set
		// Processing: Windows.ApplicationModel.Appointments.IAppointmentParticipant
	}
}
