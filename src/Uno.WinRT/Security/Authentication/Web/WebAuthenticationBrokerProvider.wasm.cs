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
		// HTTP 408 Request Timeout, so a timeout can be told apart from the user closing the popup.
		private const uint TimeoutErrorDetail = 408;

		private static int _nextOperationId;

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

			ct.ThrowIfCancellationRequested();

			var operationId = Interlocked.Increment(ref _nextOperationId);
			var useIframe =
				options.HasFlag(WebAuthenticationOptions.SilentMode) ||
				!string.IsNullOrEmpty(WinRTFeatureConfiguration.WebAuthenticationBroker.IFrameHtmlId);

			try
			{
				var authenticate = useIframe
					? NativeMethods.AuthenticateUsingIframeAsync(
						operationId,
						WinRTFeatureConfiguration.WebAuthenticationBroker.IFrameHtmlId!,
						requestUri.OriginalString,
						callbackUri.OriginalString,
						Timeout.TotalMilliseconds)
					: NativeMethods.AuthenticateUsingWindowAsync(
						operationId,
						requestUri.OriginalString,
						callbackUri.OriginalString,
						WinRTFeatureConfiguration.WebAuthenticationBroker.WindowTitle ?? "Sign In",
						WinRTFeatureConfiguration.WebAuthenticationBroker.WindowWidth,
						WinRTFeatureConfiguration.WebAuthenticationBroker.WindowHeight,
						Timeout.TotalMilliseconds);

				// Registered only once the JS side has tracked the operation, so a token canceled meanwhile still closes it.
				string[] results;
				using (ct.Register(() => NativeMethods.Abort(operationId)))
				{
					results = (await authenticate).Split('|', 2);
				}

				if (results[0] == "cancel" && ct.IsCancellationRequested)
				{
					throw new OperationCanceledException(ct);
				}

				return results[0] switch
				{
					"success" => new WebAuthenticationResult(results[1], 0, WebAuthenticationStatus.Success),
					"cancel" => new WebAuthenticationResult(null, 0, WebAuthenticationStatus.UserCancel),
					"timeout" => new WebAuthenticationResult(null, TimeoutErrorDetail, WebAuthenticationStatus.ErrorHttp),
					_ => new WebAuthenticationResult(null, 0, WebAuthenticationStatus.ErrorHttp)
				};
			}
			catch (ApplicationException aex)
			{
				return new WebAuthenticationResult(aex.Message, 0, WebAuthenticationStatus.ErrorHttp);
			}
			catch (JSException jsex)
			{
				// A rejected JS promise, e.g. a blocked popup ("Can't open window").
				return new WebAuthenticationResult(jsex.Message, 0, WebAuthenticationStatus.ErrorHttp);
			}
		}

		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Windows.Security.Authentication.Web.WebAuthenticationBroker.authenticateUsingIframe")]
			internal static partial Task<string> AuthenticateUsingIframeAsync(int operationId, string iframe, string urlNavigate, string urlRedirect, double timeout);

			[JSImport("globalThis.Windows.Security.Authentication.Web.WebAuthenticationBroker.authenticateUsingWindow")]
			internal static partial Task<string> AuthenticateUsingWindowAsync(int operationId, string urlNavigate, string urlRedirect, string title, int windowWidth, int windowHeight, double timeout);

			[JSImport("globalThis.Windows.Security.Authentication.Web.WebAuthenticationBroker.abort")]
			internal static partial void Abort(int operationId);

			[JSImport("globalThis.Windows.Security.Authentication.Web.WebAuthenticationBroker.getReturnUrl")]
			internal static partial string GetReturnUrl();
		}
	}
}
