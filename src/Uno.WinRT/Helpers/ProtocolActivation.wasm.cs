using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Web;
using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Uno.Helpers
{
	public static partial class ProtocolActivation
	{
		private static readonly IEnumerable<string> _predefinedPrefixes = [
			"bitcoin",
			"ftp",
			"ftps",
			"geo",
			"im",
			"irc",
			"ircs",
			"magnet",
			"mailto",
			"matrix",
			"mms",
			"news",
			"nntp",
			"openpgp4fpr",
			"sftp",
			"sip",
			"sms",
			"smsto",
			"ssh",
			"tel",
			"urn",
			"webcal",
			"wtai",
			"xmpp"
		];

		/// <summary>
		/// Registers a custom URI scheme for protocol activation on WASM.
		/// </summary>
		/// <param name="scheme">Scheme (must start with web+, after which must follow one or more lowercase ASCII letter).</param>
		/// <param name="domain">Domain on which your application is running.</param>
		/// <param name="prompt">Prompt to show to the user.</param>
		public static void RegisterCustomScheme(string scheme, Uri domain, string prompt)
		{
			// rules as per https://developer.mozilla.org/en-US/docs/Web/API/Navigator/registerProtocolHandler
			if (!_predefinedPrefixes.Contains(scheme))
			{
				// The custom scheme's name begins with web+
				if (!scheme.StartsWith("web+", StringComparison.Ordinal))
				{
					throw new ArgumentException(
						"Scheme must start with 'web+'",
						nameof(scheme));
				}

				// The custom scheme's name includes at least 1 letter after the web+ prefix
				if (scheme.Length == "web+".Length)
				{
					throw new ArgumentException(
						"Scheme must include at least 1 letter after 'web+' prefix",
						nameof(scheme));
				}

				// The custom scheme has only lowercase ASCII letters in its name.
				for (int i = "web+".Length; i < scheme.Length; i++)
				{
					if (scheme[i] is not (>= 'a' and <= 'z'))
					{
						throw new ArgumentException(
							"Scheme must include only lowercase ASCII letters after " +
							"the 'web+' prefix",
							nameof(scheme));
					}
				}
			}

			ArgumentNullException.ThrowIfNull(domain);

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
			NativeMethods.RegisterProtocolHandler(scheme, uriString, prompt);
		}

		internal static partial class NativeMethods
		{
			[JSImport("globalThis.navigator.registerProtocolHandler")]
			internal static partial void RegisterProtocolHandler(string scheme, string uri, string prompt);
		}
	}
}
