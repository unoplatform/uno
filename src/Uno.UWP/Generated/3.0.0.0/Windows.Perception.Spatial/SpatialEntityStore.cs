#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Perception.Spatial
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpatialEntityStore 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SpatialEntityStore.IsSupported is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SaveAsync( global::Windows.Perception.Spatial.SpatialEntity entity)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction SpatialEntityStore.SaveAsync(SpatialEntity entity) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction RemoveAsync( global::Windows.Perception.Spatial.SpatialEntity entity)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction SpatialEntityStore.RemoveAsync(SpatialEntity entity) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Perception.Spatial.SpatialEntityWatcher CreateEntityWatcher()
		{
			throw new global::System.NotImplementedException("The member SpatialEntityWatcher SpatialEntityStore.CreateEntityWatcher() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Perception.Spatial.SpatialEntityStore.IsSupported.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Perception.Spatial.SpatialEntityStore TryGet( global::Windows.System.RemoteSystems.RemoteSystemSession session)
		{
			throw new global::System.NotImplementedException("The member SpatialEntityStore SpatialEntityStore.TryGet(RemoteSystemSession session) is not implemented in Uno.");
		}
		#endif
	}
}
