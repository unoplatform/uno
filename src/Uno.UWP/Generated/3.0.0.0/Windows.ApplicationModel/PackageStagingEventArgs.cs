#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PackageStagingEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid ActivityId
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid PackageStagingEventArgs.ActivityId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Guid%20PackageStagingEventArgs.ActivityId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Exception ErrorCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member Exception PackageStagingEventArgs.ErrorCode is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Exception%20PackageStagingEventArgs.ErrorCode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsComplete
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PackageStagingEventArgs.IsComplete is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20PackageStagingEventArgs.IsComplete");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Package Package
		{
			get
			{
				throw new global::System.NotImplementedException("The member Package PackageStagingEventArgs.Package is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Package%20PackageStagingEventArgs.Package");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double Progress
		{
			get
			{
				throw new global::System.NotImplementedException("The member double PackageStagingEventArgs.Progress is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=double%20PackageStagingEventArgs.Progress");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.PackageStagingEventArgs.ActivityId.get
		// Forced skipping of method Windows.ApplicationModel.PackageStagingEventArgs.Package.get
		// Forced skipping of method Windows.ApplicationModel.PackageStagingEventArgs.Progress.get
		// Forced skipping of method Windows.ApplicationModel.PackageStagingEventArgs.IsComplete.get
		// Forced skipping of method Windows.ApplicationModel.PackageStagingEventArgs.ErrorCode.get
	}
}
