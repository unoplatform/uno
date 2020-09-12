#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Effects
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VideoEffectDefinition : global::Windows.Media.Effects.IVideoEffectDefinition
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ActivatableClassId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string VideoEffectDefinition.ActivatableClassId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Collections.IPropertySet Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IPropertySet VideoEffectDefinition.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public VideoEffectDefinition( string activatableClassId) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Effects.VideoEffectDefinition", "VideoEffectDefinition.VideoEffectDefinition(string activatableClassId)");
		}
		#endif
		// Forced skipping of method Windows.Media.Effects.VideoEffectDefinition.VideoEffectDefinition(string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public VideoEffectDefinition( string activatableClassId,  global::Windows.Foundation.Collections.IPropertySet props) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Effects.VideoEffectDefinition", "VideoEffectDefinition.VideoEffectDefinition(string activatableClassId, IPropertySet props)");
		}
		#endif
		// Forced skipping of method Windows.Media.Effects.VideoEffectDefinition.VideoEffectDefinition(string, Windows.Foundation.Collections.IPropertySet)
		// Forced skipping of method Windows.Media.Effects.VideoEffectDefinition.ActivatableClassId.get
		// Forced skipping of method Windows.Media.Effects.VideoEffectDefinition.Properties.get
		// Processing: Windows.Media.Effects.IVideoEffectDefinition
	}
}
