using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.Content.UITests.RichEditBoxControl
{
	[Sample("RichEditBox", Name = "RichEditBox_Basic", Description = "Functional RichEditBox on Skia: programmatic plain-text via Document, Header, PlaceholderText and IsReadOnly.")]
	public sealed partial class RichEditBox_Basic : Page
	{
		public RichEditBox_Basic()
		{
			this.InitializeComponent();
		}

		private void OnSetTextClick(object sender, RoutedEventArgs e)
		{
			try
			{
				Editor.Document.SetText(TextSetOptions.None, "The quick brown fox jumps over the lazy dog.");
				Output.Text = "Text set via Document.SetText.";
			}
			catch (Exception ex)
			{
				Output.Text = ex.Message;
			}
		}

		private void OnGetTextClick(object sender, RoutedEventArgs e)
		{
			try
			{
				Editor.Document.GetText(TextGetOptions.None, out var text);
				Output.Text = $"GetText: \"{text}\"";
			}
			catch (Exception ex)
			{
				Output.Text = ex.Message;
			}
		}

		private void OnReadOnlyToggled(object sender, RoutedEventArgs e)
		{
			Editor.IsReadOnly = ReadOnlyToggle.IsOn;
		}
	}
}
