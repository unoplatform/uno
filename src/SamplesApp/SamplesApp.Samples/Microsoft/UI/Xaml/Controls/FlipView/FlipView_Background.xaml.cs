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

namespace UITests.Windows_UI_Xaml_Controls.FlipView;

[Sample(
	"FlipView",
	"FlipView_Background",
	Description = "Shows a green Page with a red FlipView in the middle. \n" +
	"This tests that the FlipView background is displayed when the FlipView is empty.",
	IsManualTest = true)]
public sealed partial class FlipView_Background : UserControl
{
	public FlipView_Background()
	{
		this.InitializeComponent();
	}
}
