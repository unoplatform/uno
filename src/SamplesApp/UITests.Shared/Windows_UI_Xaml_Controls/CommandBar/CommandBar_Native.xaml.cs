using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Uno.UI.Samples.Content.UITests.CommandBar
{
	[SampleControlInfo("CommandBar", "Native")]
	public sealed partial class CommandBar_Native : UserControl
	{
		public CommandBar_Native()
		{
			this.InitializeComponent();

			DataContext = new CommandBarCommandViewModel();
		}

		private async void OnCommandClicked(object sender, RoutedEventArgs e)
		{
			await new Windows.UI.Popups.MessageDialog("Command clicked").ShowAsync();
		}
	}
}
