using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.Content.UITests.RichEditBoxControl
{
	[Sample("RichEditBox", Name = "RichEditBox_FormattingAccelerators", Description = "Ctrl+B/I/U formatting accelerators on Skia, gated by the DisabledFormattingAccelerators property.")]
	public sealed partial class RichEditBox_FormattingAccelerators : Page
	{
		public RichEditBox_FormattingAccelerators()
		{
			this.InitializeComponent();

			Editor.Document.SetText(TextSetOptions.None, "Select some text and press Ctrl+B, Ctrl+I or Ctrl+U.");
		}

		private void OnDisabledAcceleratorsChanged(object sender, RoutedEventArgs e)
		{
			var disabled = DisabledFormattingAccelerators.None;

			if (DisableBold.IsChecked == true)
			{
				disabled |= DisabledFormattingAccelerators.Bold;
			}

			if (DisableItalic.IsChecked == true)
			{
				disabled |= DisabledFormattingAccelerators.Italic;
			}

			if (DisableUnderline.IsChecked == true)
			{
				disabled |= DisabledFormattingAccelerators.Underline;
			}

			Editor.DisabledFormattingAccelerators = disabled;
		}

		private void OnInspectClick(object sender, RoutedEventArgs e)
		{
			try
			{
				var format = Editor.Document.Selection.CharacterFormat;
				var selection = Editor.Document.Selection;
				Output.Text =
					$"Selection: [{selection.StartPosition}, {selection.EndPosition}]\n" +
					$"Bold: {format.Bold}  Italic: {format.Italic}  Underline: {format.Underline}";
			}
			catch (Exception ex)
			{
				Output.Text = ex.Message;
			}
		}
	}
}
