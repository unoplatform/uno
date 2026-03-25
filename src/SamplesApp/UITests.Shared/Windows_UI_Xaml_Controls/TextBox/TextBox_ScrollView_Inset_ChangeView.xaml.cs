using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[Sample("TextBox", Name = "TextBox_ScrollView_Inset_ChangeView", IgnoreInSnapshotTests = true,
		Description = "ScrollViewer with multiline TextBox - tests scrolling and ChangeView when keyboard opens")]
	public sealed partial class TextBox_ScrollView_Inset_ChangeView : UserControl
	{
		public TextBox_ScrollView_Inset_ChangeView()
		{
			InitializeComponent();

			// Pre-populate with ~45 lines of text to require scrolling when the keyboard is open.
			var lines = new System.Text.StringBuilder();
			lines.AppendLine("Sample document header");
			lines.AppendLine("This is a multi-line text block used for UI testing.");
			lines.AppendLine("The content is domain-neutral and has no real-world meaning.");
			lines.AppendLine();
			lines.AppendLine("Line 1: The quick brown fox jumps over the lazy dog.");
			lines.AppendLine("Line 2: Lorem ipsum dolor sit amet, consectetur adipiscing elit.");
			lines.AppendLine("Line 3: Integer posuere erat a ante venenatis dapibus posuere velit.");
			lines.AppendLine("Line 4: Aenean lacinia bibendum nulla sed consectetur.");
			lines.AppendLine("Line 5: Donec sed odio dui, vestibulum at eros sit amet.");
			lines.AppendLine("Line 6: Praesent commodo cursus magna, vel scelerisque nisl consectetur.");
			lines.AppendLine("Line 7: Curabitur blandit tempus porttitor in non sem.");
			lines.AppendLine();
			lines.AppendLine("Section A");
			lines.AppendLine("Line 8: Vestibulum id ligula porta felis euismod semper.");
			lines.AppendLine("Line 9: Maecenas faucibus mollis interdum inceptos himenaeos.");
			lines.AppendLine("Line 10: Cras justo odio, dapibus ac facilisis in, egestas eget quam.");
			lines.AppendLine("Line 11: Nullam quis risus eget urna mollis ornare vel eu leo.");
			lines.AppendLine("Line 12: Donec ullamcorper nulla non metus auctor fringilla.");
			lines.AppendLine("Line 13: Maecenas sed diam eget risus varius blandit sit amet non magna.");
			lines.AppendLine();
			lines.AppendLine("Section B");
			lines.AppendLine("Line 14: Sed posuere consectetur est at lobortis.");
			lines.AppendLine("Line 15: Nulla vitae elit libero, a pharetra augue.");
			lines.AppendLine("Line 16: Etiam porta sem malesuada magna mollis euismod.");
			lines.AppendLine("Line 17: Duis mollis, est non commodo luctus, nisi erat porttitor ligula.");
			lines.AppendLine("Line 18: Cras mattis consectetur purus sit amet fermentum.");
			lines.AppendLine("Line 19: Morbi leo risus, porta ac consectetur ac, vestibulum at eros.");
			lines.AppendLine();
			lines.AppendLine("Section C");
			lines.AppendLine("Line 20: Phasellus volutpat metus eget massa efficitur, eu tempor nulla cursus.");
			lines.AppendLine("Line 21: Integer non turpis eu arcu fermentum maximus.");
			lines.AppendLine("Line 22: Nunc aliquet, justo non facilisis sollicitudin, mauris nisl fermentum elit.");
			lines.AppendLine("Line 23: Sed ut perspiciatis unde omnis iste natus error sit voluptatem.");
			lines.AppendLine("Line 24: At vero eos et accusamus et iusto odio dignissimos ducimus.");
			lines.AppendLine("Line 25: Neque porro quisquam est, qui dolorem ipsum quia dolor sit amet.");
			lines.AppendLine();
			lines.AppendLine("Footer notes");
			lines.AppendLine("Line 26: Additional sample text line to extend the document.");
			lines.AppendLine("Line 27: This line exists solely to ensure scrolling is required.");
			lines.AppendLine("Line 28: Another neutral filler sentence for UI layout validation.");
			lines.AppendLine("Line 29: The exact wording of these lines is not significant.");
			lines.AppendLine("Line 30: Only the length and multi-line structure are important.");
			lines.AppendLine("End of sample content for TextBox scrolling test.");

			NarrativeTextBox.Text = lines.ToString();

			NarrativeTextBox.Loaded += NarrativeTextBox_Loaded;
		}

		private void NarrativeTextBox_Loaded(object sender, RoutedEventArgs e)
		{
			// Place cursor at the end so the user is typing at the bottom,
			// which is the area most likely to be covered by the keyboard.
			NarrativeTextBox.SelectionStart = NarrativeTextBox.Text.Length;
			NarrativeTextBox.Focus(FocusState.Programmatic);
		}
	}
}
