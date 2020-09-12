#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SceneAnalysisEffectDefinition : global::Windows.Media.Effects.IVideoEffectDefinition
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ActivatableClassId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SceneAnalysisEffectDefinition.ActivatableClassId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Collections.IPropertySet Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IPropertySet SceneAnalysisEffectDefinition.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public SceneAnalysisEffectDefinition() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.SceneAnalysisEffectDefinition", "SceneAnalysisEffectDefinition.SceneAnalysisEffectDefinition()");
		}
		#endif
		// Forced skipping of method Windows.Media.Core.SceneAnalysisEffectDefinition.SceneAnalysisEffectDefinition()
		// Forced skipping of method Windows.Media.Core.SceneAnalysisEffectDefinition.ActivatableClassId.get
		// Forced skipping of method Windows.Media.Core.SceneAnalysisEffectDefinition.Properties.get
		// Processing: Windows.Media.Effects.IVideoEffectDefinition
	}
}
