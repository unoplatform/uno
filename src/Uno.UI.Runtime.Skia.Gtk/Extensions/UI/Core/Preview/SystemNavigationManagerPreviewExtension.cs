#nullable enable

using Gtk;
using Uno.UI.Core.Preview;
using Uno.UI.Runtime.Skia.Gtk;

namespace Uno.Extensions.UI.Core.Preview;

internal class SystemNavigationManagerPreviewExtension : ISystemNavigationManagerPreviewExtension
{
	public SystemNavigationManagerPreviewExtension()
	{
	}

	public void RequestNativeAppClose() => GtkHost.Current!.InitialWindow!.Close();
}
