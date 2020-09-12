#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Appointments
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IAppointmentParticipant 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Address
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string DisplayName
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Appointments.IAppointmentParticipant.DisplayName.get
		// Forced skipping of method Windows.ApplicationModel.Appointments.IAppointmentParticipant.DisplayName.set
		// Forced skipping of method Windows.ApplicationModel.Appointments.IAppointmentParticipant.Address.get
		// Forced skipping of method Windows.ApplicationModel.Appointments.IAppointmentParticipant.Address.set
	}
}
