#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Miracast
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MiracastReceiverKeyboardDevice 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool TransmitInput
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MiracastReceiverKeyboardDevice.TransmitInput is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Miracast.MiracastReceiverKeyboardDevice", "bool MiracastReceiverKeyboardDevice.TransmitInput");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsRequestedByTransmitter
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MiracastReceiverKeyboardDevice.IsRequestedByTransmitter is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsTransmittingInput
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MiracastReceiverKeyboardDevice.IsTransmittingInput is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverKeyboardDevice.TransmitInput.get
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverKeyboardDevice.TransmitInput.set
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverKeyboardDevice.IsRequestedByTransmitter.get
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverKeyboardDevice.IsTransmittingInput.get
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverKeyboardDevice.Changed.add
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverKeyboardDevice.Changed.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Miracast.MiracastReceiverKeyboardDevice, object> Changed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Miracast.MiracastReceiverKeyboardDevice", "event TypedEventHandler<MiracastReceiverKeyboardDevice, object> MiracastReceiverKeyboardDevice.Changed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Miracast.MiracastReceiverKeyboardDevice", "event TypedEventHandler<MiracastReceiverKeyboardDevice, object> MiracastReceiverKeyboardDevice.Changed");
			}
		}
		#endif
	}
}
