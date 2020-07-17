#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.SerialCommunication
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SerialDevice : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsDataTerminalReadyEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SerialDevice.IsDataTerminalReadyEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.SerialCommunication.SerialDevice", "bool SerialDevice.IsDataTerminalReadyEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort DataBits
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort SerialDevice.DataBits is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.SerialCommunication.SerialDevice", "ushort SerialDevice.DataBits");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.SerialCommunication.SerialHandshake Handshake
		{
			get
			{
				throw new global::System.NotImplementedException("The member SerialHandshake SerialDevice.Handshake is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.SerialCommunication.SerialDevice", "SerialHandshake SerialDevice.Handshake");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool BreakSignalState
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SerialDevice.BreakSignalState is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.SerialCommunication.SerialDevice", "bool SerialDevice.BreakSignalState");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint BaudRate
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint SerialDevice.BaudRate is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.SerialCommunication.SerialDevice", "uint SerialDevice.BaudRate");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan WriteTimeout
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan SerialDevice.WriteTimeout is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.SerialCommunication.SerialDevice", "TimeSpan SerialDevice.WriteTimeout");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.SerialCommunication.SerialStopBitCount StopBits
		{
			get
			{
				throw new global::System.NotImplementedException("The member SerialStopBitCount SerialDevice.StopBits is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.SerialCommunication.SerialDevice", "SerialStopBitCount SerialDevice.StopBits");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan ReadTimeout
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan SerialDevice.ReadTimeout is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.SerialCommunication.SerialDevice", "TimeSpan SerialDevice.ReadTimeout");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.SerialCommunication.SerialParity Parity
		{
			get
			{
				throw new global::System.NotImplementedException("The member SerialParity SerialDevice.Parity is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.SerialCommunication.SerialDevice", "SerialParity SerialDevice.Parity");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsRequestToSendEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SerialDevice.IsRequestToSendEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.SerialCommunication.SerialDevice", "bool SerialDevice.IsRequestToSendEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint BytesReceived
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint SerialDevice.BytesReceived is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CarrierDetectState
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SerialDevice.CarrierDetectState is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool ClearToSendState
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SerialDevice.ClearToSendState is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool DataSetReadyState
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SerialDevice.DataSetReadyState is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IInputStream InputStream
		{
			get
			{
				throw new global::System.NotImplementedException("The member IInputStream SerialDevice.InputStream is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IOutputStream OutputStream
		{
			get
			{
				throw new global::System.NotImplementedException("The member IOutputStream SerialDevice.OutputStream is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string PortName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SerialDevice.PortName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort UsbProductId
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort SerialDevice.UsbProductId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort UsbVendorId
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort SerialDevice.UsbVendorId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.BaudRate.get
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.BaudRate.set
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.BreakSignalState.get
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.BreakSignalState.set
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.BytesReceived.get
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.CarrierDetectState.get
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.ClearToSendState.get
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.DataBits.get
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.DataBits.set
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.DataSetReadyState.get
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.Handshake.get
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.Handshake.set
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.IsDataTerminalReadyEnabled.get
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.IsDataTerminalReadyEnabled.set
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.IsRequestToSendEnabled.get
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.IsRequestToSendEnabled.set
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.Parity.get
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.Parity.set
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.PortName.get
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.ReadTimeout.get
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.ReadTimeout.set
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.StopBits.get
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.StopBits.set
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.UsbVendorId.get
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.UsbProductId.get
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.WriteTimeout.get
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.WriteTimeout.set
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.InputStream.get
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.OutputStream.get
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.ErrorReceived.add
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.ErrorReceived.remove
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.PinChanged.add
		// Forced skipping of method Windows.Devices.SerialCommunication.SerialDevice.PinChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.SerialCommunication.SerialDevice", "void SerialDevice.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelector()
		{
			throw new global::System.NotImplementedException("The member string SerialDevice.GetDeviceSelector() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelector( string portName)
		{
			throw new global::System.NotImplementedException("The member string SerialDevice.GetDeviceSelector(string portName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelectorFromUsbVidPid( ushort vendorId,  ushort productId)
		{
			throw new global::System.NotImplementedException("The member string SerialDevice.GetDeviceSelectorFromUsbVidPid(ushort vendorId, ushort productId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.SerialCommunication.SerialDevice> FromIdAsync( string deviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SerialDevice> SerialDevice.FromIdAsync(string deviceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.SerialCommunication.SerialDevice, global::Windows.Devices.SerialCommunication.ErrorReceivedEventArgs> ErrorReceived
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.SerialCommunication.SerialDevice", "event TypedEventHandler<SerialDevice, ErrorReceivedEventArgs> SerialDevice.ErrorReceived");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.SerialCommunication.SerialDevice", "event TypedEventHandler<SerialDevice, ErrorReceivedEventArgs> SerialDevice.ErrorReceived");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.SerialCommunication.SerialDevice, global::Windows.Devices.SerialCommunication.PinChangedEventArgs> PinChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.SerialCommunication.SerialDevice", "event TypedEventHandler<SerialDevice, PinChangedEventArgs> SerialDevice.PinChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.SerialCommunication.SerialDevice", "event TypedEventHandler<SerialDevice, PinChangedEventArgs> SerialDevice.PinChanged");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
