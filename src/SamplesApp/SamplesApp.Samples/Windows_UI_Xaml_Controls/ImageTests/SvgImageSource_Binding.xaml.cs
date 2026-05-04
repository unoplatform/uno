using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI.Samples.Controls;
using Windows.Storage;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;


namespace UITests.Windows_UI_Xaml_Controls.ImageTests;

[Sample("Image", Name = "SvgImageSource_Binding", ViewModelType = typeof(ImageViewViewModel), IsManualTest = true)]
public sealed partial class SvgImageSource_Binding : Page
{
	public SvgImageSource_Binding()
	{
		this.InitializeComponent();
	}
}
