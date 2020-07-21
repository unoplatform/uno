#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Effects
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AudioEffectDefinition : global::Windows.Media.Effects.IAudioEffectDefinition
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ActivatableClassId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AudioEffectDefinition.ActivatableClassId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Collections.IPropertySet Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IPropertySet AudioEffectDefinition.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AudioEffectDefinition( string activatableClassId) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Effects.AudioEffectDefinition", "AudioEffectDefinition.AudioEffectDefinition(string activatableClassId)");
		}
		#endif
		// Forced skipping of method Windows.Media.Effects.AudioEffectDefinition.AudioEffectDefinition(string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AudioEffectDefinition( string activatableClassId,  global::Windows.Foundation.Collections.IPropertySet props) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Effects.AudioEffectDefinition", "AudioEffectDefinition.AudioEffectDefinition(string activatableClassId, IPropertySet props)");
		}
		#endif
		// Forced skipping of method Windows.Media.Effects.AudioEffectDefinition.AudioEffectDefinition(string, Windows.Foundation.Collections.IPropertySet)
		// Forced skipping of method Windows.Media.Effects.AudioEffectDefinition.ActivatableClassId.get
		// Forced skipping of method Windows.Media.Effects.AudioEffectDefinition.Properties.get
		// Processing: Windows.Media.Effects.IAudioEffectDefinition
	}
}
