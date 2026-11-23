using Android.App;
using Uno.UI.Runtime.Android;

namespace SamplesApp.Droid
{
	/// <summary>
	/// A second activity instance stacked over the running one, as an intent delivered in the standard launch mode
	/// creates. Started by the Given_ApplicationActivity runtime tests; MainActivity itself is SingleTask.
	/// </summary>
	[Activity(Exported = false, Theme = "@style/AppTheme", ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges)]
	public class StackedWindowActivity : ApplicationActivity
	{
	}
}
