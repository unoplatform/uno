#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Lights.Effects
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LampArrayCustomEffect : global::Windows.Devices.Lights.Effects.ILampArrayEffect
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan UpdateInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan LampArrayCustomEffect.UpdateInterval is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Lights.Effects.LampArrayCustomEffect", "TimeSpan LampArrayCustomEffect.UpdateInterval");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan Duration
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan LampArrayCustomEffect.Duration is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Lights.Effects.LampArrayCustomEffect", "TimeSpan LampArrayCustomEffect.Duration");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int ZIndex
		{
			get
			{
				throw new global::System.NotImplementedException("The member int LampArrayCustomEffect.ZIndex is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Lights.Effects.LampArrayCustomEffect", "int LampArrayCustomEffect.ZIndex");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public LampArrayCustomEffect( global::Windows.Devices.Lights.LampArray lampArray,  int[] lampIndexes) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Lights.Effects.LampArrayCustomEffect", "LampArrayCustomEffect.LampArrayCustomEffect(LampArray lampArray, int[] lampIndexes)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Lights.Effects.LampArrayCustomEffect.LampArrayCustomEffect(Windows.Devices.Lights.LampArray, int[])
		// Forced skipping of method Windows.Devices.Lights.Effects.LampArrayCustomEffect.Duration.get
		// Forced skipping of method Windows.Devices.Lights.Effects.LampArrayCustomEffect.Duration.set
		// Forced skipping of method Windows.Devices.Lights.Effects.LampArrayCustomEffect.UpdateInterval.get
		// Forced skipping of method Windows.Devices.Lights.Effects.LampArrayCustomEffect.UpdateInterval.set
		// Forced skipping of method Windows.Devices.Lights.Effects.LampArrayCustomEffect.UpdateRequested.add
		// Forced skipping of method Windows.Devices.Lights.Effects.LampArrayCustomEffect.UpdateRequested.remove
		// Forced skipping of method Windows.Devices.Lights.Effects.LampArrayCustomEffect.ZIndex.get
		// Forced skipping of method Windows.Devices.Lights.Effects.LampArrayCustomEffect.ZIndex.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Lights.Effects.LampArrayCustomEffect, global::Windows.Devices.Lights.Effects.LampArrayUpdateRequestedEventArgs> UpdateRequested
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Lights.Effects.LampArrayCustomEffect", "event TypedEventHandler<LampArrayCustomEffect, LampArrayUpdateRequestedEventArgs> LampArrayCustomEffect.UpdateRequested");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Lights.Effects.LampArrayCustomEffect", "event TypedEventHandler<LampArrayCustomEffect, LampArrayUpdateRequestedEventArgs> LampArrayCustomEffect.UpdateRequested");
			}
		}
		#endif
		// Processing: Windows.Devices.Lights.Effects.ILampArrayEffect
	}
}
