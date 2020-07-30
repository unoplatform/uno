using System;
using Windows.UI.ViewManagement;

using WpfApplication = System.Windows.Application;
using WpfWindow = System.Windows.Window;

namespace Uno.UI.Skia.Platform
{
	public class WpfApplicationViewExtension : IApplicationViewExtension
	{
		private readonly ApplicationView _owner;
		private WpfWindow _mainWpfWindow;

		public WpfApplicationViewExtension(object owner)
		{
			_owner = (ApplicationView)owner;

			// TODO: support many windows
			_mainWpfWindow = WpfApplication.Current.MainWindow;
		}

#if !DEBUG
#error TODO
#endif
		public string Title
		{
			get => _mainWpfWindow.Title;
			set => _mainWpfWindow.Title = value;
		}

		public bool TryEnterFullScreenMode() => throw new NotImplementedException();

		public void ExitFullScreenMode() => throw new NotImplementedException();
	}
}
