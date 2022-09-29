#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class PasswordBox
	{
		partial void SetPasswordScope(bool shouldHideText)
		{
			SetIsPassword(shouldHideText);
		}

		partial void EndRevealPartial() => base.FocusTextView();
	}
}
