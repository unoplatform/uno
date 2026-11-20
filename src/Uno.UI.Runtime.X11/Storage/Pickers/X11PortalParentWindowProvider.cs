#nullable enable

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Uno.UI.Runtime.Linux;

namespace Uno.UI.Runtime.X11;

internal sealed class X11PortalParentWindowProvider : IPortalParentWindowProvider
{
	public static X11PortalParentWindowProvider Instance { get; } = new();

	private X11PortalParentWindowProvider()
	{
	}

	public Task<PortalParentWindow?> GetParentWindowAsync(CancellationToken token)
		=> Task.FromResult(X11Helper.XamlRootHostFromApplicationView(ApplicationView.GetForCurrentViewSafe(), out var host)
			? new PortalParentWindow("x11:" + host.RootX11Window.Window.ToString("X", CultureInfo.InvariantCulture))
			: null);
}
