#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum WebErrorStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CertificateCommonNameIsIncorrect,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CertificateExpired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CertificateContainsErrors,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CertificateRevoked,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CertificateIsInvalid,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ServerUnreachable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Timeout,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ErrorHttpInvalidServerResponse,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConnectionAborted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConnectionReset,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Disconnected,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HttpToHttpsOnRedirection,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HttpsToHttpOnRedirection,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CannotConnect,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HostNameNotResolved,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OperationCanceled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RedirectFailed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnexpectedStatusCode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnexpectedRedirection,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnexpectedClientError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnexpectedServerError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InsufficientRangeSupport,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MissingContentLengthSupport,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MultipleChoices,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MovedPermanently,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Found,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SeeOther,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotModified,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UseProxy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TemporaryRedirect,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BadRequest,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unauthorized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PaymentRequired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Forbidden,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotFound,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MethodNotAllowed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotAcceptable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProxyAuthenticationRequired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RequestTimeout,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Conflict,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Gone,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LengthRequired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PreconditionFailed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RequestEntityTooLarge,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RequestUriTooLong,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnsupportedMediaType,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RequestedRangeNotSatisfiable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ExpectationFailed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InternalServerError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotImplemented,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BadGateway,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ServiceUnavailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GatewayTimeout,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HttpVersionNotSupported,
		#endif
	}
	#endif
}
