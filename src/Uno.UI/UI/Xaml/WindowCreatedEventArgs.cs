using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;
namespace Microsoft.UI.Xaml
{
	public sealed partial class WindowCreatedEventArgs
	{
		internal WindowCreatedEventArgs(Window window)
			=> Window = window;

		public Window Window
		{
			get;
		}
	}
}
