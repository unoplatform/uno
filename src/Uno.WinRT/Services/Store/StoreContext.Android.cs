#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI;
using Windows.Foundation;
using Windows.Services.Store.Internal;

namespace Windows.Services.Store;

public sealed partial class StoreContext
{
	private IStoreContextExtension? _storeContextExtension;

	partial void InitializePlatform()
	{
		if (ApiExtensibility.CreateInstance<IStoreContextExtension>(this) is { } extension)
		{
			_storeContextExtension = extension;
		}
	}

	public IAsyncOperation<StoreProductResult> GetStoreProductForCurrentAppAsync()
	{
		return AsyncOperation.FromTask(ct =>
		{
			var storeId = ContextHelper.Current.PackageName;

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

	private async Task<StoreRateAndReviewResult> RequestRateAndReviewAppTaskAsync(CancellationToken cancellationToken)
	{
		if (_storeContextExtension is null)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().LogError("No StoreContext extension is available. Cannot request rate and review.");
			}

			return new StoreRateAndReviewResult(StoreRateAndReviewStatus.Error);
		}
		return await _storeContextExtension.RequestRateAndReviewAsync(cancellationToken);
	}
}
