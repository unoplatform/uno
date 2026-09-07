using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ImageTests
{
	[Sample("Image", IsManualTest = true, Description = "All three images should load. The animated WebP and animated GIF should visibly cycle through colors. The static WebP should display normally.")]
	public sealed partial class Image_AnimatedWebP : Page
	{
		public Image_AnimatedWebP()
		{
			this.InitializeComponent();
		}
	}
}
