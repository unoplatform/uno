using System;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;

namespace Uno.UI.Runtime.Skia.Gtk.UI;

internal static class GtkDispatch
{
	internal static void DispatchNativeSingle(Action d, NativeDispatcherPriority p) =>
		GLib.Idle.Add(delegate
		{
			if (typeof(GtkDispatch).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(GtkDispatch).Log().Trace($"Dispatch Iteration");
			}

			try
			{
				d();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}

			return false;
		});
}
