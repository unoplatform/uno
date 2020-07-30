#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Appointments
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppointmentManagerForUser 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User AppointmentManagerForUser.User is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> ShowAddAppointmentAsync( global::Windows.ApplicationModel.Appointments.Appointment appointment,  global::Windows.Foundation.Rect selection)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> AppointmentManagerForUser.ShowAddAppointmentAsync(Appointment appointment, Rect selection) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> ShowAddAppointmentAsync( global::Windows.ApplicationModel.Appointments.Appointment appointment,  global::Windows.Foundation.Rect selection,  global::Windows.UI.Popups.Placement preferredPlacement)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> AppointmentManagerForUser.ShowAddAppointmentAsync(Appointment appointment, Rect selection, Placement preferredPlacement) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> ShowReplaceAppointmentAsync( string appointmentId,  global::Windows.ApplicationModel.Appointments.Appointment appointment,  global::Windows.Foundation.Rect selection)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> AppointmentManagerForUser.ShowReplaceAppointmentAsync(string appointmentId, Appointment appointment, Rect selection) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> ShowReplaceAppointmentAsync( string appointmentId,  global::Windows.ApplicationModel.Appointments.Appointment appointment,  global::Windows.Foundation.Rect selection,  global::Windows.UI.Popups.Placement preferredPlacement)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> AppointmentManagerForUser.ShowReplaceAppointmentAsync(string appointmentId, Appointment appointment, Rect selection, Placement preferredPlacement) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> ShowReplaceAppointmentAsync( string appointmentId,  global::Windows.ApplicationModel.Appointments.Appointment appointment,  global::Windows.Foundation.Rect selection,  global::Windows.UI.Popups.Placement preferredPlacement,  global::System.DateTimeOffset instanceStartDate)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> AppointmentManagerForUser.ShowReplaceAppointmentAsync(string appointmentId, Appointment appointment, Rect selection, Placement preferredPlacement, DateTimeOffset instanceStartDate) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> ShowRemoveAppointmentAsync( string appointmentId,  global::Windows.Foundation.Rect selection)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> AppointmentManagerForUser.ShowRemoveAppointmentAsync(string appointmentId, Rect selection) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> ShowRemoveAppointmentAsync( string appointmentId,  global::Windows.Foundation.Rect selection,  global::Windows.UI.Popups.Placement preferredPlacement)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> AppointmentManagerForUser.ShowRemoveAppointmentAsync(string appointmentId, Rect selection, Placement preferredPlacement) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> ShowRemoveAppointmentAsync( string appointmentId,  global::Windows.Foundation.Rect selection,  global::Windows.UI.Popups.Placement preferredPlacement,  global::System.DateTimeOffset instanceStartDate)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> AppointmentManagerForUser.ShowRemoveAppointmentAsync(string appointmentId, Rect selection, Placement preferredPlacement, DateTimeOffset instanceStartDate) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ShowTimeFrameAsync( global::System.DateTimeOffset timeToShow,  global::System.TimeSpan duration)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction AppointmentManagerForUser.ShowTimeFrameAsync(DateTimeOffset timeToShow, TimeSpan duration) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ShowAppointmentDetailsAsync( string appointmentId)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction AppointmentManagerForUser.ShowAppointmentDetailsAsync(string appointmentId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ShowAppointmentDetailsAsync( string appointmentId,  global::System.DateTimeOffset instanceStartDate)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction AppointmentManagerForUser.ShowAppointmentDetailsAsync(string appointmentId, DateTimeOffset instanceStartDate) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> ShowEditNewAppointmentAsync( global::Windows.ApplicationModel.Appointments.Appointment appointment)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> AppointmentManagerForUser.ShowEditNewAppointmentAsync(Appointment appointment) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Appointments.AppointmentStore> RequestStoreAsync( global::Windows.ApplicationModel.Appointments.AppointmentStoreAccessType options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AppointmentStore> AppointmentManagerForUser.RequestStoreAsync(AppointmentStoreAccessType options) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Appointments.AppointmentManagerForUser.User.get
	}
}
