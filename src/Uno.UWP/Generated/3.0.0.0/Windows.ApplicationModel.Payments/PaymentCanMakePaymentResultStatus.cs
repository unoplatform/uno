#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Payments
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PaymentCanMakePaymentResultStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Yes,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		No,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotAllowed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UserNotSignedIn,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SpecifiedPaymentMethodIdsNotSupported,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoQualifyingCardOnFile,
		#endif
	}
	#endif
}
