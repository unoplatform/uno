#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Windows.Security.Authentication.Web;

namespace Uno.UI.MSAL;

internal sealed class WasmWebUi : ICustomWebUi
{
	internal static readonly WasmWebUi Instance = new();

	public async Task<Uri> AcquireAuthorizationCodeAsync(Uri authorizationUri, Uri redirectUri, CancellationToken cancellationToken)
	{
		var result = await WebAuthenticationBroker
			.AuthenticateAsync(WebAuthenticationOptions.None, authorizationUri, redirectUri)
			.AsTask(cancellationToken)
			// The browser broker does not observe cancellation yet, so don't wait for the popup to close.
			.WaitAsync(cancellationToken);

		return result switch
		{
			{ ResponseStatus: WebAuthenticationStatus.Success, ResponseData: { } responseUri } => new Uri(responseUri),
			// MSAL reports an OperationCanceledException as authentication_canceled.
			{ ResponseStatus: WebAuthenticationStatus.UserCancel } => throw new OperationCanceledException(),
			_ => throw new MsalClientException(MsalError.AuthenticationUiFailedError, result.ResponseData ?? "The browser sign-in popup failed."),
		};
	}
}
