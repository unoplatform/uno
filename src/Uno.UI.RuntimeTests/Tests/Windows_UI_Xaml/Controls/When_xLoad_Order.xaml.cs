using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
