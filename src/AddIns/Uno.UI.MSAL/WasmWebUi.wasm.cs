using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Extensibility;
using Uno.Foundation;

#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.JavaScript;
#endif

namespace Uno.UI.MSAL
{
	internal partial class WasmWebUi : ICustomWebUi
	{
		internal static readonly WasmWebUi Instance = new WasmWebUi();

		public async Task<Uri> AcquireAuthorizationCodeAsync(Uri authorizationUri, Uri redirectUri, CancellationToken cancellationToken)
		{
#if NET7_0_OR_GREATER
			var uri = await NativeMethods.AuthenticateAsync(authorizationUri.OriginalString, redirectUri.OriginalString, "Sign in", 483, 600);
#else
			var urlNavigate = WebAssemblyRuntime.EscapeJs(authorizationUri.OriginalString);
			var urlRedirect = WebAssemblyRuntime.EscapeJs(redirectUri.OriginalString);

			var js = $@"MSAL.WebUI.authenticate(""{urlNavigate}"", ""{urlRedirect}"", ""Sign in"", 483, 600);";

			var uri = await WebAssemblyRuntime.InvokeAsync(js);
#endif

			return new Uri(uri);
		}

#if NET7_0_OR_GREATER
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.MSAL.WebUI.authenticate")]
			internal static partial Task<string> AuthenticateAsync(string urlNavigate, string urlRedirect, string title, int popupWidth, int popupHeight);
		}
#endif
	}
}
