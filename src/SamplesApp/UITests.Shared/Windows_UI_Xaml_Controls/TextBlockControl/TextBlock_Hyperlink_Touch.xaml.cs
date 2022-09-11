using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBlockControl
{
	[SampleControlInfo("TextBlock", "TextBlock_Hyperlink_Touch")]
	public sealed partial class TextBlock_Hyperlink_Touch : UserControl
	{
		public TextBlock_Hyperlink_Touch()
		{
			this.InitializeComponent();
		}

		private void OnClick(object sender, RoutedEventArgs args)
		{
			var t = new Windows.UI.Popups.MessageDialog($"Clicked on {sender.GetType().Name}").ShowAsync();
		}
	}
}
