#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Store
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UnfulfilledConsumable 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string OfferId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string UnfulfilledConsumable.OfferId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20UnfulfilledConsumable.OfferId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ProductId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string UnfulfilledConsumable.ProductId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20UnfulfilledConsumable.ProductId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid TransactionId
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid UnfulfilledConsumable.TransactionId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Guid%20UnfulfilledConsumable.TransactionId");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Store.UnfulfilledConsumable.ProductId.get
		// Forced skipping of method Windows.ApplicationModel.Store.UnfulfilledConsumable.TransactionId.get
		// Forced skipping of method Windows.ApplicationModel.Store.UnfulfilledConsumable.OfferId.get
	}
}
