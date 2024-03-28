using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Controls.TextBlockControl
{
	[Sample("TextBlock")]
	public sealed partial class TextBlock_RelativeTextSize : Page
	{
		public TextBlock_RelativeTextSize()
		{
			this.InitializeComponent();

			Loaded += async (s, e) =>
			{
				await Task.Delay(100);
				result.Text = $"TextBlock Height={textBlock.ActualHeight}, TextBox Height={textBox.ActualHeight}. Windows is 164.";
			};
		}
	}
}
