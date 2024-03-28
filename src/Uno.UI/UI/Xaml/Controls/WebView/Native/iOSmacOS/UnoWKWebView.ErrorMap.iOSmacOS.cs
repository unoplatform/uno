using Foundation;
using System.Collections.Generic;
using Windows.Web;
using Microsoft.Web.WebView2.Core;

namespace Windows.UI.Xaml.Controls;

partial class UnoWKWebView
{
	private Dictionary<NSUrlError, CoreWebView2WebErrorStatus> _errorMap = new()
	{
		[NSUrlError.DownloadDecodingFailedToComplete] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.DownloadDecodingFailedMidStream] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.CannotMoveFile] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.CannotRemoveFile] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.CannotWriteToFile] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.CannotCloseFile] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.CannotOpenFile] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.CannotCreateFile] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.CannotLoadFromNetwork] = CoreWebView2WebErrorStatus.ServerUnreachable,

		[NSUrlError.ClientCertificateRequired] = CoreWebView2WebErrorStatus.ValidAuthenticationCredentialsRequired,
		[NSUrlError.ClientCertificateRejected] = CoreWebView2WebErrorStatus.CertificateIsInvalid,
		[NSUrlError.ServerCertificateNotYetValid] = CoreWebView2WebErrorStatus.CertificateIsInvalid,
		[NSUrlError.ServerCertificateHasUnknownRoot] = CoreWebView2WebErrorStatus.CertificateIsInvalid,
		[NSUrlError.ServerCertificateUntrusted] = CoreWebView2WebErrorStatus.CertificateCommonNameIsIncorrect,
		[NSUrlError.ServerCertificateHasBadDate] = CoreWebView2WebErrorStatus.CertificateExpired,
		[NSUrlError.SecureConnectionFailed] = CoreWebView2WebErrorStatus.ServerUnreachable,

		[NSUrlError.DataLengthExceedsMaximum] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.NoPermissionsToReadFile] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.FileIsDirectory] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.FileDoesNotExist] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.AppTransportSecurityRequiresSecureConnection] = CoreWebView2WebErrorStatus.CannotConnect,
		[NSUrlError.RequestBodyStreamExhausted] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.DataNotAllowed] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.CallIsActive] = CoreWebView2WebErrorStatus.UnexpectedError,

		[NSUrlError.InternationalRoamingOff] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.CannotParseResponse] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.CannotDecodeContentData] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.CannotDecodeRawData] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.ZeroByteResource] = CoreWebView2WebErrorStatus.UnexpectedError,

		[NSUrlError.UserAuthenticationRequired] = CoreWebView2WebErrorStatus.ValidAuthenticationCredentialsRequired,
		[NSUrlError.UserCancelledAuthentication] = CoreWebView2WebErrorStatus.UnexpectedError,

		[NSUrlError.BadServerResponse] = CoreWebView2WebErrorStatus.ErrorHttpInvalidServerResponse,
		[NSUrlError.RedirectToNonExistentLocation] = CoreWebView2WebErrorStatus.RedirectFailed,
		[NSUrlError.NotConnectedToInternet] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.ResourceUnavailable] = CoreWebView2WebErrorStatus.ServerUnreachable,
		[NSUrlError.HTTPTooManyRedirects] = CoreWebView2WebErrorStatus.RedirectFailed,
		[NSUrlError.DNSLookupFailed] = CoreWebView2WebErrorStatus.HostNameNotResolved,
		[NSUrlError.NetworkConnectionLost] = CoreWebView2WebErrorStatus.ServerUnreachable,
		[NSUrlError.CannotConnectToHost] = CoreWebView2WebErrorStatus.ServerUnreachable,
		[NSUrlError.CannotFindHost] = CoreWebView2WebErrorStatus.HostNameNotResolved,
		[NSUrlError.UnsupportedURL] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.TimedOut] = CoreWebView2WebErrorStatus.Timeout,
		[NSUrlError.BadURL] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.Cancelled] = CoreWebView2WebErrorStatus.OperationCanceled,

		[NSUrlError.BackgroundSessionWasDisconnected] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.BackgroundSessionInUseByAnotherProcess] = CoreWebView2WebErrorStatus.UnexpectedError,
		[NSUrlError.BackgroundSessionRequiresSharedContainer] = CoreWebView2WebErrorStatus.UnexpectedError,
	};
}
