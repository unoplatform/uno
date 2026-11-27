#nullable enable

using Android.App;
using Android.Content;
using Android.OS;

namespace SamplesApp.Droid
{
	/// <summary>
	/// A plain, non-Uno activity that opens a <see cref="StackedWindowActivity"/> on top of itself, in whatever task it
	/// was started in. When that copy finishes, this activity comes back instead of the original one, which then never
	/// gets the chance to reclaim the window. Started by the Given_ApplicationActivity runtime tests.
	/// </summary>
	[Activity(Exported = false, Theme = "@style/AppTheme")]
	public class TaskHostActivity : Activity
	{
		public static TaskHostActivity? Current { get; private set; }

		protected override void OnCreate(Bundle? savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			Current = this;

			if (savedInstanceState is null)
			{
				StartActivity(new Intent(this, typeof(StackedWindowActivity)));
			}
		}

		protected override void OnDestroy()
		{
			if (Current == this)
			{
				Current = null;
			}

			base.OnDestroy();
		}
	}
}
