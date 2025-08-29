namespace Microsoft.UI.Xaml.Controls;

public partial class PasswordBox
{
	partial void SetPasswordRevealState(PasswordRevealState state) => TextBoxView?.SetPasswordRevealState(state);

	partial void OnPasswordCharChangedPartial(DependencyPropertyChangedEventArgs e)
	{
		// Update the password character when it changes
		if (TextBoxView != null)
		{
			TextBoxView.UpdateDisplayBlockText(Text);
		}
	}
}
