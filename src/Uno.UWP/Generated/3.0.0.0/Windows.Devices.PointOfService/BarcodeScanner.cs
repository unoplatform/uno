#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BarcodeScanner : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.PointOfService.BarcodeScannerCapabilities Capabilities
		{
			get
			{
				throw new global::System.NotImplementedException("The member BarcodeScannerCapabilities BarcodeScanner.Capabilities is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string BarcodeScanner.DeviceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string VideoDeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string BarcodeScanner.VideoDeviceId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.BarcodeScanner.DeviceId.get
		// Forced skipping of method Windows.Devices.PointOfService.BarcodeScanner.Capabilities.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.PointOfService.ClaimedBarcodeScanner> ClaimScannerAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ClaimedBarcodeScanner> BarcodeScanner.ClaimScannerAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<string> CheckHealthAsync( global::Windows.Devices.PointOfService.UnifiedPosHealthCheckLevel level)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> BarcodeScanner.CheckHealthAsync(UnifiedPosHealthCheckLevel level) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<uint>> GetSupportedSymbologiesAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<uint>> BarcodeScanner.GetSupportedSymbologiesAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> IsSymbologySupportedAsync( uint barcodeSymbology)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> BarcodeScanner.IsSymbologySupportedAsync(uint barcodeSymbology) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IBuffer> RetrieveStatisticsAsync( global::System.Collections.Generic.IEnumerable<string> statisticsCategories)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IBuffer> BarcodeScanner.RetrieveStatisticsAsync(IEnumerable<string> statisticsCategories) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IReadOnlyList<string> GetSupportedProfiles()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<string> BarcodeScanner.GetSupportedProfiles() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsProfileSupported( string profile)
		{
			throw new global::System.NotImplementedException("The member bool BarcodeScanner.IsProfileSupported(string profile) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.BarcodeScanner.StatusUpdated.add
		// Forced skipping of method Windows.Devices.PointOfService.BarcodeScanner.StatusUpdated.remove
		// Forced skipping of method Windows.Devices.PointOfService.BarcodeScanner.VideoDeviceId.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.BarcodeScanner", "void BarcodeScanner.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static string GetDeviceSelector( global::Windows.Devices.PointOfService.PosConnectionTypes connectionTypes)
		{
			throw new global::System.NotImplementedException("The member string BarcodeScanner.GetDeviceSelector(PosConnectionTypes connectionTypes) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.PointOfService.BarcodeScanner> GetDefaultAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<BarcodeScanner> BarcodeScanner.GetDefaultAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.PointOfService.BarcodeScanner> FromIdAsync( string deviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<BarcodeScanner> BarcodeScanner.FromIdAsync(string deviceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static string GetDeviceSelector()
		{
			throw new global::System.NotImplementedException("The member string BarcodeScanner.GetDeviceSelector() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.PointOfService.BarcodeScanner, global::Windows.Devices.PointOfService.BarcodeScannerStatusUpdatedEventArgs> StatusUpdated
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.BarcodeScanner", "event TypedEventHandler<BarcodeScanner, BarcodeScannerStatusUpdatedEventArgs> BarcodeScanner.StatusUpdated");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.BarcodeScanner", "event TypedEventHandler<BarcodeScanner, BarcodeScannerStatusUpdatedEventArgs> BarcodeScanner.StatusUpdated");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
