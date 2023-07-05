using Gtk;
using Uno.UI.Core.Preview;
using Uno.UI.Runtime.Skia;

namespace Uno.Extensions.UI.Core.Preview;

internal class SystemNavigationManagerPreviewExtension : ISystemNavigationManagerPreviewExtension
{
	public SystemNavigationManagerPreviewExtension()
	{
	}

	public void RequestNativeAppClose() => GtkHost.Current!.MainWindow!.Close();
}
