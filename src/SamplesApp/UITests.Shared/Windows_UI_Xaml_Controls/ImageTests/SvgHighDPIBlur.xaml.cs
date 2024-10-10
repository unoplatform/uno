using System;
using Windows.UI.Xaml.Controls;
using Uno.Extensions;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ImageTests;

[Sample("Image", IsManualTest = true, Description = "Try setting the scaling (DPI) to higher than 100%, the image should stay sharp in all configurations and should be similar to the quality on WinUI.")]
public sealed partial class SvgHighDPIBlur : Page
{
	public SvgHighDPIBlur()
	{
		this.InitializeComponent();
	}
}
