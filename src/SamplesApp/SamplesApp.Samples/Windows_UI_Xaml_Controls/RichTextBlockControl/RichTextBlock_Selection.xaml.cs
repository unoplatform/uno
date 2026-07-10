using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.RichTextBlockControl
{
	[Sample("RichTextBlock", Name = "RichTextBlock_Selection")]
	public sealed partial class RichTextBlock_Selection : UserControl
	{
		public RichTextBlock_Selection()
		{
			this.InitializeComponent();
			ProgrammaticSelection.SelectionChanged += (s, e) =>
			{
				SelectedTextDisplay.Text = $"Selected: \"{ProgrammaticSelection.SelectedText}\"";
			};
		}

		private void OnSelectAllClick(object sender, RoutedEventArgs e)
		{
			ProgrammaticSelection.SelectAll();
			ProgrammaticSelection.Focus(FocusState.Programmatic);
		}

		private void OnCopyClick(object sender, RoutedEventArgs e)
		{
			ProgrammaticSelection.CopySelectionToClipboard();
		}
	}
}
