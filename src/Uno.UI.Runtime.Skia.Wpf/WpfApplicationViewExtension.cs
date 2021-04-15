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
		private WpfWindow _mainWpfWindow;

		public WpfApplicationViewExtension(object owner)
		{
			_owner = (ApplicationView)owner;

			// TODO: support many windows
			_mainWpfWindow = WpfApplication.Current.MainWindow;
		}

		public string Title
		{
			get => _mainWpfWindow.Title;
			set => _mainWpfWindow.Title = value;
		}

		private bool _isFullScreen = false;
		private (WindowStyle WindowStyle, WindowState WindowState) _previousModes;

		public bool TryEnterFullScreenMode()
		{
			if (_isFullScreen || _mainWpfWindow.WindowStyle == WindowStyle.None)
			{
				return false;
			}

			_previousModes = (_mainWpfWindow.WindowStyle, _mainWpfWindow.WindowState);

			_mainWpfWindow.WindowStyle = WindowStyle.None;
			_mainWpfWindow.WindowState = WindowState.Maximized;
			_isFullScreen = true;

			return true;
		}

		public void ExitFullScreenMode()
		{
			if (!_isFullScreen)
			{
				return;
			}

			_isFullScreen = false;
			_mainWpfWindow.WindowStyle = _previousModes.WindowStyle;
			_mainWpfWindow.WindowState = _previousModes.WindowState;
		}
	}
}
