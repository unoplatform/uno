using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#if __APPLE_UIKIT__
using UIKit;
#endif
using Uno.UI;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ScrollViewerTests
{
	[Sample("ScrollViewer")]
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
#if HAS_UNO && !__WASM__ && !__SKIA__
			layout.Text = this.ShowLocalVisualTree();
#endif
		}
	}
}
