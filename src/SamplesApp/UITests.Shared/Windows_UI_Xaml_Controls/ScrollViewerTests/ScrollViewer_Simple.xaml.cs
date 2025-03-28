using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ScrollViewerTests
{
	[SampleControlInfo("Scrolling", nameof(ScrollViewer_Simple), typeof(ListViewViewModel))]
	public sealed partial class ScrollViewer_Simple : Page
	{
		public ScrollViewer_Simple()
		{
			this.InitializeComponent();
		}
	}
}
