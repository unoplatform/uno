using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using UIKit;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ScrollViewerTests
{
	[SampleControlInfo(category: "ScrollViewer")]
	public sealed partial class ScrollViewer_Padding : Page
	{
		public ScrollViewer_Padding()
		{
			this.InitializeComponent();

			Loaded += OnLoaded;
		}

		private async void OnLoaded(object sender, RoutedEventArgs e)
		{
			await Task.Delay(300);
			layout.Text = this.ShowLocalVisualTree();
		}
	}
}
