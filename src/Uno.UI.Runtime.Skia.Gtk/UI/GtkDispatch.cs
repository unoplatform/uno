#nullable disable

using System;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.GTK.UI;

internal static class GtkDispatch
{
	internal static void DispatchNativeSingle(Action d) =>
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
