using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Windows.Services.Store
{
	public sealed partial class StoreContext
    {
		private StoreContext() { }

		public static StoreContext GetDefault() => new StoreContext();

		public IAsyncOperation<StoreProductQueryResult> GetStoreProductsAsync(
			IEnumerable<string> productKinds,
			IEnumerable<string> storeIds) =>
			GetStoreProductsImplAsync(productKinds, storeIds)
				.AsAsyncOperation();
	}
}
