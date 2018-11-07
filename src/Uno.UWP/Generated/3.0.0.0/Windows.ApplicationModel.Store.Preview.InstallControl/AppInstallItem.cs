#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Store.Preview.InstallControl
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppInstallItem 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallType InstallType
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppInstallType AppInstallItem.InstallType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsUserInitiated
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool AppInstallItem.IsUserInitiated is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string PackageFamilyName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppInstallItem.PackageFamilyName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string ProductId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppInstallItem.ProductId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem> Children
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<AppInstallItem> AppInstallItem.Children is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool ItemOperationsMightAffectOtherItems
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool AppInstallItem.ItemOperationsMightAffectOtherItems is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool LaunchAfterInstall
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool AppInstallItem.LaunchAfterInstall is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem", "bool AppInstallItem.LaunchAfterInstall");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem.ProductId.get
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem.PackageFamilyName.get
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem.InstallType.get
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem.IsUserInitiated.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallStatus GetCurrentStatus()
		{
			throw new global::System.NotImplementedException("The member AppInstallStatus AppInstallItem.GetCurrentStatus() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Cancel()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem", "void AppInstallItem.Cancel()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Pause()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem", "void AppInstallItem.Pause()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Restart()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem", "void AppInstallItem.Restart()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem.Completed.add
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem.Completed.remove
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem.StatusChanged.add
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem.StatusChanged.remove
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Cancel( string correlationVector)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem", "void AppInstallItem.Cancel(string correlationVector)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Pause( string correlationVector)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem", "void AppInstallItem.Pause(string correlationVector)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Restart( string correlationVector)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem", "void AppInstallItem.Restart(string correlationVector)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem.Children.get
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem.ItemOperationsMightAffectOtherItems.get
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem.LaunchAfterInstall.get
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem.LaunchAfterInstall.set
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem, object> Completed
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem", "event TypedEventHandler<AppInstallItem, object> AppInstallItem.Completed");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem", "event TypedEventHandler<AppInstallItem, object> AppInstallItem.Completed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem, object> StatusChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem", "event TypedEventHandler<AppInstallItem, object> AppInstallItem.StatusChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem", "event TypedEventHandler<AppInstallItem, object> AppInstallItem.StatusChanged");
			}
		}
		#endif
	}
}
