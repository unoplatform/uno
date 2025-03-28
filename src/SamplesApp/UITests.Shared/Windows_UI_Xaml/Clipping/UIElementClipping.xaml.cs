using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace SamplesApp.Windows_UI_Xaml.Clipping
{
	[SampleControlInfo(category: "Clipping")]
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

		private void RotateClip(object sender, RoutedEventArgs e)
		{
			RotateClipStoryboard.Begin();
		}

		private void FixHeight(object sender, RoutedEventArgs e)
		{
			ContainingBorder.Height = 50;
		}

		private void FixHeight300(object sender, RoutedEventArgs e)
		{
			ContainingBorder.Height = 300;
		}

		private void ReleaseHeight(object sender, RoutedEventArgs e)
		{
			ContainingBorder.ClearValue(HeightProperty);
		}

		private void FixWidth(object sender, RoutedEventArgs e)
		{
			ContainingBorder.Width = 50;
		}

		private void FixWidth300(object sender, RoutedEventArgs e)
		{
			ContainingBorder.Width = 300;
		}

		private void ReleaseWidth(object sender, RoutedEventArgs e)
		{
			ContainingBorder.ClearValue(WidthProperty);
		}

		protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize);

		protected override Size ArrangeOverride(Size finalSize) => base.ArrangeOverride(finalSize);
	}
}
