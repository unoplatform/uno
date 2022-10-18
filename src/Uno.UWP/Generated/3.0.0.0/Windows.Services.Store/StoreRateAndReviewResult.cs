#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Store
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StoreRateAndReviewResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Exception ExtendedError
		{
			get
			{
				throw new global::System.NotImplementedException("The member Exception StoreRateAndReviewResult.ExtendedError is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string ExtendedJsonData
		{
			get
			{
				throw new global::System.NotImplementedException("The member string StoreRateAndReviewResult.ExtendedJsonData is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Services.Store.StoreRateAndReviewStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member StoreRateAndReviewStatus StoreRateAndReviewResult.Status is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool WasUpdated
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool StoreRateAndReviewResult.WasUpdated is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Services.Store.StoreRateAndReviewResult.ExtendedError.get
		// Forced skipping of method Windows.Services.Store.StoreRateAndReviewResult.ExtendedJsonData.get
		// Forced skipping of method Windows.Services.Store.StoreRateAndReviewResult.WasUpdated.get
		// Forced skipping of method Windows.Services.Store.StoreRateAndReviewResult.Status.get
	}
}
