namespace Uno.UI.ViewManagement;

public static class ApplicationViewHelper
{
	/// <summary>
	/// Gets an instance to <see cref="IBaseActivityEvents"/> which provides a set of events
	/// raised on key Activity method overrides.
	/// </summary>
	public static IBaseActivityEvents GetBaseActivityEvents() => ContextHelper.Current as IBaseActivityEvents;
}
