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
		private static readonly IWebAuthenticationBrokerProvider _authenticationBrokerProvider = InitializeAuthenticationBrokerProvider();

		private static IWebAuthenticationBrokerProvider InitializeAuthenticationBrokerProvider()
		{
			ApiExtensibility.CreateInstance(null, out IWebAuthenticationBrokerProvider authenticationBrokerProvider);

			// If no custom extension found, default to internal one.
			if (authenticationBrokerProvider == null)
			{
				authenticationBrokerProvider = new WebAuthenticationBrokerProvider();
			}

			return authenticationBrokerProvider;
		}

		public static Uri GetCurrentApplicationCallbackUri()
		{
			return _authenticationBrokerProvider?.GetCurrentApplicationCallbackUri();
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
