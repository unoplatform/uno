#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Store
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	#endif
	public  partial class StoreSubscriptionInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint BillingPeriod
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint StoreSubscriptionInfo.BillingPeriod is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Services.Store.StoreDurationUnit BillingPeriodUnit
		{
			get
			{
				throw new global::System.NotImplementedException("The member StoreDurationUnit StoreSubscriptionInfo.BillingPeriodUnit is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool HasTrialPeriod
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool StoreSubscriptionInfo.HasTrialPeriod is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint TrialPeriod
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint StoreSubscriptionInfo.TrialPeriod is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Services.Store.StoreDurationUnit TrialPeriodUnit
		{
			get
			{
				throw new global::System.NotImplementedException("The member StoreDurationUnit StoreSubscriptionInfo.TrialPeriodUnit is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Services.Store.StoreSubscriptionInfo.BillingPeriod.get
		// Forced skipping of method Windows.Services.Store.StoreSubscriptionInfo.BillingPeriodUnit.get
		// Forced skipping of method Windows.Services.Store.StoreSubscriptionInfo.HasTrialPeriod.get
		// Forced skipping of method Windows.Services.Store.StoreSubscriptionInfo.TrialPeriod.get
		// Forced skipping of method Windows.Services.Store.StoreSubscriptionInfo.TrialPeriodUnit.get
	}
}
