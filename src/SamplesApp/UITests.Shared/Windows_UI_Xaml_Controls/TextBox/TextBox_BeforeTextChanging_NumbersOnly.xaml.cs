using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests
{
	[Sample("TextBox", IsManualTest = true, Description = "This is related to #15372. Try to move the cursor around and insert various characters and make sure only digits are actually submitted. The cursor should not move weirdly when inserting digits in the middle. This is relevant specifically to the Gtk and Wpf native textboxes.")]
	public sealed partial class TextBox_BeforeTextChanging_NumbersOnly : UserControl
	{
		public TextBox_BeforeTextChanging_NumbersOnly()
		{
			this.InitializeComponent();
		}

		private void OnBeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
		{
			args.Cancel = !double.TryParse(args.NewText, out double _);
		}
	}
}
