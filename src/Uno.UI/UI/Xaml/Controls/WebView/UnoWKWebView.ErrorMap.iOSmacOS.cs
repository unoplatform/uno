using CoreGraphics;
using Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using WebKit;
using System.Threading;
using System.Threading.Tasks;
using ObjCRuntime;
using Uno.UI.Web;

namespace Windows.UI.Xaml.Controls;

partial class UnoWKWebView
{
	private Dictionary<NSUrlError, WebErrorStatus> _errorMap = new Dictionary<NSUrlError, WebErrorStatus>
	{
		[NSUrlError.DownloadDecodingFailedToComplete] = WebErrorStatus.UnexpectedClientError,
		[NSUrlError.DownloadDecodingFailedMidStream] = WebErrorStatus.UnexpectedClientError,
		[NSUrlError.CannotMoveFile] = WebErrorStatus.UnexpectedClientError,
		[NSUrlError.CannotRemoveFile] = WebErrorStatus.UnexpectedClientError,
		[NSUrlError.CannotWriteToFile] = WebErrorStatus.UnexpectedClientError,
		[NSUrlError.CannotCloseFile] = WebErrorStatus.UnexpectedClientError,
		[NSUrlError.CannotOpenFile] = WebErrorStatus.UnexpectedClientError,
		[NSUrlError.CannotCreateFile] = WebErrorStatus.UnexpectedClientError,
		[NSUrlError.CannotLoadFromNetwork] = WebErrorStatus.ServerUnreachable,

		[NSUrlError.ClientCertificateRequired] = WebErrorStatus.Unauthorized,
		[NSUrlError.ClientCertificateRejected] = WebErrorStatus.CertificateIsInvalid,
		[NSUrlError.ServerCertificateNotYetValid] = WebErrorStatus.CertificateIsInvalid,
		[NSUrlError.ServerCertificateHasUnknownRoot] = WebErrorStatus.CertificateContainsErrors,
		[NSUrlError.ServerCertificateUntrusted] = WebErrorStatus.CertificateCommonNameIsIncorrect,
		[NSUrlError.ServerCertificateHasBadDate] = WebErrorStatus.CertificateContainsErrors,
		[NSUrlError.SecureConnectionFailed] = WebErrorStatus.ServerUnreachable,

		[NSUrlError.DataLengthExceedsMaximum] = WebErrorStatus.UnexpectedClientError,
		[NSUrlError.NoPermissionsToReadFile] = WebErrorStatus.UnexpectedClientError,
		[NSUrlError.FileIsDirectory] = WebErrorStatus.UnexpectedClientError,
		[NSUrlError.FileDoesNotExist] = WebErrorStatus.UnexpectedClientError,
		[NSUrlError.AppTransportSecurityRequiresSecureConnection] = WebErrorStatus.CannotConnect,
		[NSUrlError.RequestBodyStreamExhausted] = WebErrorStatus.UnexpectedClientError,
		[NSUrlError.DataNotAllowed] = WebErrorStatus.UnexpectedClientError,
		[NSUrlError.CallIsActive] = WebErrorStatus.UnexpectedClientError,

		[NSUrlError.InternationalRoamingOff] = WebErrorStatus.UnexpectedClientError,
		[NSUrlError.CannotParseResponse] = WebErrorStatus.UnexpectedClientError,
		[NSUrlError.CannotDecodeContentData] = WebErrorStatus.UnexpectedClientError,
		[NSUrlError.CannotDecodeRawData] = WebErrorStatus.UnexpectedClientError,
		[NSUrlError.ZeroByteResource] = WebErrorStatus.UnexpectedClientError,

		[NSUrlError.UserAuthenticationRequired] = WebErrorStatus.Unauthorized,
		[NSUrlError.UserCancelledAuthentication] = WebErrorStatus.UnexpectedClientError,

		[NSUrlError.BadServerResponse] = WebErrorStatus.ErrorHttpInvalidServerResponse,
		[NSUrlError.RedirectToNonExistentLocation] = WebErrorStatus.UnexpectedRedirection,
		[NSUrlError.NotConnectedToInternet] = WebErrorStatus.UnexpectedClientError,
		[NSUrlError.ResourceUnavailable] = WebErrorStatus.ServerUnreachable,
		[NSUrlError.HTTPTooManyRedirects] = WebErrorStatus.UnexpectedRedirection,
		[NSUrlError.DNSLookupFailed] = WebErrorStatus.HostNameNotResolved,
		[NSUrlError.NetworkConnectionLost] = WebErrorStatus.ServerUnreachable,
		[NSUrlError.CannotConnectToHost] = WebErrorStatus.ServerUnreachable,
		[NSUrlError.CannotFindHost] = WebErrorStatus.HostNameNotResolved,
		[NSUrlError.UnsupportedURL] = WebErrorStatus.UnexpectedClientError,
		[NSUrlError.TimedOut] = WebErrorStatus.Timeout,
		[NSUrlError.BadURL] = WebErrorStatus.UnexpectedServerError,
		[NSUrlError.Cancelled] = WebErrorStatus.OperationCanceled,

		[NSUrlError.BackgroundSessionWasDisconnected] = WebErrorStatus.UnexpectedServerError,
		[NSUrlError.BackgroundSessionInUseByAnotherProcess] = WebErrorStatus.UnexpectedServerError,
		[NSUrlError.BackgroundSessionRequiresSharedContainer] = WebErrorStatus.UnexpectedServerError,
	};
}
