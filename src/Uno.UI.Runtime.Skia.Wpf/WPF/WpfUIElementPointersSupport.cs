using System;
using Windows.UI.Core;

namespace Uno.UI.Skia.Platform
{
	public class WpfUIElementPointersSupport : ICoreWindowExtension
	{
		private CoreWindow _owner;
		private ICoreWindowEvents _ownerEvents;

		public WpfUIElementPointersSupport(object owner)
		{
			_owner = (CoreWindow)owner;
			_ownerEvents = (ICoreWindowEvents)owner;
		}
	}
}
