#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Perception.People
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HandMeshObserver 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int ModelId
		{
			get
			{
				throw new global::System.NotImplementedException("The member int HandMeshObserver.ModelId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Perception.People.HandPose NeutralPose
		{
			get
			{
				throw new global::System.NotImplementedException("The member HandPose HandMeshObserver.NeutralPose is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int NeutralPoseVersion
		{
			get
			{
				throw new global::System.NotImplementedException("The member int HandMeshObserver.NeutralPoseVersion is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Spatial.SpatialInteractionSource Source
		{
			get
			{
				throw new global::System.NotImplementedException("The member SpatialInteractionSource HandMeshObserver.Source is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint TriangleIndexCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint HandMeshObserver.TriangleIndexCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint VertexCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint HandMeshObserver.VertexCount is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Perception.People.HandMeshObserver.Source.get
		// Forced skipping of method Windows.Perception.People.HandMeshObserver.TriangleIndexCount.get
		// Forced skipping of method Windows.Perception.People.HandMeshObserver.VertexCount.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void GetTriangleIndices( ushort[] indices)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Perception.People.HandMeshObserver", "void HandMeshObserver.GetTriangleIndices(ushort[] indices)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Perception.People.HandMeshVertexState GetVertexStateForPose( global::Windows.Perception.People.HandPose handPose)
		{
			throw new global::System.NotImplementedException("The member HandMeshVertexState HandMeshObserver.GetVertexStateForPose(HandPose handPose) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Perception.People.HandMeshObserver.NeutralPose.get
		// Forced skipping of method Windows.Perception.People.HandMeshObserver.NeutralPoseVersion.get
		// Forced skipping of method Windows.Perception.People.HandMeshObserver.ModelId.get
	}
}
