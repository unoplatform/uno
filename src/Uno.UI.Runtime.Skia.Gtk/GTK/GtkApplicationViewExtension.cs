#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.ViewManagement;

namespace Uno.UI.Runtime.Skia
{
	public class GtkApplicationViewExtension : IApplicationViewExtension
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
	}
}
