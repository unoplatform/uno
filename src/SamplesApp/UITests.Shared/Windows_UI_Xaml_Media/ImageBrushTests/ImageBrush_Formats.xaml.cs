using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using UITests.Windows_UI_Xaml_Controls.ImageTests;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Media.ImageBrushTests
{
	[Sample("Brushes")]
	public sealed partial class ImageBrush_Formats : Page
	{
		public ImageBrush_Formats()
		{
			this.InitializeComponent();
		}

		private Dictionary<string, string> Images => Image_Formats.Formats;
	}
}
