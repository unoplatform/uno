#nullable enable

using System;
using System.ComponentModel;
using UnoValidation = Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

[UnoValidation.ValidationProperty(nameof(Value))]
public partial class Slider : UnoValidation.IInputValidationControl
{
	/// <inheritdoc />
	public event EventHandler<DataErrorsChangedEventArgs> ErrorChanged
	{
		add => UnoValidation.Validation.AddErrorChangedHandler(this, value);
		remove => UnoValidation.Validation.RemoveErrorChangedHandler(this, value);
	}
}
