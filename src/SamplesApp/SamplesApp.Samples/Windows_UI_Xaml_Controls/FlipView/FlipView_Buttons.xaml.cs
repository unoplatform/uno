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

namespace UITests.Windows_UI_Xaml_Controls.FlipView
{
	[Sample("FlipView", IsManualTest = true, Description = "Flipping should be tested using touch swiping on Wasm")]
	public sealed partial class FlipView_Buttons : UserControl
	{
		public FlipView_Buttons()
		{
			this.InitializeComponent();
		}
	}
}
