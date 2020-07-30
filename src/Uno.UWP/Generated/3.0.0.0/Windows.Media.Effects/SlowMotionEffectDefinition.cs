#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Effects
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SlowMotionEffectDefinition : global::Windows.Media.Effects.IVideoEffectDefinition
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double TimeStretchRate
		{
			get
			{
				throw new global::System.NotImplementedException("The member double SlowMotionEffectDefinition.TimeStretchRate is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Effects.SlowMotionEffectDefinition", "double SlowMotionEffectDefinition.TimeStretchRate");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ActivatableClassId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SlowMotionEffectDefinition.ActivatableClassId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Collections.IPropertySet Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IPropertySet SlowMotionEffectDefinition.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public SlowMotionEffectDefinition() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Effects.SlowMotionEffectDefinition", "SlowMotionEffectDefinition.SlowMotionEffectDefinition()");
		}
		#endif
		// Forced skipping of method Windows.Media.Effects.SlowMotionEffectDefinition.SlowMotionEffectDefinition()
		// Forced skipping of method Windows.Media.Effects.SlowMotionEffectDefinition.TimeStretchRate.get
		// Forced skipping of method Windows.Media.Effects.SlowMotionEffectDefinition.TimeStretchRate.set
		// Forced skipping of method Windows.Media.Effects.SlowMotionEffectDefinition.ActivatableClassId.get
		// Forced skipping of method Windows.Media.Effects.SlowMotionEffectDefinition.Properties.get
		// Processing: Windows.Media.Effects.IVideoEffectDefinition
	}
}
