using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;

namespace Uno.AuthenticationBroker
{
	partial class WebAuthenticationBrokerProvider
	{
		private static IEnumerable<string> GetCustomSchemes()
		{
			throw new NotImplementedException();
		}

		public async Task<WebAuthenticationResult> AuthenticateAsync(
			WebAuthenticationOptions options,
			Uri requestUri,
			Uri callbackUri,
			CancellationToken ct)
		{
			throw new NotImplementedException();
		}
	}
}
