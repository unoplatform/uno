using System;
using Android.OS;

namespace Uno.UI.ViewManagement;

internal interface IActivityLifecycleEvents
{
	event EventHandler<Bundle> Create;

	event EventHandler Stop;

	event EventHandler Start;
}
