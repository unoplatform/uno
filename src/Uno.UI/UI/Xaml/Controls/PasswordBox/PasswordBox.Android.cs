using Android.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class PasswordBox : TextBox
	{
		partial void SetPasswordScope(bool shouldHideText)
		{
			if (shouldHideText)
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
}
