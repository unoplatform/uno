using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBlockControl
{
	[Sample("TextBlock", "SimpleText_MaxLines_Different_Font_Size")]
	public sealed partial class SimpleText_MaxLines_Different_Font_Size : UserControl
	{
		public SimpleText_MaxLines_Different_Font_Size()
		{
			this.InitializeComponent();
		}

		private void MaxLinesUp(object sender, object args)
		{
			container1.MaxLines += 1;
			container2.MaxLines += 1;
			container3.MaxLines += 1;
		}

		private void MaxLinesDown(object sender, object args)
		{
			container1.MaxLines -= 1;
			container2.MaxLines -= 1;
			container3.MaxLines -= 1;
		}
	}
}
