#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string AppUserModelId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppInfo.AppUserModelId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.ApplicationModel.AppDisplayInfo DisplayInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppDisplayInfo AppInfo.DisplayInfo is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppInfo.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string PackageFamilyName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppInfo.PackageFamilyName is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.AppInfo.Id.get
		// Forced skipping of method Windows.ApplicationModel.AppInfo.AppUserModelId.get
		// Forced skipping of method Windows.ApplicationModel.AppInfo.DisplayInfo.get
		// Forced skipping of method Windows.ApplicationModel.AppInfo.PackageFamilyName.get
	}
}
