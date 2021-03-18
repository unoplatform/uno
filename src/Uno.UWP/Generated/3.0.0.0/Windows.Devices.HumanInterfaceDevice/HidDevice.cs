#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.HumanInterfaceDevice
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HidDevice : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort ProductId
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort HidDevice.ProductId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort UsageId
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort HidDevice.UsageId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort UsagePage
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort HidDevice.UsagePage is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort VendorId
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort HidDevice.VendorId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort Version
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort HidDevice.Version is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.HumanInterfaceDevice.HidDevice.VendorId.get
		// Forced skipping of method Windows.Devices.HumanInterfaceDevice.HidDevice.ProductId.get
		// Forced skipping of method Windows.Devices.HumanInterfaceDevice.HidDevice.Version.get
		// Forced skipping of method Windows.Devices.HumanInterfaceDevice.HidDevice.UsagePage.get
		// Forced skipping of method Windows.Devices.HumanInterfaceDevice.HidDevice.UsageId.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.HumanInterfaceDevice.HidInputReport> GetInputReportAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<HidInputReport> HidDevice.GetInputReportAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.HumanInterfaceDevice.HidInputReport> GetInputReportAsync( ushort reportId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<HidInputReport> HidDevice.GetInputReportAsync(ushort reportId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.HumanInterfaceDevice.HidFeatureReport> GetFeatureReportAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<HidFeatureReport> HidDevice.GetFeatureReportAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.HumanInterfaceDevice.HidFeatureReport> GetFeatureReportAsync( ushort reportId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<HidFeatureReport> HidDevice.GetFeatureReportAsync(ushort reportId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.HumanInterfaceDevice.HidOutputReport CreateOutputReport()
		{
			throw new global::System.NotImplementedException("The member HidOutputReport HidDevice.CreateOutputReport() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.HumanInterfaceDevice.HidOutputReport CreateOutputReport( ushort reportId)
		{
			throw new global::System.NotImplementedException("The member HidOutputReport HidDevice.CreateOutputReport(ushort reportId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.HumanInterfaceDevice.HidFeatureReport CreateFeatureReport()
		{
			throw new global::System.NotImplementedException("The member HidFeatureReport HidDevice.CreateFeatureReport() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.HumanInterfaceDevice.HidFeatureReport CreateFeatureReport( ushort reportId)
		{
			throw new global::System.NotImplementedException("The member HidFeatureReport HidDevice.CreateFeatureReport(ushort reportId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<uint> SendOutputReportAsync( global::Windows.Devices.HumanInterfaceDevice.HidOutputReport outputReport)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<uint> HidDevice.SendOutputReportAsync(HidOutputReport outputReport) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<uint> SendFeatureReportAsync( global::Windows.Devices.HumanInterfaceDevice.HidFeatureReport featureReport)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<uint> HidDevice.SendFeatureReportAsync(HidFeatureReport featureReport) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.HumanInterfaceDevice.HidBooleanControlDescription> GetBooleanControlDescriptions( global::Windows.Devices.HumanInterfaceDevice.HidReportType reportType,  ushort usagePage,  ushort usageId)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<HidBooleanControlDescription> HidDevice.GetBooleanControlDescriptions(HidReportType reportType, ushort usagePage, ushort usageId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.HumanInterfaceDevice.HidNumericControlDescription> GetNumericControlDescriptions( global::Windows.Devices.HumanInterfaceDevice.HidReportType reportType,  ushort usagePage,  ushort usageId)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<HidNumericControlDescription> HidDevice.GetNumericControlDescriptions(HidReportType reportType, ushort usagePage, ushort usageId) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.HumanInterfaceDevice.HidDevice.InputReportReceived.add
		// Forced skipping of method Windows.Devices.HumanInterfaceDevice.HidDevice.InputReportReceived.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.HumanInterfaceDevice.HidDevice", "void HidDevice.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelector( ushort usagePage,  ushort usageId)
		{
			throw new global::System.NotImplementedException("The member string HidDevice.GetDeviceSelector(ushort usagePage, ushort usageId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelector( ushort usagePage,  ushort usageId,  ushort vendorId,  ushort productId)
		{
			throw new global::System.NotImplementedException("The member string HidDevice.GetDeviceSelector(ushort usagePage, ushort usageId, ushort vendorId, ushort productId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.HumanInterfaceDevice.HidDevice> FromIdAsync( string deviceId,  global::Windows.Storage.FileAccessMode accessMode)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<HidDevice> HidDevice.FromIdAsync(string deviceId, FileAccessMode accessMode) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.HumanInterfaceDevice.HidDevice, global::Windows.Devices.HumanInterfaceDevice.HidInputReportReceivedEventArgs> InputReportReceived
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.HumanInterfaceDevice.HidDevice", "event TypedEventHandler<HidDevice, HidInputReportReceivedEventArgs> HidDevice.InputReportReceived");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.HumanInterfaceDevice.HidDevice", "event TypedEventHandler<HidDevice, HidInputReportReceivedEventArgs> HidDevice.InputReportReceived");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
