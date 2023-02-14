using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
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
