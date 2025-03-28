using Android.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls;

public partial class PasswordBox : TextBox
{
	partial void SetPasswordRevealState(PasswordRevealState state)
	{
		if (state == PasswordRevealState.Obscured)
		{
			var scopeName = InputScope.GetFirstInputScopeNameValue();

			if (scopeName == Input.InputScopeNameValue.Number || scopeName == Input.InputScopeNameValue.NumericPin)
			{
				SetInputScope(InputTypes.NumberVariationPassword | InputTypes.ClassNumber);
			}
			else
			{
				SetInputScope(InputTypes.TextVariationPassword | InputTypes.ClassText);
			}
		}
		else
		{
			SetInputScope(InputTypes.ClassText);
		}

		if (Password != null)
		{
			SetSelection(Password.Length);
		}
	}
}
