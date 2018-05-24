#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.EnterpriseData
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ProtectionPolicyEvaluationResult 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Allowed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Blocked,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConsentRequired,
		#endif
	}
	#endif
}
