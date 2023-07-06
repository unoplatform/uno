using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Extensibility;
using Uno.Foundation;

namespace Uno.UI.MSAL
{
	internal partial class WasmWebUi : ICustomWebUi
	{
		internal static readonly WasmWebUi Instance = new WasmWebUi();

		public async Task<Uri> AcquireAuthorizationCodeAsync(Uri authorizationUri, Uri redirectUri, CancellationToken cancellationToken)
		{
			var uri = await NativeMethods.AuthenticateAsync(authorizationUri.OriginalString, redirectUri.OriginalString, "Sign in", 483, 600);
			return new Uri(uri);
		}

		internal static partial class NativeMethods
		{
			[JSImport("globalThis.MSAL.WebUI.authenticate")]
			internal static partial Task<string> AuthenticateAsync(string urlNavigate, string urlRedirect, string title, int popupWidth, int popupHeight);
		}
	}
}
