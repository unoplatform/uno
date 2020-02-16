#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Store
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StorePurchaseResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Exception ExtendedError
		{
			get
			{
				throw new global::System.NotImplementedException("The member Exception StorePurchaseResult.ExtendedError is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Services.Store.StorePurchaseStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorePurchaseStatus StorePurchaseResult.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Services.Store.StorePurchaseResult.Status.get
		// Forced skipping of method Windows.Services.Store.StorePurchaseResult.ExtendedError.get
	}
}
