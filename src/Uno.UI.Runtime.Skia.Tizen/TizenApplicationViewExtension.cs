using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Windows.UI.ViewManagement;
using TizenWindow = ElmSharp.Window;

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
	}
}
