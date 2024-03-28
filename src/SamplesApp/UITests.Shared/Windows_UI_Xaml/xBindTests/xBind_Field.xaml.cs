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

namespace UITests.Shared.Windows_UI_Xaml.xBindTests
{
	[SampleControlInfo("x:Bind", "XBind_Field")]
	public sealed partial class xBind_Field : UserControl
	{
		public xBind_Field()
		{
			this.InitializeComponent();
		}

		public string PublicField = "Enemy";
#pragma warning disable CS0414
		private string PrivateField = "Ryan";
#pragma warning restore CS0414
	}
}
