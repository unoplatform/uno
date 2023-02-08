using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls;

public partial class PasswordBox
{
	partial void SetPasswordRevealState(PasswordRevealState state) => _textBoxView?.SetPasswordRevealState(state);

	partial void EndRevealPartial() => base.FocusTextView();
}
