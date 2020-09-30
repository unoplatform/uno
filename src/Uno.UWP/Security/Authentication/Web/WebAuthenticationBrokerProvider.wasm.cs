#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;
using Uno.Foundation;

namespace Uno.AuthenticationBroker
{
	partial class WebAuthenticationBrokerProvider
	{
		protected virtual string[] GetApplicationCustomSchemes()
		{
			var js = $@"Windows.Security.Authentication.Web.WebAuthenticationBroker.getReturnUrl();";
			var origin = WebAssemblyRuntime.InvokeJS(js);

			// origin will be something line http://localhost:5001

			Console.WriteLine("origin is: " + origin);

			return new[] {origin};
		}

		protected virtual async Task<WebAuthenticationResult> AuthenticateAsyncCore(
			WebAuthenticationOptions options,
			Uri requestUri,
			Uri callbackUri,
			CancellationToken ct)
		{
			// options are ignored for now

			// TODO: support ct

			var urlNavigate = WebAssemblyRuntime.EscapeJs(requestUri.OriginalString);
			var urlRedirect = WebAssemblyRuntime.EscapeJs(callbackUri.OriginalString);
			string js;

			var timeout = ((long) Timeout.TotalMilliseconds).ToString();

			var useIframe =
				options.HasFlag(WebAuthenticationOptions.SilentMode) ||
				!string.IsNullOrEmpty(WinRTFeatureConfiguration.WebAuthenticationBroker.IFrameHtmlId);

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

			try
			{
				var results = (await WebAssemblyRuntime.InvokeAsync(js)).Split(new[] {'|'}, 2);

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
	}
}
