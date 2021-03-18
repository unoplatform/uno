#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.SmartCards
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CardRemovedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.SmartCards.SmartCard SmartCard
		{
			get
			{
				throw new global::System.NotImplementedException("The member SmartCard CardRemovedEventArgs.SmartCard is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.SmartCards.CardRemovedEventArgs.SmartCard.get
	}
}
