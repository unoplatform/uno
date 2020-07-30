#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EqualizerEffectDefinition : global::Windows.Media.Effects.IAudioEffectDefinition
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Audio.EqualizerBand> Bands
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<EqualizerBand> EqualizerEffectDefinition.Bands is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ActivatableClassId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EqualizerEffectDefinition.ActivatableClassId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Collections.IPropertySet Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IPropertySet EqualizerEffectDefinition.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public EqualizerEffectDefinition( global::Windows.Media.Audio.AudioGraph audioGraph) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.EqualizerEffectDefinition", "EqualizerEffectDefinition.EqualizerEffectDefinition(AudioGraph audioGraph)");
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.EqualizerEffectDefinition.EqualizerEffectDefinition(Windows.Media.Audio.AudioGraph)
		// Forced skipping of method Windows.Media.Audio.EqualizerEffectDefinition.Bands.get
		// Forced skipping of method Windows.Media.Audio.EqualizerEffectDefinition.ActivatableClassId.get
		// Forced skipping of method Windows.Media.Audio.EqualizerEffectDefinition.Properties.get
		// Processing: Windows.Media.Effects.IAudioEffectDefinition
	}
}
