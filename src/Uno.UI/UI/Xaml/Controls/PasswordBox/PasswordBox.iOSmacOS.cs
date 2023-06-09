using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls;

public partial class PasswordBox : TextBox
{
	// TODO: copy UpdateThemeBindings+UpdateKeyboardThemePartial impl when PasswordBox no longer inherits from TextBox

	partial void SetPasswordRevealState(PasswordRevealState state)
	{
		SetSecureTextEntry(state == PasswordRevealState.Obscured);
	}
}
