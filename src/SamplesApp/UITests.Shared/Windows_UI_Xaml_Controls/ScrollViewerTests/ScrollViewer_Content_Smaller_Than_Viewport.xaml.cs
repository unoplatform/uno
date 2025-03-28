using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ScrollViewerTests
{
	[Sample("Scrolling", Name = nameof(ScrollViewer_Content_Smaller_Than_Viewport), Description = "When the content of a ScrollViewer is smaller than the width or height of the ScrollViewer, the ScrollViewer should not be scrollable horizontally or vertically, respectively. To see this in action, resize SamplesApp to around 1000 pixels wide.")]
	public sealed partial class ScrollViewer_Content_Smaller_Than_Viewport : Page
	{
		public ScrollViewer_Content_Smaller_Than_Viewport()
		{
			this.InitializeComponent();
		}
	}
}
