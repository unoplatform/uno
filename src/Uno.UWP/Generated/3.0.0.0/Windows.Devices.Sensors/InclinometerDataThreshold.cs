#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InclinometerDataThreshold 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float YawInDegrees
		{
			get
			{
				throw new global::System.NotImplementedException("The member float InclinometerDataThreshold.YawInDegrees is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.InclinometerDataThreshold", "float InclinometerDataThreshold.YawInDegrees");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float RollInDegrees
		{
			get
			{
				throw new global::System.NotImplementedException("The member float InclinometerDataThreshold.RollInDegrees is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.InclinometerDataThreshold", "float InclinometerDataThreshold.RollInDegrees");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float PitchInDegrees
		{
			get
			{
				throw new global::System.NotImplementedException("The member float InclinometerDataThreshold.PitchInDegrees is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.InclinometerDataThreshold", "float InclinometerDataThreshold.PitchInDegrees");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.InclinometerDataThreshold.PitchInDegrees.get
		// Forced skipping of method Windows.Devices.Sensors.InclinometerDataThreshold.PitchInDegrees.set
		// Forced skipping of method Windows.Devices.Sensors.InclinometerDataThreshold.RollInDegrees.get
		// Forced skipping of method Windows.Devices.Sensors.InclinometerDataThreshold.RollInDegrees.set
		// Forced skipping of method Windows.Devices.Sensors.InclinometerDataThreshold.YawInDegrees.get
		// Forced skipping of method Windows.Devices.Sensors.InclinometerDataThreshold.YawInDegrees.set
	}
}
