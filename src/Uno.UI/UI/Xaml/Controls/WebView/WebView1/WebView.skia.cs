using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Uno.Extensions;
using Uno.UI.Extensions;
using Windows.UI.Xaml;
using Uno.UI.Web;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Windows.UI.Core;
using Uno.Foundation.Logging;

namespace Windows.UI.Xaml.Controls
{
	public partial class WebView
	{
		//This should be IAsyncOperation<string> instead of Task<string> but we use an extension method to enable the same signature in Win.
		//IAsyncOperation is not available in Xamarin.
		public Task<string> InvokeScriptAsync(CancellationToken ct, string script, string[] arguments)
		{
			throw new NotSupportedException();
		}

		public Task<string> InvokeScriptAsync(string script, string[] arguments)
		{
			throw new NotSupportedException();
		}
	}
}

