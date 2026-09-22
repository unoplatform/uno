#nullable enable

using System;
using System.ComponentModel;
using UnoValidation = Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

[UnoValidation.InputValidationProperty(nameof(Password))]
public partial class PasswordBox : UnoValidation.IInputValidationControl
{
	/// <inheritdoc />
	public event EventHandler<DataErrorsChangedEventArgs> ErrorChanged
	{
		add => UnoValidation.Validation.AddErrorChangedHandler(this, value);
		remove => UnoValidation.Validation.RemoveErrorChangedHandler(this, value);
	}
}
