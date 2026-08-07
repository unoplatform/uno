using System;
using Windows.Foundation;

namespace Windows.Services.Store;

public sealed partial class StoreContext
{
	private static Lazy<StoreContext> _instance = new(() => new StoreContext());

	private StoreContext()
	{
		InitializePlatform();
	}

	partial void InitializePlatform();

	/// <summary>
	/// Gets a StoreContext object that can be used to access
	/// and manage store-related data for the current
	/// user in the context of the current app.
	/// </summary>
	/// <returns>An object that you can use to access and manage
	/// store-related data for the current user.</returns>
	public static StoreContext GetDefault() => _instance.Value;

#if __IOS__ || __ANDROID__
	/// <summary>
	/// Requests the user to rate and review the app. This method will
	/// display the UI for the user to select a store rating and add
	/// an optional store review for the product.
	/// </summary>
	/// <returns>Store rate and review result.</returns>
	/// <remarks>
	/// On iOS and macOS, it is not possible to know whether the user
	/// actually rated the app or whether the system allowed the dialog
	/// to be displayed.
	/// </remarks>
	public IAsyncOperation<StoreRateAndReviewResult> RequestRateAndReviewAppAsync() =>
		AsyncOperation.FromTask(ct => RequestRateAndReviewAppTaskAsync(ct));
#endif
}
