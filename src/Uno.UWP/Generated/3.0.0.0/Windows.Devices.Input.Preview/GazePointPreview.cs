#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Input.Preview
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GazePointPreview 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Point? EyeGazePosition
		{
			get
			{
				throw new global::System.NotImplementedException("The member Point? GazePointPreview.EyeGazePosition is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Point? HeadGazePosition
		{
			get
			{
				throw new global::System.NotImplementedException("The member Point? GazePointPreview.HeadGazePosition is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.HumanInterfaceDevice.HidInputReport HidInputReport
		{
			get
			{
				throw new global::System.NotImplementedException("The member HidInputReport GazePointPreview.HidInputReport is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Input.Preview.GazeDevicePreview SourceDevice
		{
			get
			{
				throw new global::System.NotImplementedException("The member GazeDevicePreview GazePointPreview.SourceDevice is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong GazePointPreview.Timestamp is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Input.Preview.GazePointPreview.SourceDevice.get
		// Forced skipping of method Windows.Devices.Input.Preview.GazePointPreview.EyeGazePosition.get
		// Forced skipping of method Windows.Devices.Input.Preview.GazePointPreview.HeadGazePosition.get
		// Forced skipping of method Windows.Devices.Input.Preview.GazePointPreview.Timestamp.get
		// Forced skipping of method Windows.Devices.Input.Preview.GazePointPreview.HidInputReport.get
	}
}
