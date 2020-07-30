#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class IsoSpeedControl 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.IsoSpeedPreset Preset
		{
			get
			{
				throw new global::System.NotImplementedException("The member IsoSpeedPreset IsoSpeedControl.Preset is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Supported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool IsoSpeedControl.Supported is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Devices.IsoSpeedPreset> SupportedPresets
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<IsoSpeedPreset> IsoSpeedControl.SupportedPresets is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Auto
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool IsoSpeedControl.Auto is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Max
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint IsoSpeedControl.Max is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Min
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint IsoSpeedControl.Min is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Step
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint IsoSpeedControl.Step is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Value
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint IsoSpeedControl.Value is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.IsoSpeedControl.Supported.get
		// Forced skipping of method Windows.Media.Devices.IsoSpeedControl.SupportedPresets.get
		// Forced skipping of method Windows.Media.Devices.IsoSpeedControl.Preset.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SetPresetAsync( global::Windows.Media.Devices.IsoSpeedPreset preset)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction IsoSpeedControl.SetPresetAsync(IsoSpeedPreset preset) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.IsoSpeedControl.Min.get
		// Forced skipping of method Windows.Media.Devices.IsoSpeedControl.Max.get
		// Forced skipping of method Windows.Media.Devices.IsoSpeedControl.Step.get
		// Forced skipping of method Windows.Media.Devices.IsoSpeedControl.Value.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SetValueAsync( uint isoSpeed)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction IsoSpeedControl.SetValueAsync(uint isoSpeed) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.IsoSpeedControl.Auto.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SetAutoAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction IsoSpeedControl.SetAutoAsync() is not implemented in Uno.");
		}
		#endif
	}
}
