#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.LockScreen
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LockApplicationHost 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void RequestUnlock()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.LockScreen.LockApplicationHost", "void LockApplicationHost.RequestUnlock()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.LockScreen.LockApplicationHost.Unlocking.add
		// Forced skipping of method Windows.ApplicationModel.LockScreen.LockApplicationHost.Unlocking.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.ApplicationModel.LockScreen.LockApplicationHost GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member LockApplicationHost LockApplicationHost.GetForCurrentView() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.LockScreen.LockApplicationHost, global::Windows.ApplicationModel.LockScreen.LockScreenUnlockingEventArgs> Unlocking
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.LockScreen.LockApplicationHost", "event TypedEventHandler<LockApplicationHost, LockScreenUnlockingEventArgs> LockApplicationHost.Unlocking");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.LockScreen.LockApplicationHost", "event TypedEventHandler<LockApplicationHost, LockScreenUnlockingEventArgs> LockApplicationHost.Unlocking");
			}
		}
		#endif
	}
}
