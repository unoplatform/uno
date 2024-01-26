using Android.App;
using Android.Runtime;
using Android.Views;
using Uno.UI.Xaml.Controls;
using Window = Microsoft.UI.Xaml.Window;

namespace Uno.UI
{
	public class OnSystemUiVisibilityChangeListener
#pragma warning disable 618
		: Java.Lang.Object, View.IOnSystemUiVisibilityChangeListener
#pragma warning restore 618
	{
		public void OnSystemUiVisibilityChange([GeneratedEnum] StatusBarVisibility visibility)
		{
			var activity = ContextHelper.Current as Activity;
			var decorView = activity.Window.DecorView;
#pragma warning disable 618
#pragma warning disable CA1422 // Validate platform compatibility
			var newUiOptions = (int)decorView.SystemUiVisibility;
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore 618

			if (((int)visibility & (int)SystemUiFlags.HideNavigation) == 0)
			{
				newUiOptions &= ~(int)SystemUiFlags.HideNavigation;
			}
			else
			{
				newUiOptions |= (int)SystemUiFlags.HideNavigation;
			}

			// We actually don't want to update the decorView.SystemUiVisibility because of the difference between SystemUiFlags.HideNavigation and SystemUiFlags.LayoutHideNavigation
			// - HideNavigation : User can show the navigation bar by sliding up from the bottom of the screen but it will disappear after 2-3 seconds
			// - LayoutHideNavigation : User can show the navigation bar by sliding up from the bottom of the screen and have the option to dock it / undock it
			// In the case we set the navigation bar to LayoutHideNavigation, when the user hide the bar, HideNavigation will be triggered.
			// But we don't want to inject it in the decorView.SystemUiVisibility to let the navigation bar dockable again
			NativeWindowWrapper.Instance.SystemUiVisibility = newUiOptions;

			activity.OnConfigurationChanged(activity.Resources.Configuration);
		}
	}
}
