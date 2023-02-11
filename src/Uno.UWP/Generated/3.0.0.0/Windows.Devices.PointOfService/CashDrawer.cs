#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CashDrawer : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.PointOfService.CashDrawerCapabilities Capabilities
		{
			get
			{
				throw new global::System.NotImplementedException("The member CashDrawerCapabilities CashDrawer.Capabilities is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CashDrawerCapabilities%20CashDrawer.Capabilities");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CashDrawer.DeviceId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CashDrawer.DeviceId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.PointOfService.CashDrawerEventSource DrawerEventSource
		{
			get
			{
				throw new global::System.NotImplementedException("The member CashDrawerEventSource CashDrawer.DrawerEventSource is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CashDrawerEventSource%20CashDrawer.DrawerEventSource");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsDrawerOpen
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CashDrawer.IsDrawerOpen is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20CashDrawer.IsDrawerOpen");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.PointOfService.CashDrawerStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member CashDrawerStatus CashDrawer.Status is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CashDrawerStatus%20CashDrawer.Status");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawer.DeviceId.get
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawer.Capabilities.get
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawer.Status.get
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawer.IsDrawerOpen.get
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawer.DrawerEventSource.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.PointOfService.ClaimedCashDrawer> ClaimDrawerAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ClaimedCashDrawer> CashDrawer.ClaimDrawerAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CClaimedCashDrawer%3E%20CashDrawer.ClaimDrawerAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> CheckHealthAsync( global::Windows.Devices.PointOfService.UnifiedPosHealthCheckLevel level)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> CashDrawer.CheckHealthAsync(UnifiedPosHealthCheckLevel level) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cstring%3E%20CashDrawer.CheckHealthAsync%28UnifiedPosHealthCheckLevel%20level%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> GetStatisticsAsync( global::System.Collections.Generic.IEnumerable<string> statisticsCategories)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> CashDrawer.GetStatisticsAsync(IEnumerable<string> statisticsCategories) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cstring%3E%20CashDrawer.GetStatisticsAsync%28IEnumerable%3Cstring%3E%20statisticsCategories%29");
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawer.StatusUpdated.add
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawer.StatusUpdated.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.CashDrawer", "void CashDrawer.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelector( global::Windows.Devices.PointOfService.PosConnectionTypes connectionTypes)
		{
			throw new global::System.NotImplementedException("The member string CashDrawer.GetDeviceSelector(PosConnectionTypes connectionTypes) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CashDrawer.GetDeviceSelector%28PosConnectionTypes%20connectionTypes%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.PointOfService.CashDrawer> GetDefaultAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CashDrawer> CashDrawer.GetDefaultAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CCashDrawer%3E%20CashDrawer.GetDefaultAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.PointOfService.CashDrawer> FromIdAsync( string deviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CashDrawer> CashDrawer.FromIdAsync(string deviceId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CCashDrawer%3E%20CashDrawer.FromIdAsync%28string%20deviceId%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelector()
		{
			throw new global::System.NotImplementedException("The member string CashDrawer.GetDeviceSelector() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CashDrawer.GetDeviceSelector%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.PointOfService.CashDrawer, global::Windows.Devices.PointOfService.CashDrawerStatusUpdatedEventArgs> StatusUpdated
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.CashDrawer", "event TypedEventHandler<CashDrawer, CashDrawerStatusUpdatedEventArgs> CashDrawer.StatusUpdated");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.CashDrawer", "event TypedEventHandler<CashDrawer, CashDrawerStatusUpdatedEventArgs> CashDrawer.StatusUpdated");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
