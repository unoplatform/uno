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

namespace UITests.Windows_UI_Xaml_Controls.RelativePanelTests
{
	[Sample("RelativePanel", Description = "Both ellipse and text should be vertically centered within their container and the app should not crash")]
	public sealed partial class RelativePanel_MultipleCenterAlign : Page
	{
		public RelativePanel_MultipleCenterAlign()
		{
			this.InitializeComponent();
		}
	}
}
