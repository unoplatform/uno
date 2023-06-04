#nullable enable
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
			get => GtkHost.Current!.MainWindow!.Title;
			set => GtkHost.Current!.MainWindow!.Title = value;
		}

		public void ExitFullScreenMode()
		{
			GtkHost.Current!.MainWindow!.Unfullscreen();
		}

		public bool TryEnterFullScreenMode()
		{
			GtkHost.Current!.MainWindow!.Fullscreen();
			return true;
		}

		public bool TryResizeView(Size size)
		{
			GtkHost.Current!.MainWindow!.Resize((int)size.Width, (int)size.Height);
			return true;
		}

		public void SetPreferredMinSize(Size size)
		{
			GtkHost.Current!.MainWindow!.SetSizeRequest((int)size.Width, (int)size.Height);
		}
	}
}
