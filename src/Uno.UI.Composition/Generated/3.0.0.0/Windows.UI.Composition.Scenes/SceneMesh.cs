#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition.Scenes
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SceneMesh : global::Windows.UI.Composition.Scenes.SceneObject
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.DirectX.DirectXPrimitiveTopology PrimitiveTopology
		{
			get
			{
				throw new global::System.NotImplementedException("The member DirectXPrimitiveTopology SceneMesh.PrimitiveTopology is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.Scenes.SceneMesh", "DirectXPrimitiveTopology SceneMesh.PrimitiveTopology");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Composition.Scenes.SceneBoundingBox Bounds
		{
			get
			{
				throw new global::System.NotImplementedException("The member SceneBoundingBox SceneMesh.Bounds is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.Scenes.SceneMesh.Bounds.get
		// Forced skipping of method Windows.UI.Composition.Scenes.SceneMesh.PrimitiveTopology.get
		// Forced skipping of method Windows.UI.Composition.Scenes.SceneMesh.PrimitiveTopology.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void FillMeshAttribute( global::Windows.UI.Composition.Scenes.SceneAttributeSemantic semantic,  global::Windows.Graphics.DirectX.DirectXPixelFormat format,  global::Windows.Foundation.MemoryBuffer memory)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.Scenes.SceneMesh", "void SceneMesh.FillMeshAttribute(SceneAttributeSemantic semantic, DirectXPixelFormat format, MemoryBuffer memory)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Composition.Scenes.SceneMesh Create( global::Windows.UI.Composition.Compositor compositor)
		{
			throw new global::System.NotImplementedException("The member SceneMesh SceneMesh.Create(Compositor compositor) is not implemented in Uno.");
		}
		#endif
	}
}
