using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UITests.Windows_UI_Xaml_Controls.CommandBar.Native_Frame;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml_Controls.CommandBar.BackButtonTitle
{
	[Sample("CommandBar", Name = "CommandBar_BackButtonTitle")]
	public sealed partial class CommandBar_Frame : UserControl
	{
		public CommandBar_Frame()
		{
			this.InitializeComponent();
		}

		private void AddGlobalStyle(object sender, RoutedEventArgs args)
		{
			Application.Current.Resources.Add(typeof(Microsoft.UI.Xaml.Controls.CommandBar), Application.Current.Resources["DefaultCommandBarStyle"]);
		}

		private void RemoveGlobalStyle(object sender, RoutedEventArgs args)
		{
			Application.Current.Resources.Remove(typeof(Microsoft.UI.Xaml.Controls.CommandBar));
		}

		private void Navigate_Initial(object sender, RoutedEventArgs args)
		{
			HostFrame.Navigate(typeof(CommandBar_Page1));
		}
	}
}
