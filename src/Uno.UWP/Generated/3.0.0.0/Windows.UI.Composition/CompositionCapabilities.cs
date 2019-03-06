#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CompositionCapabilities 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool AreEffectsSupported()
		{
			throw new global::System.NotImplementedException("The member bool CompositionCapabilities.AreEffectsSupported() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool AreEffectsFast()
		{
			throw new global::System.NotImplementedException("The member bool CompositionCapabilities.AreEffectsFast() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.CompositionCapabilities.Changed.add
		// Forced skipping of method Windows.UI.Composition.CompositionCapabilities.Changed.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Composition.CompositionCapabilities GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member CompositionCapabilities CompositionCapabilities.GetForCurrentView() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Composition.CompositionCapabilities, object> Changed
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionCapabilities", "event TypedEventHandler<CompositionCapabilities, object> CompositionCapabilities.Changed");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionCapabilities", "event TypedEventHandler<CompositionCapabilities, object> CompositionCapabilities.Changed");
			}
		}
		#endif
	}
}
