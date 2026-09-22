#nullable enable

using System;
using System.ComponentModel;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Implemented by a control that reports the validation errors of its binding source.
/// </summary>
/// <remarks>
/// Uno only, and public so that third-party controls can participate. A control also has to declare its
/// input through <see cref="InputValidationPropertyAttribute"/>; implementing this interface on its own reports
/// nothing.
/// </remarks>
public interface IInputValidationControl
{
	/// <summary>
	/// Raised when the binding source reports a change to the errors of the validated property.
	/// </summary>
	/// <remarks>
	/// Carries the <see cref="DataErrorsChangedEventArgs"/> of the source unchanged, so that a consumer of
	/// several controls sharing one source can tell which property changed.
	/// </remarks>
	event EventHandler<DataErrorsChangedEventArgs> ErrorChanged;
}
