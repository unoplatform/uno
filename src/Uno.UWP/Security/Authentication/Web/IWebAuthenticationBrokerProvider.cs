#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;

namespace Uno.AuthenticationBroker
{
	public interface IWebAuthenticationBrokerProvider
	{
		Uri GetCurrentApplicationCallbackUri();

		Task<WebAuthenticationResult> AuthenticateAsync(WebAuthenticationOptions options, Uri requestUri, Uri callbackUri, CancellationToken ct);
	}
}
