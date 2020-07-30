#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ActivitySensorTrigger : global::Windows.ApplicationModel.Background.IBackgroundTrigger
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint MinimumReportInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint ActivitySensorTrigger.MinimumReportInterval is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint ReportInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint ActivitySensorTrigger.ReportInterval is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Devices.Sensors.ActivityType> SubscribedActivities
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<ActivityType> ActivitySensorTrigger.SubscribedActivities is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Sensors.ActivityType> SupportedActivities
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<ActivityType> ActivitySensorTrigger.SupportedActivities is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ActivitySensorTrigger( uint reportIntervalInMilliseconds) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.ActivitySensorTrigger", "ActivitySensorTrigger.ActivitySensorTrigger(uint reportIntervalInMilliseconds)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.ActivitySensorTrigger.ActivitySensorTrigger(uint)
		// Forced skipping of method Windows.ApplicationModel.Background.ActivitySensorTrigger.SubscribedActivities.get
		// Forced skipping of method Windows.ApplicationModel.Background.ActivitySensorTrigger.ReportInterval.get
		// Forced skipping of method Windows.ApplicationModel.Background.ActivitySensorTrigger.SupportedActivities.get
		// Forced skipping of method Windows.ApplicationModel.Background.ActivitySensorTrigger.MinimumReportInterval.get
		// Processing: Windows.ApplicationModel.Background.IBackgroundTrigger
	}
}
