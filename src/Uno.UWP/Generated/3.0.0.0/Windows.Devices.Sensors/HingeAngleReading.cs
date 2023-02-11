#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	#endif
	public  partial class HingeAngleReading 
	{
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double AngleInDegrees
		{
			get
			{
				throw new global::System.NotImplementedException("The member double HingeAngleReading.AngleInDegrees is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=double%20HingeAngleReading.AngleInDegrees");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyDictionary<string, object> Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<string, object> HingeAngleReading.Properties is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyDictionary%3Cstring%2C%20object%3E%20HingeAngleReading.Properties");
			}
		}
		#endif
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset HingeAngleReading.Timestamp is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DateTimeOffset%20HingeAngleReading.Timestamp");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.HingeAngleReading.Timestamp.get
		// Forced skipping of method Windows.Devices.Sensors.HingeAngleReading.AngleInDegrees.get
		// Forced skipping of method Windows.Devices.Sensors.HingeAngleReading.Properties.get
	}
}
