namespace Windows.UI.Xaml.Controls;

public partial class PasswordBox
{
	partial void SetPasswordRevealState(PasswordRevealState state) => TextBoxView?.SetPasswordRevealState(state);

	partial void EndRevealPartial() => base.FocusTextView();
}
