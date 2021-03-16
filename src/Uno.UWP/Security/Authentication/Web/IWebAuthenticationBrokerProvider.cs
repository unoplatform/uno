using System;
using System.Threading;
using System.Threading.Tasks;

namespace Windows.Security.Authentication.Web
{
	public interface IWebAuthenticationBrokerProvider
	{
		Uri GetCurrentApplicationCallbackUri();

		Task<WebAuthenticationResult> AuthenticateAsync(WebAuthenticationOptions options, Uri requestUri, Uri callbackUri, CancellationToken ct);
	}
}
