using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace Uno.UI.Samples.Content.UITests.CommandBar
{
	[Sample("CommandBar", Name = "Native")]
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
