using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
using Windows.UI.Popups;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.Samples.Content.UITests.CommandBar
{
	[SampleControlInfo("CommandBar", "Examples")]
	public sealed partial class CommandBar_Examples : UserControl
	{
		public CommandBar_Examples()
		{
			this.InitializeComponent();
		}

		private async void OnCommandClicked(object sender, RoutedEventArgs e)
		{
			await new Windows.UI.Popups.MessageDialog("Clicked").ShowAsync();
		}
	}
}
