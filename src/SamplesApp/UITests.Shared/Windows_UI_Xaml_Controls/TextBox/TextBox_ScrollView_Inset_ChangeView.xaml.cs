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

			NarrativeTextBox.Loaded += NarrativeTextBox_Loaded;
			
			// Pre-populate with ~45 lines of text to require scrolling when the keyboard is open.
			NarrativeTextBox.Text = new System.Text.StringBuilder()
				.AppendLine("Sample document header")
				.AppendLine("This is a multi-line text block used for UI testing.")
				.AppendLine("The content is domain-neutral and has no real-world meaning.")
				.AppendLine()
				.AppendLine("Line 1: The quick brown fox jumps over the lazy dog.")
				.AppendLine("Line 2: Lorem ipsum dolor sit amet, consectetur adipiscing elit.")
				.AppendLine("Line 3: Integer posuere erat a ante venenatis dapibus posuere velit.")
				.AppendLine("Line 4: Aenean lacinia bibendum nulla sed consectetur.")
				.AppendLine("Line 5: Donec sed odio dui, vestibulum at eros sit amet.")
				.AppendLine("Line 6: Praesent commodo cursus magna, vel scelerisque nisl consectetur.")
				.AppendLine("Line 7: Curabitur blandit tempus porttitor in non sem.")
				.AppendLine()
				.AppendLine("Section A")
				.AppendLine("Line 8: Vestibulum id ligula porta felis euismod semper.")
				.AppendLine("Line 9: Maecenas faucibus mollis interdum inceptos himenaeos.")
				.AppendLine("Line 10: Cras justo odio, dapibus ac facilisis in, egestas eget quam.")
				.AppendLine("Line 11: Nullam quis risus eget urna mollis ornare vel eu leo.")
				.AppendLine("Line 12: Donec ullamcorper nulla non metus auctor fringilla.")
				.AppendLine("Line 13: Maecenas sed diam eget risus varius blandit sit amet non magna.")
				.AppendLine()
				.AppendLine("Section B")
				.AppendLine("Line 14: Sed posuere consectetur est at lobortis.")
				.AppendLine("Line 15: Nulla vitae elit libero, a pharetra augue.")
				.AppendLine("Line 16: Etiam porta sem malesuada magna mollis euismod.")
				.AppendLine("Line 17: Duis mollis, est non commodo luctus, nisi erat porttitor ligula.")
				.AppendLine("Line 18: Cras mattis consectetur purus sit amet fermentum.")
				.AppendLine("Line 19: Morbi leo risus, porta ac consectetur ac, vestibulum at eros.")
				.AppendLine()
				.AppendLine("Section C")
				.AppendLine("Line 20: Phasellus volutpat metus eget massa efficitur, eu tempor nulla cursus.")
				.AppendLine("Line 21: Integer non turpis eu arcu fermentum maximus.")
				.AppendLine("Line 22: Nunc aliquet, justo non facilisis sollicitudin, mauris nisl fermentum elit.")
				.AppendLine("Line 23: Sed ut perspiciatis unde omnis iste natus error sit voluptatem.")
				.AppendLine("Line 24: At vero eos et accusamus et iusto odio dignissimos ducimus.")
				.AppendLine("Line 25: Neque porro quisquam est, qui dolorem ipsum quia dolor sit amet.")
				.AppendLine()
				.AppendLine("Footer notes")
				.AppendLine("Line 26: Additional sample text line to extend the document.")
				.AppendLine("Line 27: This line exists solely to ensure scrolling is required.")
				.AppendLine("Line 28: Another neutral filler sentence for UI layout validation.")
				.AppendLine("Line 29: The exact wording of these lines is not significant.")
				.AppendLine("Line 30: Only the length and multi-line structure are important.")
				.AppendLine("End of sample content for TextBox scrolling test.")
				.ToString();
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
