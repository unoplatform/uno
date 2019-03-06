#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.UI
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GameMonitor 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Gaming.UI.GameMonitoringPermission> RequestPermissionAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GameMonitoringPermission> GameMonitor.RequestPermissionAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Gaming.UI.GameMonitor GetDefault()
		{
			throw new global::System.NotImplementedException("The member GameMonitor GameMonitor.GetDefault() is not implemented in Uno.");
		}
		#endif
	}
}
