#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Perception.Spatial
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpatialAnchorExporter 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Perception.Spatial.SpatialAnchorExportSufficiency> GetAnchorExportSufficiencyAsync( global::Windows.Perception.Spatial.SpatialAnchor anchor,  global::Windows.Perception.Spatial.SpatialAnchorExportPurpose purpose)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SpatialAnchorExportSufficiency> SpatialAnchorExporter.GetAnchorExportSufficiencyAsync(SpatialAnchor anchor, SpatialAnchorExportPurpose purpose) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CSpatialAnchorExportSufficiency%3E%20SpatialAnchorExporter.GetAnchorExportSufficiencyAsync%28SpatialAnchor%20anchor%2C%20SpatialAnchorExportPurpose%20purpose%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryExportAnchorAsync( global::Windows.Perception.Spatial.SpatialAnchor anchor,  global::Windows.Perception.Spatial.SpatialAnchorExportPurpose purpose,  global::Windows.Storage.Streams.IOutputStream stream)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> SpatialAnchorExporter.TryExportAnchorAsync(SpatialAnchor anchor, SpatialAnchorExportPurpose purpose, IOutputStream stream) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20SpatialAnchorExporter.TryExportAnchorAsync%28SpatialAnchor%20anchor%2C%20SpatialAnchorExportPurpose%20purpose%2C%20IOutputStream%20stream%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Perception.Spatial.SpatialAnchorExporter GetDefault()
		{
			throw new global::System.NotImplementedException("The member SpatialAnchorExporter SpatialAnchorExporter.GetDefault() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SpatialAnchorExporter%20SpatialAnchorExporter.GetDefault%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Perception.Spatial.SpatialPerceptionAccessStatus> RequestAccessAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SpatialPerceptionAccessStatus> SpatialAnchorExporter.RequestAccessAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CSpatialPerceptionAccessStatus%3E%20SpatialAnchorExporter.RequestAccessAsync%28%29");
		}
		#endif
	}
}
