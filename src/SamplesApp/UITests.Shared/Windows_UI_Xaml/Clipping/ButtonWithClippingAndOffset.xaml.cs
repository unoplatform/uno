using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SamplesApp.Windows_UI_Xaml.Clipping
{
	[SampleControlInfo("Clipping", description: "Button with Clip set to offset rectangle. Touch should be detected correctly.")]
	public sealed partial class ButtonWithClippingAndOffset : UserControl
	{
		public ButtonWithClippingAndOffset()
		{
			this.InitializeComponent();
		}

		private int _counter;
		private void IncrementCounter(object sender, RoutedEventArgs e)
		{
			_counter++;
			ClickCountTextBlock.Text = $"Outer button clicked {_counter} times.";
		}

		private int _innerCounter;
		private void IncrementCounterInner(object sender, RoutedEventArgs e)
		{
			_innerCounter++;
			InnerClickCountTextBlock.Text = $"Inner button clicked {_innerCounter} times.";
		}
	}
}
