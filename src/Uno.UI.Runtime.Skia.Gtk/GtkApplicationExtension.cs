using System;
using Uno.UI.Xaml;
using Microsoft.UI.Xaml;

namespace Uno.UI.Runtime.Skia;

public class GtkApplicationExtension : IApplicationExtension
{
	private readonly Application _owner;

	public GtkApplicationExtension(Application owner)
	{
		_owner = owner ?? throw new ArgumentNullException(nameof(owner));

		var settings = Gtk.Settings.Default;
		settings.AddNotification(nameof(settings.ApplicationPreferDarkTheme), ApplicationPreferDarkThemeHandler);
	}

	public event EventHandler SystemThemeChanged;

	private void ApplicationPreferDarkThemeHandler(object o, GLib.NotifyArgs args)
		=> SystemThemeChanged?.Invoke(o, EventArgs.Empty);
}
