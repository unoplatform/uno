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
			throw new global::System.NotImplementedException("The member IAsyncOperation<SpatialAnchorExportSufficiency> SpatialAnchorExporter.GetAnchorExportSufficiencyAsync(SpatialAnchor anchor, SpatialAnchorExportPurpose purpose) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryExportAnchorAsync( global::Windows.Perception.Spatial.SpatialAnchor anchor,  global::Windows.Perception.Spatial.SpatialAnchorExportPurpose purpose,  global::Windows.Storage.Streams.IOutputStream stream)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> SpatialAnchorExporter.TryExportAnchorAsync(SpatialAnchor anchor, SpatialAnchorExportPurpose purpose, IOutputStream stream) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Perception.Spatial.SpatialAnchorExporter GetDefault()
		{
			throw new global::System.NotImplementedException("The member SpatialAnchorExporter SpatialAnchorExporter.GetDefault() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Perception.Spatial.SpatialPerceptionAccessStatus> RequestAccessAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SpatialPerceptionAccessStatus> SpatialAnchorExporter.RequestAccessAsync() is not implemented in Uno.");
		}
		#endif
	}
}
