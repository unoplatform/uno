#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Enumeration
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DevicePicker 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Enumeration.DevicePickerAppearance Appearance
		{
			get
			{
				throw new global::System.NotImplementedException("The member DevicePickerAppearance DevicePicker.Appearance is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Enumeration.DevicePickerFilter Filter
		{
			get
			{
				throw new global::System.NotImplementedException("The member DevicePickerFilter DevicePicker.Filter is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<string> RequestedProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<string> DevicePicker.RequestedProperties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public DevicePicker() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.DevicePicker", "DevicePicker.DevicePicker()");
		}
		#endif
		// Forced skipping of method Windows.Devices.Enumeration.DevicePicker.DevicePicker()
		// Forced skipping of method Windows.Devices.Enumeration.DevicePicker.Filter.get
		// Forced skipping of method Windows.Devices.Enumeration.DevicePicker.Appearance.get
		// Forced skipping of method Windows.Devices.Enumeration.DevicePicker.RequestedProperties.get
		// Forced skipping of method Windows.Devices.Enumeration.DevicePicker.DeviceSelected.add
		// Forced skipping of method Windows.Devices.Enumeration.DevicePicker.DeviceSelected.remove
		// Forced skipping of method Windows.Devices.Enumeration.DevicePicker.DisconnectButtonClicked.add
		// Forced skipping of method Windows.Devices.Enumeration.DevicePicker.DisconnectButtonClicked.remove
		// Forced skipping of method Windows.Devices.Enumeration.DevicePicker.DevicePickerDismissed.add
		// Forced skipping of method Windows.Devices.Enumeration.DevicePicker.DevicePickerDismissed.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Show( global::Windows.Foundation.Rect selection)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.DevicePicker", "void DevicePicker.Show(Rect selection)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Show( global::Windows.Foundation.Rect selection,  global::Windows.UI.Popups.Placement placement)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.DevicePicker", "void DevicePicker.Show(Rect selection, Placement placement)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Enumeration.DeviceInformation> PickSingleDeviceAsync( global::Windows.Foundation.Rect selection)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DeviceInformation> DevicePicker.PickSingleDeviceAsync(Rect selection) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Enumeration.DeviceInformation> PickSingleDeviceAsync( global::Windows.Foundation.Rect selection,  global::Windows.UI.Popups.Placement placement)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DeviceInformation> DevicePicker.PickSingleDeviceAsync(Rect selection, Placement placement) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Hide()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.DevicePicker", "void DevicePicker.Hide()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetDisplayStatus( global::Windows.Devices.Enumeration.DeviceInformation device,  string status,  global::Windows.Devices.Enumeration.DevicePickerDisplayStatusOptions options)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.DevicePicker", "void DevicePicker.SetDisplayStatus(DeviceInformation device, string status, DevicePickerDisplayStatusOptions options)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Enumeration.DevicePicker, object> DevicePickerDismissed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.DevicePicker", "event TypedEventHandler<DevicePicker, object> DevicePicker.DevicePickerDismissed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.DevicePicker", "event TypedEventHandler<DevicePicker, object> DevicePicker.DevicePickerDismissed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Enumeration.DevicePicker, global::Windows.Devices.Enumeration.DeviceSelectedEventArgs> DeviceSelected
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.DevicePicker", "event TypedEventHandler<DevicePicker, DeviceSelectedEventArgs> DevicePicker.DeviceSelected");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.DevicePicker", "event TypedEventHandler<DevicePicker, DeviceSelectedEventArgs> DevicePicker.DeviceSelected");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Enumeration.DevicePicker, global::Windows.Devices.Enumeration.DeviceDisconnectButtonClickedEventArgs> DisconnectButtonClicked
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.DevicePicker", "event TypedEventHandler<DevicePicker, DeviceDisconnectButtonClickedEventArgs> DevicePicker.DisconnectButtonClicked");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.DevicePicker", "event TypedEventHandler<DevicePicker, DeviceDisconnectButtonClickedEventArgs> DevicePicker.DisconnectButtonClicked");
			}
		}
		#endif
	}
}
