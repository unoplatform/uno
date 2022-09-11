#nullable enable

using Gtk;
using Uno.UI.Core.Preview;

namespace Uno.Extensions.UI.Core.Preview;

internal class SystemNavigationManagerPreviewExtension : ISystemNavigationManagerPreviewExtension
{
	private readonly Window _window;

	public SystemNavigationManagerPreviewExtension(Gtk.Window window)
	{
		_window = window;
	}

	public void RequestNativeAppClose() => _window.Close();
}
