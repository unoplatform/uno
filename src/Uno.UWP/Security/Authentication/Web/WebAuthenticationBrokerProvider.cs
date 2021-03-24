#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;
using Uno.Extensions;
using Uno.Logging;

namespace Uno.AuthenticationBroker
{
	public partial class WebAuthenticationBrokerProvider : IWebAuthenticationBrokerProvider
	{
		internal static TimeSpan Timeout => WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout;

		public Uri GetCurrentApplicationCallbackUri()
		{
			var defaultUri = WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultReturnUri;

			if (defaultUri != null)
			{
				// A default one is defined: we're using it instead of using
				// a discovery mechanism.
				return defaultUri;
			}

			var schemes = this.GetApplicationCustomSchemes().ToArray();

			if(schemes.Length == 0)
			{
				throw new InvalidOperationException("No custom scheme found for this application.");
			}

			if (schemes.Length > 1)
			{
				var message =
					"More than one custom scheme is defined for this application.\n" +
					$"Uno will use the first one found ({schemes[0]}), but you may want" +
					"to specify which one to use by setting " +
					"WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultReturnUri.\n" +
					"Found schemes: " + schemes.JoinBy(" ");
				this.Log().Warn(message);
			}

			return new Uri(schemes[0] + WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultCallbackPath);
		}

		public async Task<WebAuthenticationResult> AuthenticateAsync(
			WebAuthenticationOptions options,
			Uri requestUri,
			Uri callbackUri,
			CancellationToken ct)
		{
			return await this.AuthenticateAsyncCore(options, requestUri, callbackUri, ct);
		}

	}
}
