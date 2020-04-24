#if __WASM__
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Microsoft.Extensions.Logging;
using Uno.Extensions;

namespace Uno.Helpers
{
	public static class ProtocolActivation
	{
		internal const string QueryKey = "unoprotocolactivation";

		/// <summary>
		/// Registers a custom URI scheme for protocol activation on WASM.
		/// </summary>
		/// <param name="scheme">Scheme (must start with web+, after which must follow one or more lowercase ASCII letter).</param>
		/// <param name="domain">Domain on which your application is running.</param>
		/// <param name="prompt">Prompt to show to the user.</param>
		public static void RegisterCustomScheme(string scheme, Uri domain, string prompt)
		{
			// rules as per https://developer.mozilla.org/en-US/docs/Web/API/Navigator/registerProtocolHandler

			// The custom scheme's name begins with web+
			if (scheme.IndexOf("web+", System.StringComparison.InvariantCulture) != 0)
			{
				throw new ArgumentException(
					"Scheme must start with 'web+'",
					nameof(scheme));
			}

			var schemeWithoutPrefix = scheme.Substring("web+".Length);
			// The custom scheme's name includes at least 1 letter after the web+ prefix
			if (schemeWithoutPrefix.Length == 0)
			{
				throw new ArgumentException(
					"Scheme must include at least 1 letter after 'web+' prefix",
					nameof(scheme));
			}

			// The custom scheme has only lowercase ASCII letters in its name.
			if (!schemeWithoutPrefix.ToCharArray().All(c => 'a' <= c && c <= 'z'))
			{
				throw new ArgumentException(
					"Scheme must include only lowercase ASCII letters after " +
					"the 'web+' prefix",
					nameof(scheme));
			}

			if (domain == null)
			{
				throw new ArgumentNullException(nameof(domain));
			}

			if (!domain.IsAbsoluteUri)
			{
				throw new ArgumentException(
					"Domain name must be an absolute URI.",
					nameof(domain));
			}

			var uriBuilder = new UriBuilder(domain);
			var query = HttpUtility.ParseQueryString(uriBuilder.Query);
			query[QueryKey] = ""; //set empty, otherwise %s would be encoded
			uriBuilder.Query = query.ToString();
			var uriString = uriBuilder.ToString();

			uriString += "%s";

			// register scheme
			var initialized = Uno.Foundation.WebAssemblyRuntime.InvokeJS(
				$"navigator.registerProtocolHandler('{scheme}', '{uriString}' , '{prompt.Replace("'", "\\'")}')");
		}

		internal static bool TryParseActivationUri(string queryArguments, out Uri uri)
		{
			NameValueCollection queryValues = null;
			uri = null;
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
				protocolUriString = Uri.UnescapeDataString(protocolUriString);
				if (Uri.TryCreate(protocolUriString, UriKind.Absolute, out uri))
				{
					return true;
				}
				else
				{
					typeof(ProtocolActivation).Log().LogError($"Activation URI {protocolUriString} could not be parsed");					
				}
			}

			// arguments did not contain activation URI or it could not be parsed
			return false;
		}
	}
}
#endif
