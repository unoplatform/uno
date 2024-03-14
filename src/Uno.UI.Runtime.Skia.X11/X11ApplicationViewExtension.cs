using System;
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
				using var _1 = X11Helper.XLock(host.X11Window.Display);
				var _2 = XLib.XResizeWindow(host.X11Window.Display, host.X11Window.Window, (int)size.Width, (int)size.Height);
			}
			return false;
		}
	}
}
