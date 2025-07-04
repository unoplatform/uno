using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using StoreKit;

namespace Uno.Services.Store.Internal
{
	[Preserve(AllMembers = true)]
	internal class ProductRequestDelegate : SKRequestDelegate, ISKProductsRequestDelegate
	{
		private readonly TaskCompletionSource<SKProductsResponse> _completionSource;

		public ProductRequestDelegate(
			TaskCompletionSource<SKProductsResponse> completionSource)
		{
			_completionSource = completionSource;
		}

		public override void RequestFailed(SKRequest request, NSError error)
		{
			base.RequestFailed(request, error);
		}

		public void ReceivedResponse(SKProductsRequest request, SKProductsResponse response) =>
			_completionSource.SetResult(response);
	}

}
