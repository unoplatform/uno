#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Perception.People
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HandMeshVertexState 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Perception.Spatial.SpatialCoordinateSystem CoordinateSystem
		{
			get
			{
				throw new global::System.NotImplementedException("The member SpatialCoordinateSystem HandMeshVertexState.CoordinateSystem is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Perception.PerceptionTimestamp UpdateTimestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member PerceptionTimestamp HandMeshVertexState.UpdateTimestamp is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Perception.People.HandMeshVertexState.CoordinateSystem.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void GetVertices( global::Windows.Perception.People.HandMeshVertex[] vertices)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Perception.People.HandMeshVertexState", "void HandMeshVertexState.GetVertices(HandMeshVertex[] vertices)");
		}
		#endif
		// Forced skipping of method Windows.Perception.People.HandMeshVertexState.UpdateTimestamp.get
	}
}
