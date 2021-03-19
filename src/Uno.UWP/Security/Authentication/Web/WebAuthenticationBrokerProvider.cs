#nullable enable
using System;
using System.Linq;

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

			var schemes = GetCustomSchemes().ToArray();

			if(schemes.Length == 0)
			{
				throw new InvalidOperationException("No custom scheme found for this application.");
			}

			if (schemes.Length > 1)
			{
				throw new InvalidOperationException(
					"More than one custom scheme is defined for this application. " +
					"You must specify the one you want to use using WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultReturnUri.");
			}

			return new Uri(schemes[0] + WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultCallbackPath);
		}
	}
}
