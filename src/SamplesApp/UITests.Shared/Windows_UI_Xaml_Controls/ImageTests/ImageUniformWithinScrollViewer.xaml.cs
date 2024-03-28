using System;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.UITests.ImageTestsControl
{
	[Sample("Image", Description = "ImageUniformWithinScrollViewer - text should appear below and not be crowded out by the empty Image control.")]
	public sealed partial class ImageUniformWithinScrollViewer : Page
	{
		public ImageUniformWithinScrollViewer()
		{
			this.InitializeComponent();
		}
	}
}
