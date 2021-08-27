#if __ANDROID__
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
#pragma warning disable CA1822 // Mark members as static - align with UWP
		public IAsyncOperation<StoreProductResult> GetStoreProductForCurrentAppAsync()
#pragma warning restore CA1822 // Mark members as static
		{
			return AsyncOperation.FromTask(async ct =>
			{
				var storeId = ContextHelper.Current.PackageName;

				return new StoreProductResult
				{
					Product = new StoreProduct
					{
						StoreId = storeId,
						LinkUri = new Uri($"https://play.google.com/store/apps/details?id={storeId}")
					}
				};
			});
		}
	}
}
#endif
