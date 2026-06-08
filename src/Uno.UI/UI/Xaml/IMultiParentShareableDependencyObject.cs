namespace Microsoft.UI.Xaml;

internal interface IMultiParentShareableDependencyObject;

/* This interface is used to mark DependencyObjects that can be shared across multiple "parents".
 * While they CAN participate in data-context inheritance/propagation,
 * this only works until they are shared by more than one parent.
 * And, once multiple parents are detected, the DC inheritance/propagation is forever disabled on this object.
 */
