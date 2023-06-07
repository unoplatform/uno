#nullable enable
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Uno.UI.Runtime.Skia
{
	internal class GtkApplicationViewExtension : IApplicationViewExtension
	{
		private readonly ApplicationView _owner;

		public GtkApplicationViewExtension(object owner)
		{
			_owner = (ApplicationView)owner;
		}

		public void ExitFullScreenMode()
		{
			GtkHost.Current!.MainWindow!.Unfullscreen();
		}

		public bool TryEnterFullScreenMode()
		{
			GtkHost.Current!.MainWindow!.Fullscreen();
			return true;
		}

		public bool TryResizeView(Size size)
		{
			GtkHost.Current!.MainWindow!.Resize((int)size.Width, (int)size.Height);
			return true;
		}
	}
}
