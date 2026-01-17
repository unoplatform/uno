using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[Sample("TextBox", "TextBox_Simple", IgnoreInSnapshotTests: true /*Cursor blinks in TextBox*/)]
	public sealed partial class TextBox_Simple : UserControl
	{
		public TextBox_Simple()
		{
			InitializeComponent();

			Focused.Loaded += Focused_Loaded;
		}

		private void Focused_Loaded(object sender, RoutedEventArgs e)
		{
			Focused.Focus(FocusState.Programmatic);
		}
	}
}
