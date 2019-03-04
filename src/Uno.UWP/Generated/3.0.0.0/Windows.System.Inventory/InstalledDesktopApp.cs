#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Inventory
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InstalledDesktopApp : global::Windows.Foundation.IStringable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string DisplayName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string InstalledDesktopApp.DisplayName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string DisplayVersion
		{
			get
			{
				throw new global::System.NotImplementedException("The member string InstalledDesktopApp.DisplayVersion is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string InstalledDesktopApp.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Publisher
		{
			get
			{
				throw new global::System.NotImplementedException("The member string InstalledDesktopApp.Publisher is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.System.Inventory.InstalledDesktopApp.Id.get
		// Forced skipping of method Windows.System.Inventory.InstalledDesktopApp.DisplayName.get
		// Forced skipping of method Windows.System.Inventory.InstalledDesktopApp.Publisher.get
		// Forced skipping of method Windows.System.Inventory.InstalledDesktopApp.DisplayVersion.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public override string ToString()
		{
			throw new global::System.NotImplementedException("The member string InstalledDesktopApp.ToString() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.System.Inventory.InstalledDesktopApp>> GetInventoryAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<InstalledDesktopApp>> InstalledDesktopApp.GetInventoryAsync() is not implemented in Uno.");
		}
		#endif
	}
}
