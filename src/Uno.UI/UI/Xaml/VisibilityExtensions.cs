using System;
using System.ComponentModel;

namespace Windows.UI.Xaml;

public static class VisibilityExtensions
{
	/// <summary>
	/// Determines if the specified visibility is hidden
	/// </summary>
	public static bool IsHidden(this Visibility visibility) => visibility == Visibility.Collapsed;
}

[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete]
public static class VisiblityExtensions
{
	// This class is for binary compatibility before the typo was corrected in its name.
	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete]
	public static bool IsHidden(Visibility visibility) => visibility.IsHidden();

}
