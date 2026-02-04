using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Content.UITests.CommandBar.BackGesture;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Uno.UI.Samples.Content.UITests.CommandBar
{
	[Sample("CommandBar", Name = "BackGesture")]
	public sealed partial class CommandBar_BackGesture : Page
	{
		public CommandBar_BackGesture()
		{
			this.InitializeComponent();
		}

		private void FullWindow_Click(object sender, RoutedEventArgs e)
		{
			(XamlRoot?.Content as Microsoft.UI.Xaml.Controls.Frame).Navigate(typeof(BackGesture_Chooser));
		}
	}
}
