using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Uno.UI.Runtime.Skia
{
	internal class GtkApplicationViewExtension : IApplicationViewExtension
	{
		private readonly ApplicationView _owner;
		private readonly IApplicationViewEvents _ownerEvents;

		public GtkApplicationViewExtension(object owner)
		{
			_owner = (ApplicationView)owner;
			_ownerEvents = (IApplicationViewEvents)owner;
		}

		public string Title
		{
			get => GtkHost.Window.Title;
			set => GtkHost.Window.Title = value;
		}

		public void ExitFullScreenMode()
		{
			GtkHost.Window.Unfullscreen();
		}

		public bool TryEnterFullScreenMode()
		{
			GtkHost.Window.Fullscreen();
			return true;
		}

		public bool TryResizeView(Size size)
		{
			GtkHost.Window.Resize((int)size.Width, (int)size.Height);
			return true;
		}

		public void SetPreferredMinSize(Size size)
		{
			GtkHost.Window.SetSizeRequest((int)size.Width, (int)size.Height);
		}
	}
}
