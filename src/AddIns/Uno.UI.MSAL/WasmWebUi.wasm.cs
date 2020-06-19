using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Extensibility;
using Uno.Foundation;

namespace Uno.UI.MSAL
{
	internal class WasmWebUi : ICustomWebUi
	{
		internal static readonly WasmWebUi Instance = new WasmWebUi();

		public async Task<Uri> AcquireAuthorizationCodeAsync(Uri authorizationUri, Uri redirectUri, CancellationToken cancellationToken)
		{
			var urlNavigate = WebAssemblyRuntime.EscapeJs(authorizationUri.OriginalString);
			var urlRedirect = WebAssemblyRuntime.EscapeJs(redirectUri.OriginalString);

			var js = $@"MSAL.WebUI.authenticate(""{urlNavigate}"", ""{urlRedirect}"", ""Sign in"", 483, 600);";

			var uri = await WebAssemblyRuntime.InvokeAsync(js);
			return new Uri(uri);
		}
	}
}
