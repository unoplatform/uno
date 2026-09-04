using System;
using System.Collections.Specialized;
using System.Web;
using Uno.Foundation.Logging;

namespace Uno.Helpers
{
	public static partial class ProtocolActivation
	{
		/// <summary>
		/// The query-string key Uno Platform transports a protocol activation through on WebAssembly,
		/// where <c>navigator.registerProtocolHandler</c> can only deliver one by navigating the page.
		/// </summary>
		internal const string QueryKey = "unoprotocolactivation";

		/// <summary>
		/// Extracts the protocol activation URI from a browser query string.
		/// </summary>
		/// <param name="queryArguments">The query string, without its leading '?'.</param>
		/// <param name="uri">The activation URI, when one is present.</param>
		/// <param name="remainingArguments">
		/// <paramref name="queryArguments"/> with the activation key removed, so that the app's own
		/// launch arguments do not carry Uno's transport detail.
		/// </param>
		internal static bool TryParseActivationUri(string queryArguments, out Uri uri, out string remainingArguments)
		{
			NameValueCollection queryValues = null;
			uri = null;
			remainingArguments = queryArguments;
			try
			{
				queryValues = HttpUtility.ParseQueryString(queryArguments);
			}
			catch (Exception ex)
			{
				typeof(ProtocolActivation).Log().LogError(
					"Launch arguments could not be parsed as a query string", ex);
			}

			if (queryValues != null &&
				queryValues[QueryKey] is string protocolUriString)
			{
				// ParseQueryString has already decoded the value; unescaping again would corrupt a
				// URI that legitimately contains an encoded '%' or '/'.
				if (Uri.TryCreate(protocolUriString, UriKind.Absolute, out uri))
				{
					queryValues.Remove(QueryKey);
					remainingArguments = queryValues.ToString();
					return true;
				}
				else
				{
					// Length rather than the value: an activation URI routinely carries an OAuth code.
					typeof(ProtocolActivation).Log().LogError(
						$"The '{QueryKey}' query value is {protocolUriString.Length} characters but is not an absolute URI.");
				}
			}
			else if (typeof(ProtocolActivation).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(ProtocolActivation).Log().LogDebug(
					$"The launch query carries no '{QueryKey}' key, so this is not a protocol activation.");
			}

			return false;
		}
	}
}
