using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI;
using Windows.Foundation;

namespace Windows.Services.Store
{
	public sealed partial class StoreContext
	{
		public IAsyncOperation<StoreProductResult> GetStoreProductForCurrentAppAsync()
		{
			return AsyncOperation.FromTask(ct =>
			{
				var storeId = ContextHelper.Current.PackageName!;

				return Task.FromResult(new StoreProductResult
				{
					Product = new StoreProduct
					{
						StoreId = storeId,
						LinkUri = new Uri($"https://play.google.com/store/apps/details?id={storeId}")
					}
				});
			});
		}
	}
}
