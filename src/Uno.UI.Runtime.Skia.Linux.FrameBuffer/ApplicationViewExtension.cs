using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Uno.UI.Runtime.Skia
{
	internal class ApplicationViewExtension : IApplicationViewExtension
	{
		private readonly ApplicationView _owner;
		private readonly IApplicationViewEvents _ownerEvents;

		public ApplicationViewExtension(object owner)
		{
			_owner = (ApplicationView)owner;
			_ownerEvents = (IApplicationViewEvents)owner;
		}

		public string Title
		{
			get => "";
			set { }
		}

		public void ExitFullScreenMode()
		{
		}

		public bool TryEnterFullScreenMode()
		{
			return true;
		}

		public bool TryResizeView(Size size)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning("Resizing windows is not yet supported on Linux frame buffer.");
			}
			return false;
		}

		public void SetPreferredMinSize(Size minSize)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning("Setting min size of windows is not yet supported on Linux frame buffer.");
			}
		}
	}
}
