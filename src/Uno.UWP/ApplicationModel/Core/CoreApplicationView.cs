using Windows.System;
using Windows.UI.Core;

namespace Windows.ApplicationModel.Core
{
	public partial class CoreApplicationView
	{
		private CoreApplicationViewTitleBar? _titleBar;

		public CoreApplicationView()
		{
		}

		public CoreWindow? CoreWindow => CoreWindow.Main;

		public CoreDispatcher Dispatcher => CoreDispatcher.Main;

		public DispatcherQueue? DispatcherQueue { get; } = DispatcherQueue.GetForCurrentThread();

		public CoreApplicationViewTitleBar TitleBar
		{
			get
			{
				if (_titleBar == null)
				{
					_titleBar = new CoreApplicationViewTitleBar();
				}

				return _titleBar;
			}
		}
	}
}
