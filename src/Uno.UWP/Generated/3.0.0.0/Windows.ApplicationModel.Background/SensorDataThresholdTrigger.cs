#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SensorDataThresholdTrigger : global::Windows.ApplicationModel.Background.IBackgroundTrigger
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public SensorDataThresholdTrigger( global::Windows.Devices.Sensors.ISensorDataThreshold threshold) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.SensorDataThresholdTrigger", "SensorDataThresholdTrigger.SensorDataThresholdTrigger(ISensorDataThreshold threshold)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.SensorDataThresholdTrigger.SensorDataThresholdTrigger(Windows.Devices.Sensors.ISensorDataThreshold)
		// Processing: Windows.ApplicationModel.Background.IBackgroundTrigger
	}
}
