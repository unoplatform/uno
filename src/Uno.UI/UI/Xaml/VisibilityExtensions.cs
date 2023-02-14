using System;
using System.ComponentModel;

namespace Microsoft.UI.Xaml;

public static class VisibilityExtensions
{
	/// <summary>
	/// Determines if the specified visibility is hidden
	/// </summary>
	public static bool IsHidden(this Visibility visibility) => visibility == Visibility.Collapsed;
}

[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete("'VisiblityExtensions' is obsolete. Use 'VisibilityExtensions' instead.")]
public static class VisiblityExtensions
{
	// This class is for binary compatibility before the typo was corrected in its name.
	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("'VisiblityExtensions.IsHidden' is obsolete. Use 'VisibilityExtensions.IsHidden' instead.")]
	public static bool IsHidden(Visibility visibility) => visibility.IsHidden();

}
