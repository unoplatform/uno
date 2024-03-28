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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml.xLoadTests
{
	[Sample("x:Load", Name = "xLoad_FindName")]
	public sealed partial class xLoad_FindName : UserControl
	{
		public xLoad_FindName()
		{
			this.InitializeComponent();
			LoadButton.Click += LoadButton_Click;
		}

		private void LoadButton_Click(object sender, RoutedEventArgs e)
		{
			FindName("LoadBorder");
		}
	}
}
