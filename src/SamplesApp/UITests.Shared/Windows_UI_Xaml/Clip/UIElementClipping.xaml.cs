using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml.UIElementTests
{
	[SampleControlInfo(category: "Clip")]
	public sealed partial class UIElementClipping : Page
	{
		public UIElementClipping()
		{
			this.InitializeComponent();
		}

		private void Open(object sender, RoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, "Opened", true);
		}

		private void Close(object sender, RoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, "Closed", true);
		}

		private void Rotate(object sender, RoutedEventArgs e)
		{
			RotateStoryboard.Begin();
		}

		private void FixHeight(object sender, RoutedEventArgs e)
		{
			ExpanderPresenter.Height = 50;
		}

		private void FixHeight300(object sender, RoutedEventArgs e)
		{
			ExpanderPresenter.Height = 300;
		}

		private void ReleaseHeight(object sender, RoutedEventArgs e)
		{
			ExpanderPresenter.ClearValue(HeightProperty);
		}

		private void FixWidth(object sender, RoutedEventArgs e)
		{
			ExpanderPresenter.Width = 50;
		}

		private void FixWidth300(object sender, RoutedEventArgs e)
		{
			ExpanderPresenter.Width = 300;
		}

		private void ReleaseWidth(object sender, RoutedEventArgs e)
		{
			ExpanderPresenter.ClearValue(WidthProperty);
		}
	}
}
