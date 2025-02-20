namespace Microsoft.UI.Xaml.Media;

/// <summary>
/// A ThemeShadow is a preconfigured shadow effect that can be applied to any XAML element to draw shadows appropriately based on x,y,z coordinates.
/// ThemeShadow also automatically adjusts for other environmental specifications.
/// - Adapts to changes in lighting, user theme, app environment, and shell.
/// - Shadows elements automatically based on their elevation.
/// - Keeps elements in sync as they move and change elevation.
/// - Keeps shadows consistent throughout and across applications.
/// </summary>
public partial class ThemeShadow : Shadow
{
	/// <summary>
	/// Initializes a new instance of the ThemeShadow class.
	/// </summary>
	public ThemeShadow()
	{
	}

	/// <summary>
	/// Gets a collection of UI elements that this ThemeShadow is cast on.
	/// </summary>
	public UIElementWeakCollection Receivers { get; } = new UIElementWeakCollection();

	// Lifted Xaml had an old projected shadow code path that we're keeping around (disabled) as insurance. All shadows
	// are going through the drop shadow code path, hence we just return true here.
	internal static bool IsDropShadowMode => true;
}
