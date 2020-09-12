#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Appointments
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppointmentStore 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Appointments.AppointmentStoreChangeTracker ChangeTracker
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppointmentStoreChangeTracker AppointmentStore.ChangeTracker is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Appointments.AppointmentStore.ChangeTracker.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Appointments.AppointmentCalendar> CreateAppointmentCalendarAsync( string name)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AppointmentCalendar> AppointmentStore.CreateAppointmentCalendarAsync(string name) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Appointments.AppointmentCalendar> GetAppointmentCalendarAsync( string calendarId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AppointmentCalendar> AppointmentStore.GetAppointmentCalendarAsync(string calendarId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Appointments.Appointment> GetAppointmentAsync( string localId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<Appointment> AppointmentStore.GetAppointmentAsync(string localId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Appointments.Appointment> GetAppointmentInstanceAsync( string localId,  global::System.DateTimeOffset instanceStartTime)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<Appointment> AppointmentStore.GetAppointmentInstanceAsync(string localId, DateTimeOffset instanceStartTime) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Appointments.AppointmentCalendar>> FindAppointmentCalendarsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<AppointmentCalendar>> AppointmentStore.FindAppointmentCalendarsAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Appointments.AppointmentCalendar>> FindAppointmentCalendarsAsync( global::Windows.ApplicationModel.Appointments.FindAppointmentCalendarsOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<AppointmentCalendar>> AppointmentStore.FindAppointmentCalendarsAsync(FindAppointmentCalendarsOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Appointments.Appointment>> FindAppointmentsAsync( global::System.DateTimeOffset rangeStart,  global::System.TimeSpan rangeLength)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<Appointment>> AppointmentStore.FindAppointmentsAsync(DateTimeOffset rangeStart, TimeSpan rangeLength) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Appointments.Appointment>> FindAppointmentsAsync( global::System.DateTimeOffset rangeStart,  global::System.TimeSpan rangeLength,  global::Windows.ApplicationModel.Appointments.FindAppointmentsOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<Appointment>> AppointmentStore.FindAppointmentsAsync(DateTimeOffset rangeStart, TimeSpan rangeLength, FindAppointmentsOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Appointments.AppointmentConflictResult> FindConflictAsync( global::Windows.ApplicationModel.Appointments.Appointment appointment)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AppointmentConflictResult> AppointmentStore.FindConflictAsync(Appointment appointment) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Appointments.AppointmentConflictResult> FindConflictAsync( global::Windows.ApplicationModel.Appointments.Appointment appointment,  global::System.DateTimeOffset instanceStartTime)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AppointmentConflictResult> AppointmentStore.FindConflictAsync(Appointment appointment, DateTimeOffset instanceStartTime) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction MoveAppointmentAsync( global::Windows.ApplicationModel.Appointments.Appointment appointment,  global::Windows.ApplicationModel.Appointments.AppointmentCalendar destinationCalendar)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction AppointmentStore.MoveAppointmentAsync(Appointment appointment, AppointmentCalendar destinationCalendar) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> ShowAddAppointmentAsync( global::Windows.ApplicationModel.Appointments.Appointment appointment,  global::Windows.Foundation.Rect selection)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> AppointmentStore.ShowAddAppointmentAsync(Appointment appointment, Rect selection) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> ShowReplaceAppointmentAsync( string localId,  global::Windows.ApplicationModel.Appointments.Appointment appointment,  global::Windows.Foundation.Rect selection)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> AppointmentStore.ShowReplaceAppointmentAsync(string localId, Appointment appointment, Rect selection) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> ShowReplaceAppointmentAsync( string localId,  global::Windows.ApplicationModel.Appointments.Appointment appointment,  global::Windows.Foundation.Rect selection,  global::Windows.UI.Popups.Placement preferredPlacement,  global::System.DateTimeOffset instanceStartDate)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> AppointmentStore.ShowReplaceAppointmentAsync(string localId, Appointment appointment, Rect selection, Placement preferredPlacement, DateTimeOffset instanceStartDate) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> ShowRemoveAppointmentAsync( string localId,  global::Windows.Foundation.Rect selection)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> AppointmentStore.ShowRemoveAppointmentAsync(string localId, Rect selection) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> ShowRemoveAppointmentAsync( string localId,  global::Windows.Foundation.Rect selection,  global::Windows.UI.Popups.Placement preferredPlacement,  global::System.DateTimeOffset instanceStartDate)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> AppointmentStore.ShowRemoveAppointmentAsync(string localId, Rect selection, Placement preferredPlacement, DateTimeOffset instanceStartDate) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ShowAppointmentDetailsAsync( string localId)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction AppointmentStore.ShowAppointmentDetailsAsync(string localId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ShowAppointmentDetailsAsync( string localId,  global::System.DateTimeOffset instanceStartDate)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction AppointmentStore.ShowAppointmentDetailsAsync(string localId, DateTimeOffset instanceStartDate) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> ShowEditNewAppointmentAsync( global::Windows.ApplicationModel.Appointments.Appointment appointment)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> AppointmentStore.ShowEditNewAppointmentAsync(Appointment appointment) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<string>> FindLocalIdsFromRoamingIdAsync( string roamingId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<string>> AppointmentStore.FindLocalIdsFromRoamingIdAsync(string roamingId) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Appointments.AppointmentStore.StoreChanged.add
		// Forced skipping of method Windows.ApplicationModel.Appointments.AppointmentStore.StoreChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Appointments.AppointmentCalendar> CreateAppointmentCalendarAsync( string name,  string userDataAccountId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AppointmentCalendar> AppointmentStore.CreateAppointmentCalendarAsync(string name, string userDataAccountId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Appointments.AppointmentStoreChangeTracker GetChangeTracker( string identity)
		{
			throw new global::System.NotImplementedException("The member AppointmentStoreChangeTracker AppointmentStore.GetChangeTracker(string identity) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.Appointments.AppointmentStore, global::Windows.ApplicationModel.Appointments.AppointmentStoreChangedEventArgs> StoreChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Appointments.AppointmentStore", "event TypedEventHandler<AppointmentStore, AppointmentStoreChangedEventArgs> AppointmentStore.StoreChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Appointments.AppointmentStore", "event TypedEventHandler<AppointmentStore, AppointmentStoreChangedEventArgs> AppointmentStore.StoreChanged");
			}
		}
		#endif
	}
}
