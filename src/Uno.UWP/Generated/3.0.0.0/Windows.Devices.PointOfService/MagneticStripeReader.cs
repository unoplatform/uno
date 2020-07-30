#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MagneticStripeReader : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.PointOfService.MagneticStripeReaderCapabilities Capabilities
		{
			get
			{
				throw new global::System.NotImplementedException("The member MagneticStripeReaderCapabilities MagneticStripeReader.Capabilities is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.PointOfService.MagneticStripeReaderAuthenticationProtocol DeviceAuthenticationProtocol
		{
			get
			{
				throw new global::System.NotImplementedException("The member MagneticStripeReaderAuthenticationProtocol MagneticStripeReader.DeviceAuthenticationProtocol is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string MagneticStripeReader.DeviceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint[] SupportedCardTypes
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint[] MagneticStripeReader.SupportedCardTypes is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.MagneticStripeReader.DeviceId.get
		// Forced skipping of method Windows.Devices.PointOfService.MagneticStripeReader.Capabilities.get
		// Forced skipping of method Windows.Devices.PointOfService.MagneticStripeReader.SupportedCardTypes.get
		// Forced skipping of method Windows.Devices.PointOfService.MagneticStripeReader.DeviceAuthenticationProtocol.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> CheckHealthAsync( global::Windows.Devices.PointOfService.UnifiedPosHealthCheckLevel level)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> MagneticStripeReader.CheckHealthAsync(UnifiedPosHealthCheckLevel level) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.PointOfService.ClaimedMagneticStripeReader> ClaimReaderAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ClaimedMagneticStripeReader> MagneticStripeReader.ClaimReaderAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IBuffer> RetrieveStatisticsAsync( global::System.Collections.Generic.IEnumerable<string> statisticsCategories)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IBuffer> MagneticStripeReader.RetrieveStatisticsAsync(IEnumerable<string> statisticsCategories) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.PointOfService.MagneticStripeReaderErrorReportingType GetErrorReportingType()
		{
			throw new global::System.NotImplementedException("The member MagneticStripeReaderErrorReportingType MagneticStripeReader.GetErrorReportingType() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.MagneticStripeReader.StatusUpdated.add
		// Forced skipping of method Windows.Devices.PointOfService.MagneticStripeReader.StatusUpdated.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.MagneticStripeReader", "void MagneticStripeReader.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelector( global::Windows.Devices.PointOfService.PosConnectionTypes connectionTypes)
		{
			throw new global::System.NotImplementedException("The member string MagneticStripeReader.GetDeviceSelector(PosConnectionTypes connectionTypes) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.PointOfService.MagneticStripeReader> GetDefaultAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MagneticStripeReader> MagneticStripeReader.GetDefaultAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.PointOfService.MagneticStripeReader> FromIdAsync( string deviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MagneticStripeReader> MagneticStripeReader.FromIdAsync(string deviceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelector()
		{
			throw new global::System.NotImplementedException("The member string MagneticStripeReader.GetDeviceSelector() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.PointOfService.MagneticStripeReader, global::Windows.Devices.PointOfService.MagneticStripeReaderStatusUpdatedEventArgs> StatusUpdated
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.MagneticStripeReader", "event TypedEventHandler<MagneticStripeReader, MagneticStripeReaderStatusUpdatedEventArgs> MagneticStripeReader.StatusUpdated");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.MagneticStripeReader", "event TypedEventHandler<MagneticStripeReader, MagneticStripeReaderStatusUpdatedEventArgs> MagneticStripeReader.StatusUpdated");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
