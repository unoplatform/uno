using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

namespace UITests.Shared.Windows_UI_Xaml.xBindTests
{
	[Sample("x:Bind", Name = "XBind_Field")]
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
