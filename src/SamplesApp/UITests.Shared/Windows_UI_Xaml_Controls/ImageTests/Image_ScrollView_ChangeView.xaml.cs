using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.UITests.ImageTests
{
	[Sample("Image",
	 IsManualTest = true,
	 Description = "The sample showcases an image inside an scrollviewer. When the image is loaded, the scrollviewer changes its zoom factor.")]
	public sealed partial class Image_ScrollView_ChangeView : Page
	{
		public Image_ScrollView_ChangeView()
		{
			this.InitializeComponent();
		}

		private void Image_OnImageOpened(object sender, RoutedEventArgs e)
		{
			scrollViewer.ChangeView(0, 0, 0.5f);
		}
	}
}
