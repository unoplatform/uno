namespace Microsoft.UI.Xaml.Automation;

/// <summary>
/// Contains values that specify the style of bullets in a bulleted list.
/// </summary>
public enum AutomationBulletStyle
{
	/// <summary>
	/// No bullet style is applied.
	/// </summary>
	None = 0,

	/// <summary>
	/// A hollow round bullet.
	/// </summary>
	HollowRoundBullet = 1,

	/// <summary>
	/// A filled round bullet.
	/// </summary>
	FilledRoundBullet = 2,

	/// <summary>
	/// A hollow square bullet.
	/// </summary>
	HollowSquareBullet = 3,

	/// <summary>
	/// A filled square bullet.
	/// </summary>
	FilledSquareBullet = 4,

	/// <summary>
	/// A dash bullet.
	/// </summary>
	DashBullet = 5,

	/// <summary>
	/// A bullet style not covered by the other values.
	/// </summary>
	Other = 6,
}
