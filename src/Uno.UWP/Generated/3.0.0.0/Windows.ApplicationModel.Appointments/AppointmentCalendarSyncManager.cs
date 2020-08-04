#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Appointments
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppointmentCalendarSyncManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Appointments.AppointmentCalendarSyncStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppointmentCalendarSyncStatus AppointmentCalendarSyncManager.Status is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Appointments.AppointmentCalendarSyncManager", "AppointmentCalendarSyncStatus AppointmentCalendarSyncManager.Status");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset LastSuccessfulSyncTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset AppointmentCalendarSyncManager.LastSuccessfulSyncTime is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Appointments.AppointmentCalendarSyncManager", "DateTimeOffset AppointmentCalendarSyncManager.LastSuccessfulSyncTime");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset LastAttemptedSyncTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset AppointmentCalendarSyncManager.LastAttemptedSyncTime is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Appointments.AppointmentCalendarSyncManager", "DateTimeOffset AppointmentCalendarSyncManager.LastAttemptedSyncTime");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Appointments.AppointmentCalendarSyncManager.Status.get
		// Forced skipping of method Windows.ApplicationModel.Appointments.AppointmentCalendarSyncManager.LastSuccessfulSyncTime.get
		// Forced skipping of method Windows.ApplicationModel.Appointments.AppointmentCalendarSyncManager.LastAttemptedSyncTime.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> SyncAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> AppointmentCalendarSyncManager.SyncAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Appointments.AppointmentCalendarSyncManager.SyncStatusChanged.add
		// Forced skipping of method Windows.ApplicationModel.Appointments.AppointmentCalendarSyncManager.SyncStatusChanged.remove
		// Forced skipping of method Windows.ApplicationModel.Appointments.AppointmentCalendarSyncManager.Status.set
		// Forced skipping of method Windows.ApplicationModel.Appointments.AppointmentCalendarSyncManager.LastSuccessfulSyncTime.set
		// Forced skipping of method Windows.ApplicationModel.Appointments.AppointmentCalendarSyncManager.LastAttemptedSyncTime.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.Appointments.AppointmentCalendarSyncManager, object> SyncStatusChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Appointments.AppointmentCalendarSyncManager", "event TypedEventHandler<AppointmentCalendarSyncManager, object> AppointmentCalendarSyncManager.SyncStatusChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Appointments.AppointmentCalendarSyncManager", "event TypedEventHandler<AppointmentCalendarSyncManager, object> AppointmentCalendarSyncManager.SyncStatusChanged");
			}
		}
		#endif
	}
}
