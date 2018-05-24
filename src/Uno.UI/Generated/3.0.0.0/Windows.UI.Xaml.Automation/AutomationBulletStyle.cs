#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AutomationBulletStyle 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HollowRoundBullet,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FilledRoundBullet,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HollowSquareBullet,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FilledSquareBullet,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DashBullet,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Other,
		#endif
	}
	#endif
}
