using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests
{
	[Sample("TextBox")]
	public sealed partial class TextBox_Selection : UserControl
	{
		public TextBox_Selection()
		{
			this.InitializeComponent();
		}
		private void Select_OnClick(object sender, RoutedEventArgs args)
		{
			myTextBox.Focus(FocusState.Programmatic);
			myTextBox.Select((int)startNumber.Value, (int)lengthNumber.Value);
		}

		private void SelectReadOnly_OnClick(object sender, RoutedEventArgs args)
		{
			MyReadOnlyTextBox.Focus(FocusState.Programmatic);
			MyReadOnlyTextBox.Select((int)startNumber.Value, (int)lengthNumber.Value);
		}

		private void SelectAll_OnClick(object sender, RoutedEventArgs args)
		{
			myTextBox.Focus(FocusState.Programmatic);
			myTextBox.SelectAll();
		}
	}
}
