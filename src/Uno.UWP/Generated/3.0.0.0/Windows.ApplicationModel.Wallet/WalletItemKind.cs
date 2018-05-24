#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Wallet
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum WalletItemKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Invalid,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Deal,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		General,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PaymentInstrument,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ticket,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BoardingPass,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MembershipCard,
		#endif
	}
	#endif
}
