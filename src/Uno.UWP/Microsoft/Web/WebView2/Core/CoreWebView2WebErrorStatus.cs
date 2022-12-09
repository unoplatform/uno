namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Indicates the error status values for web navigations.
/// </summary>
public enum CoreWebView2WebErrorStatus
{
	/// <summary>
	/// Indicates that an unknown error occurred.
	/// </summary>
	Unknown = 0,

	/// <summary>
	/// Indicates that the SSL certificate common name does not match the web address.
	/// </summary>
	CertificateCommonNameIsIncorrect = 1,

	/// <summary>
	/// Indicates that the SSL certificate has expired.
	/// </summary>
	CertificateExpired = 2,

	/// <summary>
	/// Indicates that the SSL client certificate contains errors.
	/// </summary>
	ClientCertificateContainsErrors = 3,

	/// <summary>
	/// Indicates that the SSL certificate has been revoked.
	/// </summary>
	CertificateRevoked = 4,

	/// <summary>
	/// Indicates that the SSL certificate is not valid. The certificate may not match the public key pins for the host name, the certificate is signed by an untrusted authority or using a weak sign algorithm, the certificate claimed DNS names violate name constraints, the certificate contains a weak key, the validity period of the certificate is too long, lack of revocation information or revocation mechanism, non-unique host name, lack of certificate transparency information, or the certificate is chained to a legacy Symantec root.
	/// </summary>
	CertificateIsInvalid = 5,

	/// <summary>
	/// Indicates that the host is unreachable.
	/// </summary>
	ServerUnreachable = 6,

	/// <summary>
	/// Indicates that the connection has timed out.
	/// </summary>
	Timeout = 7,

	/// <summary>
	/// Indicates that the server returned an invalid or unrecognized response.
	/// </summary>
	ErrorHttpInvalidServerResponse = 8,

	/// <summary>
	/// Indicates that the connection was stopped.
	/// </summary>
	ConnectionAborted = 9,

	/// <summary>
	/// Indicates that the connection was reset.
	/// </summary>
	ConnectionReset = 10,

	/// <summary>
	/// Indicates that the Internet connection has been lost.
	/// </summary>
	Disconnected = 11,

	/// <summary>
	/// Indicates that a connection to the destination was not established.
	/// </summary>
	CannotConnect = 12,

	/// <summary>
	/// Indicates that the provided host name was not able to be resolved.
	/// </summary>
	HostNameNotResolved = 13,

	/// <summary>
	/// Indicates that the operation was canceled.
	/// </summary>
	OperationCanceled = 14,

	/// <summary>
	/// Indicates that the request redirect failed.
	/// </summary>
	RedirectFailed = 15,

	/// <summary>
	/// An unexpected error occurred.
	/// </summary>
	UnexpectedError = 16,

	/// <summary>
	/// Indicates that user is prompted with a login, waiting on user action.
	/// Initial navigation to a login site will always return this even if app provides
	/// credential using BasicAuthenticationRequested. HTTP response status code
	/// in this case is 401. See status code reference here: https://developer.mozilla.org/docs/Web/HTTP/Status.
	/// </summary>
	ValidAuthenticationCredentialsRequired = 17,

	/// <summary>
	/// Indicates that user lacks proper authentication credentials for a proxy server.
	/// HTTP response status code in this case is 407. See status code reference
	/// here: https://developer.mozilla.org/docs/Web/HTTP/Status.
	/// </summary>
	ValidProxyAuthenticationRequired = 18,
}
