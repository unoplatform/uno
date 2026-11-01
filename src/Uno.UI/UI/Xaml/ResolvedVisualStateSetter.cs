#nullable enable

namespace Microsoft.UI.Xaml;

/// <summary>
/// A visual state setter resolved to the object and property it targets (VisualStateSetterHelper::ResolvedVisualStateSetter).
/// </summary>
internal readonly record struct ResolvedVisualStateSetter(DependencyObject TargetObject, DependencyProperty TargetProperty, object? Value);
