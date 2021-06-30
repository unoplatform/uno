using System;
using Uno.UI.Xaml;
using Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia
{
	public class GtkApplicationExtension : IApplicationExtension
	{
		private readonly Application _owner;

		public GtkApplicationExtension(Application owner)
		{
			_owner = owner ?? throw new ArgumentNullException(nameof(owner));
		}

		public bool CanExit => true;

		public void Exit() => Gtk.Application.Default.Quit();
	}
}
