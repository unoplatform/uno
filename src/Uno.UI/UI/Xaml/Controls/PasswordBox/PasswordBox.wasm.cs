namespace Microsoft.UI.Xaml.Controls;

public partial class PasswordBox
{
	partial void SetPasswordRevealState(PasswordRevealState state) => TextBoxView?.SetPasswordRevealState(state);

	partial void EndRevealPartial() => base.FocusTextView();

	partial void OnPasswordCharChangedPartial(DependencyPropertyChangedEventArgs e)
	{
		// Update the password masking when the character changes
		TextBoxView?.UpdatePasswordMasking();
	}
}
