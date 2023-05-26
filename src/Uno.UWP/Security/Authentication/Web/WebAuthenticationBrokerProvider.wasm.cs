#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;
using Uno.Foundation;
using System.Globalization;

#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.JavaScript;
#endif

namespace Uno.AuthenticationBroker
{
	partial class WebAuthenticationBrokerProvider
	{
		protected virtual string[] GetApplicationCustomSchemes()
		{
#if NET7_0_OR_GREATER
			var origin = NativeMethods.GetReturnUrl();
#else
			var js = $@"Windows.Security.Authentication.Web.WebAuthenticationBroker.getReturnUrl();";
			var origin = WebAssemblyRuntime.InvokeJS(js);

			// origin will be something line http://localhost:5001

			Console.WriteLine("origin is: " + origin);
#endif

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

#if !NET7_0_OR_GREATER
			var urlNavigate = WebAssemblyRuntime.EscapeJs(requestUri.OriginalString);
			var urlRedirect = WebAssemblyRuntime.EscapeJs(callbackUri.OriginalString);
			string js;

			var timeout = ((long)Timeout.TotalMilliseconds).ToString(CultureInfo.InvariantCulture);

			if (useIframe)
			{
				var iframeId = WinRTFeatureConfiguration.WebAuthenticationBroker.IFrameHtmlId;
				js =
					$@"Windows.Security.Authentication.Web.WebAuthenticationBroker.authenticateUsingIframe(""{iframeId}"", ""{urlNavigate}"", ""{urlRedirect}"", {timeout});";
			}
			else
			{
				var title = WebAssemblyRuntime.EscapeJs(WinRTFeatureConfiguration.WebAuthenticationBroker.WindowTitle ??
														"Sign In");

				var windowWidth = WinRTFeatureConfiguration.WebAuthenticationBroker.WindowWidth;
				var windowHeight = WinRTFeatureConfiguration.WebAuthenticationBroker.WindowHeight;
				js =
					$@"Windows.Security.Authentication.Web.WebAuthenticationBroker.authenticateUsingWindow(""{urlNavigate}"", ""{urlRedirect}"", ""{title}"", {windowWidth}, {windowHeight}, {timeout});";
			}
#endif

			try
			{
#if NET7_0_OR_GREATER
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
#else
				var results = (await WebAssemblyRuntime.InvokeAsync(js)).Split(new[] { '|' }, 2);
#endif

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

#if NET7_0_OR_GREATER
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Windows.Security.Authentication.Web.WebAuthenticationBroker.authenticateUsingIframe")]
			internal static partial Task<string> AuthenticateUsingIframeAsync(string iframe, string urlNavigate, string urlRedirect, double timeout);

			[JSImport("globalThis.Windows.Security.Authentication.Web.WebAuthenticationBroker.authenticateUsingWindow")]
			internal static partial Task<string> AuthenticateUsingWindowAsync(string urlNavigate, string urlRedirect, string title, int windowWidth, int windowHeight, double timeout);

			[JSImport("globalThis.Windows.Security.Authentication.Web.WebAuthenticationBroker.getReturnUrl")]
			internal static partial string GetReturnUrl();
		}
#endif
	}
}
