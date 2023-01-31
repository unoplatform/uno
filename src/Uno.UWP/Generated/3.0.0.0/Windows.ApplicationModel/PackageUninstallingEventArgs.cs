#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PackageUninstallingEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid ActivityId
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid PackageUninstallingEventArgs.ActivityId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Guid%20PackageUninstallingEventArgs.ActivityId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Exception ErrorCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member Exception PackageUninstallingEventArgs.ErrorCode is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Exception%20PackageUninstallingEventArgs.ErrorCode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsComplete
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PackageUninstallingEventArgs.IsComplete is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20PackageUninstallingEventArgs.IsComplete");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Package Package
		{
			get
			{
				throw new global::System.NotImplementedException("The member Package PackageUninstallingEventArgs.Package is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Package%20PackageUninstallingEventArgs.Package");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double Progress
		{
			get
			{
				throw new global::System.NotImplementedException("The member double PackageUninstallingEventArgs.Progress is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=double%20PackageUninstallingEventArgs.Progress");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.PackageUninstallingEventArgs.ActivityId.get
		// Forced skipping of method Windows.ApplicationModel.PackageUninstallingEventArgs.Package.get
		// Forced skipping of method Windows.ApplicationModel.PackageUninstallingEventArgs.Progress.get
		// Forced skipping of method Windows.ApplicationModel.PackageUninstallingEventArgs.IsComplete.get
		// Forced skipping of method Windows.ApplicationModel.PackageUninstallingEventArgs.ErrorCode.get
	}
}
