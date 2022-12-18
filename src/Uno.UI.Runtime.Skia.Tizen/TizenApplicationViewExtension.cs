using Windows.UI.ViewManagement;
using TizenWindow = Tizen.NUI.Window;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia
{
	internal class TizenApplicationViewExtension : IApplicationViewExtension
	{
		private readonly ApplicationView _owner;
		private readonly IApplicationViewEvents _ownerEvents;
		private readonly TizenWindow _window;

		public TizenApplicationViewExtension(object owner, TizenWindow window)
		{
			_owner = (ApplicationView)owner;
			_ownerEvents = (IApplicationViewEvents)owner;
			_window = window;
		}

		public string Title
		{
			get => _window.Title;
			set => _window.Title = value;
		}

		public void ExitFullScreenMode()
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning("FullScreen mode is not yet supported on Tizen.");
			}
		}

		public bool TryEnterFullScreenMode()
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning("FullScreen mode is not yet supported on Tizen.");
			}
			return false;
		}

		public bool TryResizeView(Windows.Foundation.Size size)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning("Resizing windows is not yet supported on Tizen.");
			}
			return false;
		}

		public void SetPreferredMinSize(Windows.Foundation.Size minSize)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning("Setting min size of windows is not yet supported on Tizen.");
			}
		}
	}
}
