using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls
{
	public sealed partial class When_xLoad_Order : UserControl
	{
		public When_xLoad_Order()
		{
			this.InitializeComponent();
		}

		public bool IsLoaded1 { get; set; }
		public bool IsLoaded2 { get; set; }
		public bool IsLoaded3 { get; set; }

		public void Refresh()
		{
			Bindings.Update();
		}
	}
}
