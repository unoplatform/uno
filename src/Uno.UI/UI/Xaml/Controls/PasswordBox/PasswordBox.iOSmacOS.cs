using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class PasswordBox : TextBox
	{
		partial void SetPasswordScope(bool shouldHideText)
		{
			SetSecureTextEntry(shouldHideText);
		}
	}
}
