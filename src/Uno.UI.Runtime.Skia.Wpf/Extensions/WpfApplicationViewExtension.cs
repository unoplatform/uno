using System;
using System.Windows;
using Windows.UI.ViewManagement;

using WpfApplication = System.Windows.Application;
using WpfWindow = System.Windows.Window;

namespace Uno.UI.Skia.Platform
{
	internal class WpfApplicationViewExtension : IApplicationViewExtension
	{
		private readonly ApplicationView _owner;

		public WpfApplicationViewExtension(object owner)
		{
			_owner = (ApplicationView)owner;
		}

		private bool _isFullScreen;
		private (WindowStyle WindowStyle, WindowState WindowState) _previousModes;

		public bool TryResizeView(Windows.Foundation.Size size)
		{
			if (WpfApplication.Current.MainWindow is not { } wpfMainWindow)
			{
				return false;
			}

			wpfMainWindow.Width = size.Width;
			wpfMainWindow.Height = size.Height;
			return true;
		}

		public bool TryEnterFullScreenMode()
		{
			if (WpfApplication.Current.MainWindow is not { } wpfMainWindow)
			{
				return false;
			}

			if (_isFullScreen || wpfMainWindow.WindowStyle == WindowStyle.None)
			{
				return false;
			}

			_previousModes = (wpfMainWindow.WindowStyle, wpfMainWindow.WindowState);

			wpfMainWindow.WindowStyle = WindowStyle.None;
			wpfMainWindow.WindowState = WindowState.Maximized;
			_isFullScreen = true;

			return true;
		}

		public void ExitFullScreenMode()
		{
			if (WpfApplication.Current.MainWindow is not { } wpfMainWindow || !_isFullScreen)
			{
				return;
			}

			_isFullScreen = false;
			wpfMainWindow.WindowStyle = _previousModes.WindowStyle;
			wpfMainWindow.WindowState = _previousModes.WindowState;
		}
	}
}
