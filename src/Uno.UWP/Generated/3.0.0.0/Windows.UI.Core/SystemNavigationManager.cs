#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class SystemNavigationManager 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Core.AppViewBackButtonVisibility AppViewBackButtonVisibility
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppViewBackButtonVisibility SystemNavigationManager.AppViewBackButtonVisibility is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.SystemNavigationManager", "AppViewBackButtonVisibility SystemNavigationManager.AppViewBackButtonVisibility");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Core.SystemNavigationManager.BackRequested.add
		// Forced skipping of method Windows.UI.Core.SystemNavigationManager.BackRequested.remove
		// Forced skipping of method Windows.UI.Core.SystemNavigationManager.AppViewBackButtonVisibility.get
		// Forced skipping of method Windows.UI.Core.SystemNavigationManager.AppViewBackButtonVisibility.set
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Core.SystemNavigationManager GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member SystemNavigationManager SystemNavigationManager.GetForCurrentView() is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::System.EventHandler<global::Windows.UI.Core.BackRequestedEventArgs> BackRequested
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.SystemNavigationManager", "event EventHandler<BackRequestedEventArgs> SystemNavigationManager.BackRequested");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.SystemNavigationManager", "event EventHandler<BackRequestedEventArgs> SystemNavigationManager.BackRequested");
			}
		}
		#endif
	}
}
