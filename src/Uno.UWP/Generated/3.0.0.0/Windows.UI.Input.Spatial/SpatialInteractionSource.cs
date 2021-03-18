#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Spatial
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpatialInteractionSource 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint SpatialInteractionSource.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Spatial.SpatialInteractionSourceKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member SpatialInteractionSourceKind SpatialInteractionSource.Kind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Spatial.SpatialInteractionController Controller
		{
			get
			{
				throw new global::System.NotImplementedException("The member SpatialInteractionController SpatialInteractionSource.Controller is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsGraspSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SpatialInteractionSource.IsGraspSupported is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsMenuSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SpatialInteractionSource.IsMenuSupported is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsPointingSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SpatialInteractionSource.IsPointingSupported is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Spatial.SpatialInteractionSourceHandedness Handedness
		{
			get
			{
				throw new global::System.NotImplementedException("The member SpatialInteractionSourceHandedness SpatialInteractionSource.Handedness is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Spatial.SpatialInteractionSource.Id.get
		// Forced skipping of method Windows.UI.Input.Spatial.SpatialInteractionSource.Kind.get
		// Forced skipping of method Windows.UI.Input.Spatial.SpatialInteractionSource.IsPointingSupported.get
		// Forced skipping of method Windows.UI.Input.Spatial.SpatialInteractionSource.IsMenuSupported.get
		// Forced skipping of method Windows.UI.Input.Spatial.SpatialInteractionSource.IsGraspSupported.get
		// Forced skipping of method Windows.UI.Input.Spatial.SpatialInteractionSource.Controller.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Spatial.SpatialInteractionSourceState TryGetStateAtTimestamp( global::Windows.Perception.PerceptionTimestamp timestamp)
		{
			throw new global::System.NotImplementedException("The member SpatialInteractionSourceState SpatialInteractionSource.TryGetStateAtTimestamp(PerceptionTimestamp timestamp) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Spatial.SpatialInteractionSource.Handedness.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Perception.People.HandMeshObserver TryCreateHandMeshObserver()
		{
			throw new global::System.NotImplementedException("The member HandMeshObserver SpatialInteractionSource.TryCreateHandMeshObserver() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Perception.People.HandMeshObserver> TryCreateHandMeshObserverAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<HandMeshObserver> SpatialInteractionSource.TryCreateHandMeshObserverAsync() is not implemented in Uno.");
		}
		#endif
	}
}
