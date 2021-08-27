using Windows.UI.Core;

namespace Windows.ApplicationModel.Core
{
	public partial class CoreApplicationView
	{
		private CoreApplicationViewTitleBar _titleBar;

		public CoreApplicationView()
		{
		}

#pragma warning disable CA1822 // Mark members as static - align with UWP
		public CoreWindow CoreWindow => CoreWindow.Main;

		public CoreDispatcher Dispatcher => CoreDispatcher.Main;
#pragma warning restore CA1822 // Mark members as static

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
