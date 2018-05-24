#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.Http
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum HttpStatusCode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Continue,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SwitchingProtocols,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Processing,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ok,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Created,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Accepted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NonAuthoritativeInformation,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoContent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ResetContent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PartialContent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MultiStatus,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AlreadyReported,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IMUsed,
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
		PermanentRedirect,
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
		UnprocessableEntity,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Locked,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FailedDependency,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UpgradeRequired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PreconditionRequired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TooManyRequests,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RequestHeaderFieldsTooLarge,
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
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		VariantAlsoNegotiates,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InsufficientStorage,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LoopDetected,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotExtended,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkAuthenticationRequired,
		#endif
	}
	#endif
}
