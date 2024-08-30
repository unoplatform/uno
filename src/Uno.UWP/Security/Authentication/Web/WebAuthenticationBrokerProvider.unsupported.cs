#if !__ANDROID__ && __IOS__ || __TVOS___ && !__TVOS__ && !__MACOS__ && !__SKIA__ && !__WASM__
#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;

namespace Uno.AuthenticationBroker
{
	partial class WebAuthenticationBrokerProvider
	{
		protected virtual IEnumerable<string> GetApplicationCustomSchemes()
		{
			throw new NotImplementedException();
		}

		protected virtual Task<WebAuthenticationResult> AuthenticateAsyncCore(
			WebAuthenticationOptions options,
			Uri requestUri,
			Uri callbackUri,
			CancellationToken ct)
		{
			throw new NotImplementedException();
		}
	}
}
#endif
