#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InfraredTorchControl 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int Power
		{
			get
			{
				throw new global::System.NotImplementedException("The member int InfraredTorchControl.Power is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.InfraredTorchControl", "int InfraredTorchControl.Power");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.InfraredTorchMode CurrentMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member InfraredTorchMode InfraredTorchControl.CurrentMode is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.InfraredTorchControl", "InfraredTorchMode InfraredTorchControl.CurrentMode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool InfraredTorchControl.IsSupported is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int MaxPower
		{
			get
			{
				throw new global::System.NotImplementedException("The member int InfraredTorchControl.MaxPower is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int MinPower
		{
			get
			{
				throw new global::System.NotImplementedException("The member int InfraredTorchControl.MinPower is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int PowerStep
		{
			get
			{
				throw new global::System.NotImplementedException("The member int InfraredTorchControl.PowerStep is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Devices.InfraredTorchMode> SupportedModes
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<InfraredTorchMode> InfraredTorchControl.SupportedModes is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.InfraredTorchControl.IsSupported.get
		// Forced skipping of method Windows.Media.Devices.InfraredTorchControl.SupportedModes.get
		// Forced skipping of method Windows.Media.Devices.InfraredTorchControl.CurrentMode.get
		// Forced skipping of method Windows.Media.Devices.InfraredTorchControl.CurrentMode.set
		// Forced skipping of method Windows.Media.Devices.InfraredTorchControl.MinPower.get
		// Forced skipping of method Windows.Media.Devices.InfraredTorchControl.MaxPower.get
		// Forced skipping of method Windows.Media.Devices.InfraredTorchControl.PowerStep.get
		// Forced skipping of method Windows.Media.Devices.InfraredTorchControl.Power.get
		// Forced skipping of method Windows.Media.Devices.InfraredTorchControl.Power.set
	}
}
