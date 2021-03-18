#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LauncherUIOptions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Rect? SelectionRect
		{
			get
			{
				throw new global::System.NotImplementedException("The member Rect? LauncherUIOptions.SelectionRect is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.LauncherUIOptions", "Rect? LauncherUIOptions.SelectionRect");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Popups.Placement PreferredPlacement
		{
			get
			{
				throw new global::System.NotImplementedException("The member Placement LauncherUIOptions.PreferredPlacement is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.LauncherUIOptions", "Placement LauncherUIOptions.PreferredPlacement");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Point? InvocationPoint
		{
			get
			{
				throw new global::System.NotImplementedException("The member Point? LauncherUIOptions.InvocationPoint is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.LauncherUIOptions", "Point? LauncherUIOptions.InvocationPoint");
			}
		}
		#endif
		// Forced skipping of method Windows.System.LauncherUIOptions.InvocationPoint.get
		// Forced skipping of method Windows.System.LauncherUIOptions.InvocationPoint.set
		// Forced skipping of method Windows.System.LauncherUIOptions.SelectionRect.get
		// Forced skipping of method Windows.System.LauncherUIOptions.SelectionRect.set
		// Forced skipping of method Windows.System.LauncherUIOptions.PreferredPlacement.get
		// Forced skipping of method Windows.System.LauncherUIOptions.PreferredPlacement.set
	}
}
