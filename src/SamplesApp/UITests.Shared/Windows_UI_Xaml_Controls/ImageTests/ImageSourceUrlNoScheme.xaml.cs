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

namespace Uno.UI.Samples.UITests.ImageTests
{

	[SampleControlInfo(category: "Image", controlName: nameof(ImageSourceUrlNoScheme), Description = "ImageNoScheme")]
	public sealed partial class ImageSourceUrlNoScheme : Page
	{
		public ImageSourceUrlNoScheme()
		{
			this.InitializeComponent();
		}
	}
}
