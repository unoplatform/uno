using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UITests.Windows_UI_Xaml_Controls.CommandBar.Native_Frame;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml_Controls.CommandBar.BackButtonTitle
{
	[SampleControlInfo("CommandBar", "CommandBar_BackButtonTitle")]
	public sealed partial class CommandBar_Frame : UserControl
	{
		public CommandBar_Frame()
		{
			this.InitializeComponent();
		}

		private void AddGlobalStyle(object sender, RoutedEventArgs args)
		{
			Application.Current.Resources.Add(typeof(Windows.UI.Xaml.Controls.CommandBar), Application.Current.Resources["DefaultCommandBarStyle"]);
		}

		private void RemoveGlobalStyle(object sender, RoutedEventArgs args)
		{
			Application.Current.Resources.Remove(typeof(Windows.UI.Xaml.Controls.CommandBar));
		}

		private void Navigate_Initial(object sender, RoutedEventArgs args)
		{
			HostFrame.Navigate(typeof(CommandBar_Page1));
		}
	}
}
