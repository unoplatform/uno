#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Appointments
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppointmentManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Appointments.AppointmentManagerForUser GetForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member AppointmentManagerForUser AppointmentManager.GetForUser(User user) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction ShowAppointmentDetailsAsync( string appointmentId)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction AppointmentManager.ShowAppointmentDetailsAsync(string appointmentId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction ShowAppointmentDetailsAsync( string appointmentId,  global::System.DateTimeOffset instanceStartDate)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction AppointmentManager.ShowAppointmentDetailsAsync(string appointmentId, DateTimeOffset instanceStartDate) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> ShowEditNewAppointmentAsync( global::Windows.ApplicationModel.Appointments.Appointment appointment)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> AppointmentManager.ShowEditNewAppointmentAsync(Appointment appointment) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Appointments.AppointmentStore> RequestStoreAsync( global::Windows.ApplicationModel.Appointments.AppointmentStoreAccessType options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AppointmentStore> AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> ShowAddAppointmentAsync( global::Windows.ApplicationModel.Appointments.Appointment appointment,  global::Windows.Foundation.Rect selection)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> AppointmentManager.ShowAddAppointmentAsync(Appointment appointment, Rect selection) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> ShowAddAppointmentAsync( global::Windows.ApplicationModel.Appointments.Appointment appointment,  global::Windows.Foundation.Rect selection,  global::Windows.UI.Popups.Placement preferredPlacement)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> AppointmentManager.ShowAddAppointmentAsync(Appointment appointment, Rect selection, Placement preferredPlacement) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> ShowReplaceAppointmentAsync( string appointmentId,  global::Windows.ApplicationModel.Appointments.Appointment appointment,  global::Windows.Foundation.Rect selection)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> AppointmentManager.ShowReplaceAppointmentAsync(string appointmentId, Appointment appointment, Rect selection) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> ShowReplaceAppointmentAsync( string appointmentId,  global::Windows.ApplicationModel.Appointments.Appointment appointment,  global::Windows.Foundation.Rect selection,  global::Windows.UI.Popups.Placement preferredPlacement)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> AppointmentManager.ShowReplaceAppointmentAsync(string appointmentId, Appointment appointment, Rect selection, Placement preferredPlacement) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> ShowReplaceAppointmentAsync( string appointmentId,  global::Windows.ApplicationModel.Appointments.Appointment appointment,  global::Windows.Foundation.Rect selection,  global::Windows.UI.Popups.Placement preferredPlacement,  global::System.DateTimeOffset instanceStartDate)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> AppointmentManager.ShowReplaceAppointmentAsync(string appointmentId, Appointment appointment, Rect selection, Placement preferredPlacement, DateTimeOffset instanceStartDate) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<bool> ShowRemoveAppointmentAsync( string appointmentId,  global::Windows.Foundation.Rect selection)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> AppointmentManager.ShowRemoveAppointmentAsync(string appointmentId, Rect selection) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<bool> ShowRemoveAppointmentAsync( string appointmentId,  global::Windows.Foundation.Rect selection,  global::Windows.UI.Popups.Placement preferredPlacement)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> AppointmentManager.ShowRemoveAppointmentAsync(string appointmentId, Rect selection, Placement preferredPlacement) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<bool> ShowRemoveAppointmentAsync( string appointmentId,  global::Windows.Foundation.Rect selection,  global::Windows.UI.Popups.Placement preferredPlacement,  global::System.DateTimeOffset instanceStartDate)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> AppointmentManager.ShowRemoveAppointmentAsync(string appointmentId, Rect selection, Placement preferredPlacement, DateTimeOffset instanceStartDate) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction ShowTimeFrameAsync( global::System.DateTimeOffset timeToShow,  global::System.TimeSpan duration)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction AppointmentManager.ShowTimeFrameAsync(DateTimeOffset timeToShow, TimeSpan duration) is not implemented in Uno.");
		}
		#endif
	}
}
