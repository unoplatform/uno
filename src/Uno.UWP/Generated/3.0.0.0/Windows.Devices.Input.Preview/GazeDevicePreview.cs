#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Input.Preview
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GazeDevicePreview 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanTrackEyes
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool GazeDevicePreview.CanTrackEyes is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanTrackHead
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool GazeDevicePreview.CanTrackHead is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Input.Preview.GazeDeviceConfigurationStatePreview ConfigurationState
		{
			get
			{
				throw new global::System.NotImplementedException("The member GazeDeviceConfigurationStatePreview GazeDevicePreview.ConfigurationState is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint GazeDevicePreview.Id is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Input.Preview.GazeDevicePreview.Id.get
		// Forced skipping of method Windows.Devices.Input.Preview.GazeDevicePreview.CanTrackEyes.get
		// Forced skipping of method Windows.Devices.Input.Preview.GazeDevicePreview.CanTrackHead.get
		// Forced skipping of method Windows.Devices.Input.Preview.GazeDevicePreview.ConfigurationState.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> RequestCalibrationAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> GazeDevicePreview.RequestCalibrationAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.HumanInterfaceDevice.HidNumericControlDescription> GetNumericControlDescriptions( ushort usagePage,  ushort usageId)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<HidNumericControlDescription> GazeDevicePreview.GetNumericControlDescriptions(ushort usagePage, ushort usageId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.HumanInterfaceDevice.HidBooleanControlDescription> GetBooleanControlDescriptions( ushort usagePage,  ushort usageId)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<HidBooleanControlDescription> GazeDevicePreview.GetBooleanControlDescriptions(ushort usagePage, ushort usageId) is not implemented in Uno.");
		}
		#endif
	}
}
