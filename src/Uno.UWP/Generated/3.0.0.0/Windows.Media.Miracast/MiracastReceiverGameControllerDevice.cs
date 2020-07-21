#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Miracast
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MiracastReceiverGameControllerDevice 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool TransmitInput
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MiracastReceiverGameControllerDevice.TransmitInput is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Miracast.MiracastReceiverGameControllerDevice", "bool MiracastReceiverGameControllerDevice.TransmitInput");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Miracast.MiracastReceiverGameControllerDeviceUsageMode Mode
		{
			get
			{
				throw new global::System.NotImplementedException("The member MiracastReceiverGameControllerDeviceUsageMode MiracastReceiverGameControllerDevice.Mode is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Miracast.MiracastReceiverGameControllerDevice", "MiracastReceiverGameControllerDeviceUsageMode MiracastReceiverGameControllerDevice.Mode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsRequestedByTransmitter
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MiracastReceiverGameControllerDevice.IsRequestedByTransmitter is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsTransmittingInput
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MiracastReceiverGameControllerDevice.IsTransmittingInput is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverGameControllerDevice.TransmitInput.get
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverGameControllerDevice.TransmitInput.set
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverGameControllerDevice.IsRequestedByTransmitter.get
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverGameControllerDevice.IsTransmittingInput.get
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverGameControllerDevice.Mode.get
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverGameControllerDevice.Mode.set
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverGameControllerDevice.Changed.add
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverGameControllerDevice.Changed.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Miracast.MiracastReceiverGameControllerDevice, object> Changed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Miracast.MiracastReceiverGameControllerDevice", "event TypedEventHandler<MiracastReceiverGameControllerDevice, object> MiracastReceiverGameControllerDevice.Changed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Miracast.MiracastReceiverGameControllerDevice", "event TypedEventHandler<MiracastReceiverGameControllerDevice, object> MiracastReceiverGameControllerDevice.Changed");
			}
		}
		#endif
	}
}
