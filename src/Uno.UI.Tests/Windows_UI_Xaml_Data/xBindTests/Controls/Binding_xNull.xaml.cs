using System;
using System.Collections.Generic;
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

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	public sealed partial class Binding_xNull : UserControl
	{
		public Binding_xNull()
		{
			this.InitializeComponent();
		}

		public DateTime Published => new DateTime(2000, 01, 01);

		public string MyMethod(string myString, object myParam)
		{
			return myString + " " + (myParam ?? "<null>");
		}
	}
}
