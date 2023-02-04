#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RegionsOfInterestControl 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool AutoExposureSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool RegionsOfInterestControl.AutoExposureSupported is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20RegionsOfInterestControl.AutoExposureSupported");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool AutoFocusSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool RegionsOfInterestControl.AutoFocusSupported is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20RegionsOfInterestControl.AutoFocusSupported");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool AutoWhiteBalanceSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool RegionsOfInterestControl.AutoWhiteBalanceSupported is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20RegionsOfInterestControl.AutoWhiteBalanceSupported");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint MaxRegions
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint RegionsOfInterestControl.MaxRegions is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20RegionsOfInterestControl.MaxRegions");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.RegionsOfInterestControl.MaxRegions.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SetRegionsAsync( global::System.Collections.Generic.IEnumerable<global::Windows.Media.Devices.RegionOfInterest> regions)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction RegionsOfInterestControl.SetRegionsAsync(IEnumerable<RegionOfInterest> regions) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20RegionsOfInterestControl.SetRegionsAsync%28IEnumerable%3CRegionOfInterest%3E%20regions%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SetRegionsAsync( global::System.Collections.Generic.IEnumerable<global::Windows.Media.Devices.RegionOfInterest> regions,  bool lockValues)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction RegionsOfInterestControl.SetRegionsAsync(IEnumerable<RegionOfInterest> regions, bool lockValues) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20RegionsOfInterestControl.SetRegionsAsync%28IEnumerable%3CRegionOfInterest%3E%20regions%2C%20bool%20lockValues%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ClearRegionsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction RegionsOfInterestControl.ClearRegionsAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20RegionsOfInterestControl.ClearRegionsAsync%28%29");
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.RegionsOfInterestControl.AutoFocusSupported.get
		// Forced skipping of method Windows.Media.Devices.RegionsOfInterestControl.AutoWhiteBalanceSupported.get
		// Forced skipping of method Windows.Media.Devices.RegionsOfInterestControl.AutoExposureSupported.get
	}
}
