#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Appointments
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class FindAppointmentsOptions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint MaxCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint FindAppointmentsOptions.MaxCount is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Appointments.FindAppointmentsOptions", "uint FindAppointmentsOptions.MaxCount");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IncludeHidden
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool FindAppointmentsOptions.IncludeHidden is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Appointments.FindAppointmentsOptions", "bool FindAppointmentsOptions.IncludeHidden");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<string> CalendarIds
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<string> FindAppointmentsOptions.CalendarIds is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<string> FetchProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<string> FindAppointmentsOptions.FetchProperties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public FindAppointmentsOptions() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Appointments.FindAppointmentsOptions", "FindAppointmentsOptions.FindAppointmentsOptions()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Appointments.FindAppointmentsOptions.FindAppointmentsOptions()
		// Forced skipping of method Windows.ApplicationModel.Appointments.FindAppointmentsOptions.CalendarIds.get
		// Forced skipping of method Windows.ApplicationModel.Appointments.FindAppointmentsOptions.FetchProperties.get
		// Forced skipping of method Windows.ApplicationModel.Appointments.FindAppointmentsOptions.IncludeHidden.get
		// Forced skipping of method Windows.ApplicationModel.Appointments.FindAppointmentsOptions.IncludeHidden.set
		// Forced skipping of method Windows.ApplicationModel.Appointments.FindAppointmentsOptions.MaxCount.get
		// Forced skipping of method Windows.ApplicationModel.Appointments.FindAppointmentsOptions.MaxCount.set
	}
}
