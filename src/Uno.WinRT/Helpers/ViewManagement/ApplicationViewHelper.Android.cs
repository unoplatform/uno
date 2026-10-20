namespace Uno.UI.ViewManagement;

public static class ApplicationViewHelper
{
	internal static IActivityLifecycleEvents GetActivityLifecycleEvents() => ContextHelper.Current as IActivityLifecycleEvents;
}
