using Foundation;
using Selector = ObjCRuntime.Selector;

namespace Uno.Helpers.Theming;

[Preserve(AllMembers = true)]
public class SystemThemeObserver : NSObject
{
	private readonly NSString _themeChangedNotification = new NSString("AppleInterfaceThemeChangedNotification");
	private readonly Selector _modeSelector = new Selector("themeChanged:");

	internal void ObserveSystemThemeChanges()
	{
		NSDistributedNotificationCenter
#if NET6_0_OR_GREATER
			.DefaultCenter
#else
			.GetDefaultCenter()
#endif
			.AddObserver(
				this,
				_modeSelector,
				_themeChangedNotification,
				null);
	}

	[Export("themeChanged:")]
	public void ThemeChanged(NSObject change) => SystemThemeHelper.RefreshSystemTheme();
}
