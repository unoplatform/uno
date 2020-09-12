#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PackageContentGroupStagingEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid ActivityId
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid PackageContentGroupStagingEventArgs.ActivityId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ContentGroupName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PackageContentGroupStagingEventArgs.ContentGroupName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Exception ErrorCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member Exception PackageContentGroupStagingEventArgs.ErrorCode is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsComplete
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PackageContentGroupStagingEventArgs.IsComplete is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsContentGroupRequired
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PackageContentGroupStagingEventArgs.IsContentGroupRequired is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Package Package
		{
			get
			{
				throw new global::System.NotImplementedException("The member Package PackageContentGroupStagingEventArgs.Package is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double Progress
		{
			get
			{
				throw new global::System.NotImplementedException("The member double PackageContentGroupStagingEventArgs.Progress is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.PackageContentGroupStagingEventArgs.ActivityId.get
		// Forced skipping of method Windows.ApplicationModel.PackageContentGroupStagingEventArgs.Package.get
		// Forced skipping of method Windows.ApplicationModel.PackageContentGroupStagingEventArgs.Progress.get
		// Forced skipping of method Windows.ApplicationModel.PackageContentGroupStagingEventArgs.IsComplete.get
		// Forced skipping of method Windows.ApplicationModel.PackageContentGroupStagingEventArgs.ErrorCode.get
		// Forced skipping of method Windows.ApplicationModel.PackageContentGroupStagingEventArgs.ContentGroupName.get
		// Forced skipping of method Windows.ApplicationModel.PackageContentGroupStagingEventArgs.IsContentGroupRequired.get
	}
}
