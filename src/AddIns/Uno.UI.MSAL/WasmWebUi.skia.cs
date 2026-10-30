#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Uno.Foundation.Logging;
using Windows.Security.Authentication.Web;
using LogLevel = Uno.Foundation.Logging.LogLevel;

namespace Uno.UI.MSAL;

internal sealed class WasmWebUi : ICustomWebUi
{
	// The browser broker reports a timeout as ErrorHttp with HTTP 408 Request Timeout.
	private const uint TimeoutErrorDetail = 408;

	internal static readonly WasmWebUi Instance = new();

	public async Task<Uri> AcquireAuthorizationCodeAsync(Uri authorizationUri, Uri redirectUri, CancellationToken cancellationToken)
	{
		var result = await WebAuthenticationBroker
			.AuthenticateAsync(WebAuthenticationOptions.None, authorizationUri, redirectUri)
			.AsTask(cancellationToken);

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Browser sign-in completed with {result.ResponseStatus} ({result.ResponseErrorDetail}).");
		}

		return result switch
		{
			{ ResponseStatus: WebAuthenticationStatus.Success, ResponseData: { } responseUri } => new Uri(responseUri),
			// MSAL reports an OperationCanceledException as authentication_canceled.
			{ ResponseStatus: WebAuthenticationStatus.UserCancel } => throw new OperationCanceledException(),
			_ => throw CreateFailure(result),
		};
	}

	private MsalClientException CreateFailure(WebAuthenticationResult result)
	{
		if (this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error($"Browser sign-in failed with {result.ResponseStatus} ({result.ResponseErrorDetail}).");
		}

		var message = result.ResponseErrorDetail == TimeoutErrorDetail
			? "The browser sign-in popup timed out."
			: result.ResponseData ?? "The browser sign-in popup failed.";

		return new MsalClientException(MsalError.AuthenticationUiFailedError, message);
	}
}
