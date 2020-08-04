#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ILauncherViewOptions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.ViewManagement.ViewSizePreference DesiredRemainingView
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.System.ILauncherViewOptions.DesiredRemainingView.get
		// Forced skipping of method Windows.System.ILauncherViewOptions.DesiredRemainingView.set
	}
}
