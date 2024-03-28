using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[SampleControlInfo("TextBox", "TextBox_Simple", ignoreInSnapshotTests: true /*Cursor blinks in TextBox*/)]
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
