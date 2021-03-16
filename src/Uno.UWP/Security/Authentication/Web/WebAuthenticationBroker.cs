using System;
using Windows.Foundation;
using Uno.Foundation.Extensibility;

namespace Windows.Security.Authentication.Web
{
	public static partial class WebAuthenticationBroker
	{
		private static IWebAuthenticationBrokerProvider _authenticationBrokerProvider;

		static WebAuthenticationBroker()
		{
			ApiExtensibility.CreateInstance(null, out _authenticationBrokerProvider);
		}

		public static Uri GetCurrentApplicationCallbackUri()
		{
			return _authenticationBrokerProvider?.GetCurrentApplicationCallbackUri();
		}

		public static IAsyncOperation<WebAuthenticationResult> AuthenticateAsync(WebAuthenticationOptions options, Uri requestUri, Uri callbackUri)
		{
			if(_authenticationBrokerProvider == null)
			{
				throw new InvalidOperationException("No WebAuthenticationBrokerProvider registered. You need to add reference to authentication add-in in your project.");
			}

			return AsyncOperation
				.FromTask(ct => _authenticationBrokerProvider.AuthenticateAsync(options, requestUri, callbackUri, ct));
		}
	}
}
