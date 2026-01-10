using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.Samples.Content.UITests.TextBlockControl
{
	[Sample("TextBlock", "ForcedTextWithCarriageReturn_MaxLines_Two")]
	public sealed partial class ForcedTextWithCarriageReturn_MaxLines_Two : UserControl
	{
		public ForcedTextWithCarriageReturn_MaxLines_Two()
		{
			this.InitializeComponent();
			this.Loaded += Page_Loaded;
		}

		private void Page_Loaded(object sender, RoutedEventArgs e)
		{
			textBlock1.Text = "This text\nspans\nmultiple lines.";
		}
	}
}
