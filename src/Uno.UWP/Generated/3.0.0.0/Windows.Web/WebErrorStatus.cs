#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web
{
	#if false
	public   enum WebErrorStatus 
	{
		#if false
		Unknown = 0,
		#endif
		#if false
		CertificateCommonNameIsIncorrect = 1,
		#endif
		#if false
		CertificateExpired = 2,
		#endif
		#if false
		CertificateContainsErrors = 3,
		#endif
		#if false
		CertificateRevoked = 4,
		#endif
		#if false
		CertificateIsInvalid = 5,
		#endif
		#if false
		ServerUnreachable = 6,
		#endif
		#if false
		Timeout = 7,
		#endif
		#if false
		ErrorHttpInvalidServerResponse = 8,
		#endif
		#if false
		ConnectionAborted = 9,
		#endif
		#if false
		ConnectionReset = 10,
		#endif
		#if false
		Disconnected = 11,
		#endif
		#if false
		HttpToHttpsOnRedirection = 12,
		#endif
		#if false
		HttpsToHttpOnRedirection = 13,
		#endif
		#if false
		CannotConnect = 14,
		#endif
		#if false
		HostNameNotResolved = 15,
		#endif
		#if false
		OperationCanceled = 16,
		#endif
		#if false
		RedirectFailed = 17,
		#endif
		#if false
		UnexpectedStatusCode = 18,
		#endif
		#if false
		UnexpectedRedirection = 19,
		#endif
		#if false
		UnexpectedClientError = 20,
		#endif
		#if false
		UnexpectedServerError = 21,
		#endif
		#if false
		InsufficientRangeSupport = 22,
		#endif
		#if false
		MissingContentLengthSupport = 23,
		#endif
		#if false
		MultipleChoices = 300,
		#endif
		#if false
		MovedPermanently = 301,
		#endif
		#if false
		Found = 302,
		#endif
		#if false
		SeeOther = 303,
		#endif
		#if false
		NotModified = 304,
		#endif
		#if false
		UseProxy = 305,
		#endif
		#if false
		TemporaryRedirect = 307,
		#endif
		#if false
		BadRequest = 400,
		#endif
		#if false
		Unauthorized = 401,
		#endif
		#if false
		PaymentRequired = 402,
		#endif
		#if false
		Forbidden = 403,
		#endif
		#if false
		NotFound = 404,
		#endif
		#if false
		MethodNotAllowed = 405,
		#endif
		#if false
		NotAcceptable = 406,
		#endif
		#if false
		ProxyAuthenticationRequired = 407,
		#endif
		#if false
		RequestTimeout = 408,
		#endif
		#if false
		Conflict = 409,
		#endif
		#if false
		Gone = 410,
		#endif
		#if false
		LengthRequired = 411,
		#endif
		#if false
		PreconditionFailed = 412,
		#endif
		#if false
		RequestEntityTooLarge = 413,
		#endif
		#if false
		RequestUriTooLong = 414,
		#endif
		#if false
		UnsupportedMediaType = 415,
		#endif
		#if false
		RequestedRangeNotSatisfiable = 416,
		#endif
		#if false
		ExpectationFailed = 417,
		#endif
		#if false
		InternalServerError = 500,
		#endif
		#if false
		NotImplemented = 501,
		#endif
		#if false
		BadGateway = 502,
		#endif
		#if false
		ServiceUnavailable = 503,
		#endif
		#if false
		GatewayTimeout = 504,
		#endif
		#if false
		HttpVersionNotSupported = 505,
		#endif
	}
	#endif
}
