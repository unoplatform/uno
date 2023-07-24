using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Uno.UI.Samples.Content.UITests.ButtonTestsControl
{
	[Sample("Buttons", Name="Button_Inside_Border")]
	public sealed partial class Button_Inside_Border : UserControl
	{
		public Button_Inside_Border()
		{
			this.InitializeComponent();
		}

		private void Border_Tapped(object sender, RoutedEventArgs e)
		{
			tb.Text += $"\n@xy Border_Tapped: {sender.GetType().Name}, [{e.GetHashCode().ToString("X8")}]{e.OriginalSource.GetType().Name}";
		}

		private void Button_Tapped(object sender, TappedRoutedEventArgs e)
		{
			tb.Text += $"\n@xy Button_Tapped: {sender.GetType().Name}, [{e.GetHashCode().ToString("X8")}]{e.OriginalSource.GetType().Name}";
		}

		private void Border_RightTapped(object sender, RoutedEventArgs e)
		{
			tb.Text += $"\n@xy Border_RightTapped: {sender.GetType().Name}, [{e.GetHashCode().ToString("X8")}]{e.OriginalSource.GetType().Name}";
		}

		private void Button_RightTapped(object sender, RoutedEventArgs e)
		{
			tb.Text += $"\n@xy Button_RightTapped: {sender.GetType().Name}, [{e.GetHashCode().ToString("X8")}]{e.OriginalSource.GetType().Name}";
		}
		
		private void Border_Holding(object sender, RoutedEventArgs e)
		{
			tb.Text += $"\n@xy Border_Holding: {sender.GetType().Name}, [{e.GetHashCode().ToString("X8")}]{e.OriginalSource.GetType().Name}";
		}

		private void Button_Holding(object sender, RoutedEventArgs e)
		{
			tb.Text += $"\n@xy Button_Holding: {sender.GetType().Name}, [{e.GetHashCode().ToString("X8")}]{e.OriginalSource.GetType().Name}";
		}
	}
}
