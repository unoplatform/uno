using System;
using Uno.UI.Xaml;
using Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Wpf
{
	public class WpfApplicationExtension : IApplicationExtension
	{
		private readonly Application _owner;

		public WpfApplicationExtension(Application owner)
		{
			_owner = owner ?? throw new ArgumentNullException(nameof(owner));
		}

		public bool CanExit => true;

		public void Exit() => System.Windows.Application.Current.Shutdown();
	}
}
