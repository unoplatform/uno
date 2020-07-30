#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LimiterEffectDefinition : global::Windows.Media.Effects.IAudioEffectDefinition
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Release
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint LimiterEffectDefinition.Release is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.LimiterEffectDefinition", "uint LimiterEffectDefinition.Release");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Loudness
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint LimiterEffectDefinition.Loudness is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.LimiterEffectDefinition", "uint LimiterEffectDefinition.Loudness");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ActivatableClassId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string LimiterEffectDefinition.ActivatableClassId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Collections.IPropertySet Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IPropertySet LimiterEffectDefinition.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public LimiterEffectDefinition( global::Windows.Media.Audio.AudioGraph audioGraph) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.LimiterEffectDefinition", "LimiterEffectDefinition.LimiterEffectDefinition(AudioGraph audioGraph)");
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.LimiterEffectDefinition.LimiterEffectDefinition(Windows.Media.Audio.AudioGraph)
		// Forced skipping of method Windows.Media.Audio.LimiterEffectDefinition.Release.set
		// Forced skipping of method Windows.Media.Audio.LimiterEffectDefinition.Release.get
		// Forced skipping of method Windows.Media.Audio.LimiterEffectDefinition.Loudness.set
		// Forced skipping of method Windows.Media.Audio.LimiterEffectDefinition.Loudness.get
		// Forced skipping of method Windows.Media.Audio.LimiterEffectDefinition.ActivatableClassId.get
		// Forced skipping of method Windows.Media.Audio.LimiterEffectDefinition.Properties.get
		// Processing: Windows.Media.Effects.IAudioEffectDefinition
	}
}
