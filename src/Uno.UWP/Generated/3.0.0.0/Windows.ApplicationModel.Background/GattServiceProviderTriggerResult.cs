#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GattServiceProviderTriggerResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.BluetoothError Error
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothError GattServiceProviderTriggerResult.Error is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=BluetoothError%20GattServiceProviderTriggerResult.Error");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Background.GattServiceProviderTrigger Trigger
		{
			get
			{
				throw new global::System.NotImplementedException("The member GattServiceProviderTrigger GattServiceProviderTriggerResult.Trigger is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=GattServiceProviderTrigger%20GattServiceProviderTriggerResult.Trigger");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.GattServiceProviderTriggerResult.Trigger.get
		// Forced skipping of method Windows.ApplicationModel.Background.GattServiceProviderTriggerResult.Error.get
	}
}
