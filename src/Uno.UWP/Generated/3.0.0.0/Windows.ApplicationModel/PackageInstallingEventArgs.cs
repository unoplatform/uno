#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PackageInstallingEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid ActivityId
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid PackageInstallingEventArgs.ActivityId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Guid%20PackageInstallingEventArgs.ActivityId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Exception ErrorCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member Exception PackageInstallingEventArgs.ErrorCode is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Exception%20PackageInstallingEventArgs.ErrorCode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsComplete
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PackageInstallingEventArgs.IsComplete is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20PackageInstallingEventArgs.IsComplete");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Package Package
		{
			get
			{
				throw new global::System.NotImplementedException("The member Package PackageInstallingEventArgs.Package is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Package%20PackageInstallingEventArgs.Package");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double Progress
		{
			get
			{
				throw new global::System.NotImplementedException("The member double PackageInstallingEventArgs.Progress is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=double%20PackageInstallingEventArgs.Progress");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.PackageInstallingEventArgs.ActivityId.get
		// Forced skipping of method Windows.ApplicationModel.PackageInstallingEventArgs.Package.get
		// Forced skipping of method Windows.ApplicationModel.PackageInstallingEventArgs.Progress.get
		// Forced skipping of method Windows.ApplicationModel.PackageInstallingEventArgs.IsComplete.get
		// Forced skipping of method Windows.ApplicationModel.PackageInstallingEventArgs.ErrorCode.get
	}
}
