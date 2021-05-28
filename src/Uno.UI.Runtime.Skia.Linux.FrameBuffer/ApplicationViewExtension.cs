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
	}
}
