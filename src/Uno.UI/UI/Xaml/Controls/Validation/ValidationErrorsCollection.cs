#nullable enable

using Windows.Foundation.Collections;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Backs the Errors attached property of a validating control. Its identity is stable for the lifetime of
/// the control, so a binding to it survives every synchronization, which rewrites its contents in place.
/// </summary>
/// <remarks>
/// Created on demand, so a control that never reports an error never allocates one.
/// </remarks>
internal sealed class ValidationErrorsCollection : ObservableVector<InputValidationError>
{
}
