using System;

namespace Microsoft.UI.Xaml.Controls;

public partial class PasswordBox
{
	partial void OnPasswordCharChangedPartial(DependencyPropertyChangedEventArgs e)
	{
		if (string.IsNullOrEmpty(PasswordChar) || PasswordChar.Length != 1)
		{
			throw new ArgumentException("PasswordChar must be a single character string.");
		}

		// Force display update to refresh the password character
		TextBoxView?.UpdateDisplayBlockText(Text);
	}

	partial void SetPasswordRevealState(PasswordRevealState state) => TextBoxView?.SetPasswordRevealState(state);
}
