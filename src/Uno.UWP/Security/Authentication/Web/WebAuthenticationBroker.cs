using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Uno.AuthenticationBroker;
using Uno.Foundation.Extensibility;

namespace Windows.Security.Authentication.Web
{
	public static partial class WebAuthenticationBroker
	{
		private static readonly IWebAuthenticationBrokerProvider _authenticationBrokerProvider;

		static WebAuthenticationBroker()
		{
			ApiExtensibility.CreateInstance(null!, out _authenticationBrokerProvider!);

			// If no custom extension found, default to internal one.
			if (_authenticationBrokerProvider == null)
			{
				_authenticationBrokerProvider = new WebAuthenticationBrokerProvider();
			}
		}

		public static Uri GetCurrentApplicationCallbackUri()
		{
			return _authenticationBrokerProvider.GetCurrentApplicationCallbackUri();
		}

		public static IAsyncOperation<WebAuthenticationResult> AuthenticateAsync(WebAuthenticationOptions options, Uri requestUri)
		{
			return AuthenticateAsync(options, requestUri, GetCurrentApplicationCallbackUri());
		}

		public static IAsyncOperation<WebAuthenticationResult> AuthenticateAsync(WebAuthenticationOptions options, Uri requestUri, Uri callbackUri)
		{
			Task<WebAuthenticationResult> Builder(CancellationToken ct)
			{
				return _authenticationBrokerProvider.AuthenticateAsync(options, requestUri, callbackUri, ct);
			}

			return AsyncOperation.FromTask(Builder);
		}
	}
}
