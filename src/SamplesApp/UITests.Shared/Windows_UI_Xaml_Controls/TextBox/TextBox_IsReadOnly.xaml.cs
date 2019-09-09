using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.UITests.TextBoxControl
{
	[SampleControlInfo("TextBox", "TextBox_IsReadOnly")]
	public sealed partial class TextBox_IsReadOnly : UserControl
	{
		public TextBox_IsReadOnly()
		{
			InitializeComponent();
		}

		private void OnClick(object sender, RoutedEventArgs args)
		{
			txt.IsReadOnly = !txt.IsReadOnly;
		}
	}
}
