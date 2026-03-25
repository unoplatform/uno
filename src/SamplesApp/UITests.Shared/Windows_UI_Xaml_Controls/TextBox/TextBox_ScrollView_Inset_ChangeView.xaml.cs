using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[Sample("TextBox", Name = "TextBox_ScrollView_Inset_ChangeView", IgnoreInSnapshotTests = true,
		Description = "ScrollView with SafeArea.Insets and multiline TextBox - tests scrolling and ChangeView when keyboard opens")]
	public sealed partial class TextBox_ScrollView_Inset_ChangeView : UserControl
	{
		public TextBox_ScrollView_Inset_ChangeView()
		{
			InitializeComponent();

			// Pre-populate with ~45 lines of text to require scrolling when the keyboard is open.
			var lines = new System.Text.StringBuilder();
			lines.AppendLine("Session Date: 03/24/2026");
			lines.AppendLine("Session Type: Direct Therapy");
			lines.AppendLine("Duration: 2 hours");
			lines.AppendLine();
			lines.AppendLine("Learner arrived on time and was in a good mood.");
			lines.AppendLine("Caregiver reported no significant changes since last session.");
			lines.AppendLine();
			lines.AppendLine("Target 1: Requesting with full sentences");
			lines.AppendLine("Prompted learner to use carrier phrases during snack time.");
			lines.AppendLine("Achieved 80% independent responses across 10 trials.");
			lines.AppendLine("This is an improvement from 65% last session.");
			lines.AppendLine();
			lines.AppendLine("Target 2: Following multi-step instructions");
			lines.AppendLine("Practiced 2-step instructions during play activities.");
			lines.AppendLine("Learner required gestural prompts on 3 out of 8 trials.");
			lines.AppendLine("Verbal praise was provided for each correct response.");
			lines.AppendLine();
			lines.AppendLine("Target 3: Tolerating transitions");
			lines.AppendLine("Used visual timer to signal upcoming transitions.");
			lines.AppendLine("Learner transitioned without problem behavior in 4 out of 5 opportunities.");
			lines.AppendLine("One instance of vocal protest during transition from preferred activity.");
			lines.AppendLine();
			lines.AppendLine("Behavior reduction: Elopement");
			lines.AppendLine("No instances of elopement observed during this session.");
			lines.AppendLine("Antecedent strategies (visual schedule, pre-session choices) were in place.");
			lines.AppendLine();
			lines.AppendLine("Behavior reduction: Property destruction");
			lines.AppendLine("One minor instance when asked to clean up art materials.");
			lines.AppendLine("Implemented planned response: blocked, redirected, provided choice.");
			lines.AppendLine("Learner recovered within 30 seconds and completed cleanup.");
			lines.AppendLine();
			lines.AppendLine("Parent training component:");
			lines.AppendLine("Reviewed prompting hierarchy with caregiver.");
			lines.AppendLine("Modeled least-to-most prompting during mand training.");
			lines.AppendLine("Caregiver practiced independently with feedback.");
			lines.AppendLine();
			lines.AppendLine("Data analysis notes:");
			lines.AppendLine("Created new targets in the care plan based on mastery criteria.");
			lines.AppendLine("Created new behavior reduction objectives in the care plan.");
			lines.AppendLine("Analyzed data trends across the last 5 sessions.");
			lines.AppendLine("Skill acquisition targets are progressing as expected.");
			lines.AppendLine();
			lines.AppendLine("Next session plan:");
			lines.AppendLine("Continue current targets with updated prompt levels.");
			lines.AppendLine("Introduce new social skill target: greeting peers.");

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
