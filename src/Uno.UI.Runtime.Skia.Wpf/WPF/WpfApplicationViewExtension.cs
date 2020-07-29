using System;
using Windows.UI.ViewManagement;

namespace Uno.UI.Skia.Platform
{
	public class WpfApplicationViewExtension : IApplicationViewExtension
	{
		private readonly ApplicationView _owner;

		public WpfApplicationViewExtension(object owner)
		{
			_owner = (ApplicationView)owner;
		}

#if !DEBUG
#error TODO
#endif
		public string Title { get; set; }
		public bool TryEnterFullScreenMode() => throw new NotImplementedException();

		public void ExitFullScreenMode() => throw new NotImplementedException();
	}
}
