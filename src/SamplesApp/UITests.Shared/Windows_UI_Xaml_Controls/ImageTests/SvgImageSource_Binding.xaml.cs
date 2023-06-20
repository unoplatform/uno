using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI.Samples.Controls;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;


namespace UITests.Windows_UI_Xaml_Controls.ImageTests;

[SampleControlInfo("Image", "SvgImageSource_Binding", typeof(ImageViewViewModel), isManualTest: true)]
public sealed partial class SvgImageSource_Binding : Page
{
	public SvgImageSource_Binding()
	{
		this.InitializeComponent();
	}
}
