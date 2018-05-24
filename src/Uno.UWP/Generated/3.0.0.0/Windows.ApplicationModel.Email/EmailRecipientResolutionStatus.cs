#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum EmailRecipientResolutionStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Success,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RecipientNotFound,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AmbiguousRecipient,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoCertificate,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CertificateRequestLimitReached,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CannotResolveDistributionList,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ServerError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnknownFailure,
		#endif
	}
	#endif
}
