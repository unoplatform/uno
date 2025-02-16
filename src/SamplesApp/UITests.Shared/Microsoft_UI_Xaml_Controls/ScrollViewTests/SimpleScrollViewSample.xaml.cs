using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.ScrollViewTests;

[Sample("Scrolling")]
public sealed partial class SimpleScrollViewSample : Page
{
	public SimpleScrollViewSample()
	{
		this.InitializeComponent();

		this.Loaded += (_, _) =>
		{
			scrollView.ScrollPresenter.Background = new SolidColorBrush(Windows.UI.Colors.Yellow);
		};
	}
}
