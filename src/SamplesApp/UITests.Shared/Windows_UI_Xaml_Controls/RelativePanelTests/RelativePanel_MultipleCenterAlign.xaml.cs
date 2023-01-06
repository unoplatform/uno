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
