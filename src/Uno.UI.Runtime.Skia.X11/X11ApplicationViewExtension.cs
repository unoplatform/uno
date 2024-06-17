using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Uno.WinUI.Runtime.Skia.X11
{
	internal class X11ApplicationViewExtension(object owner) : IApplicationViewExtension
	{
		private readonly ApplicationView _owner = (ApplicationView)owner;

		public bool TryResizeView(Size size)
		{
			if (X11Helper.XamlRootHostFromApplicationView(_owner, out var host))
			{
				using var rootLockDiposable = X11Helper.XLock(host.RootX11Window.Display);
				_ = XLib.XResizeWindow(host.RootX11Window.Display, host.RootX11Window.Window, (int)size.Width, (int)size.Height);
				using var topLockDiposable = X11Helper.XLock(host.TopX11Window.Display);
				_ = XLib.XResizeWindow(host.TopX11Window.Display, host.TopX11Window.Window, (int)size.Width, (int)size.Height);
			}
			return false;
		}
	}
}
