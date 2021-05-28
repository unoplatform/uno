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

		protected virtual async Task<WebAuthenticationResult> AuthenticateAsyncCore(
			WebAuthenticationOptions options,
			Uri requestUri,
			Uri callbackUri,
			CancellationToken ct)
		{
			throw new NotImplementedException();
		}
	}
}
