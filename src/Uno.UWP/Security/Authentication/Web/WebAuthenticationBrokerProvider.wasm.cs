#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;
using Uno.Foundation;
using System.Globalization;

namespace Uno.AuthenticationBroker
{
	partial class WebAuthenticationBrokerProvider
	{
		protected virtual IEnumerable<string> GetApplicationCustomSchemes()
		{
			var origin = NativeMethods.GetReturnUrl();
			return new[] { origin };
		}

		protected virtual async Task<WebAuthenticationResult> AuthenticateAsyncCore(
			WebAuthenticationOptions options,
			Uri requestUri,
			Uri callbackUri,
			CancellationToken ct)
		{
			// options are ignored for now

			// TODO: support ct

			var useIframe =
				options.HasFlag(WebAuthenticationOptions.SilentMode) ||
				!string.IsNullOrEmpty(WinRTFeatureConfiguration.WebAuthenticationBroker.IFrameHtmlId);

			try
			{
				string[] results;

				if (useIframe)
				{
					results = (await NativeMethods.AuthenticateUsingIframeAsync(
						WinRTFeatureConfiguration.WebAuthenticationBroker.IFrameHtmlId!,
						requestUri.OriginalString,
						callbackUri.OriginalString,
						Timeout.TotalMilliseconds))
							.Split('|', 2);
				}
				else
				{
					results = (await NativeMethods.AuthenticateUsingWindowAsync(
						requestUri.OriginalString,
						callbackUri.OriginalString,
						WinRTFeatureConfiguration.WebAuthenticationBroker.WindowTitle ?? "Sign In",
						WinRTFeatureConfiguration.WebAuthenticationBroker.WindowWidth,
						WinRTFeatureConfiguration.WebAuthenticationBroker.WindowHeight,
						Timeout.TotalMilliseconds))
							.Split('|', 2);
				}

				return results[0] switch
				{
					"success" => new WebAuthenticationResult(results[1], 0, WebAuthenticationStatus.Success),
					"cancel" => new WebAuthenticationResult(null, 0, WebAuthenticationStatus.UserCancel),
					"timeout" => new WebAuthenticationResult(null, 0, WebAuthenticationStatus.UserCancel),
					_ => new WebAuthenticationResult(null, 0, WebAuthenticationStatus.ErrorHttp)
				};
			}
			catch (ApplicationException aex)
			{
				return new WebAuthenticationResult(aex.Message, 0, WebAuthenticationStatus.ErrorHttp);
			}
		}

		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Windows.Security.Authentication.Web.WebAuthenticationBroker.authenticateUsingIframe")]
			internal static partial Task<string> AuthenticateUsingIframeAsync(string iframe, string urlNavigate, string urlRedirect, double timeout);

			[JSImport("globalThis.Windows.Security.Authentication.Web.WebAuthenticationBroker.authenticateUsingWindow")]
			internal static partial Task<string> AuthenticateUsingWindowAsync(string urlNavigate, string urlRedirect, string title, int windowWidth, int windowHeight, double timeout);

			[JSImport("globalThis.Windows.Security.Authentication.Web.WebAuthenticationBroker.getReturnUrl")]
			internal static partial string GetReturnUrl();
		}
	}
}
