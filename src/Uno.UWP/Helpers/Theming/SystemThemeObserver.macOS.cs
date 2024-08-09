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
			.DefaultCenter
			.AddObserver(
				this,
				_modeSelector,
				_themeChangedNotification,
				null);
	}

	[Export("themeChanged:")]
	public void ThemeChanged(NSObject change) => SystemThemeHelper.RefreshSystemTheme();
}
