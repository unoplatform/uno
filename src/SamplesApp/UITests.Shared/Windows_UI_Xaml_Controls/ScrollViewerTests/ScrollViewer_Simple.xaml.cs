using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ScrollViewerTests
{
	[Sample("Scrolling", nameof(ScrollViewer_Simple), typeof(ListViewViewModel))]
	public sealed partial class ScrollViewer_Simple : Page
	{
		public ScrollViewer_Simple()
		{
			this.InitializeComponent();
		}
	}
}
