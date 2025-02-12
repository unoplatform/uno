namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify the password reveal behavior of a PasswordBox.
/// </summary>
public enum PasswordRevealMode
{
	/// <summary>
	/// The password reveal button is visible. The password is not obscured while the button is pressed.
	/// </summary>
	Peek = 0,

	/// <summary>
	/// The password reveal button is not visible. The password is always obscured.
	/// </summary>
	Hidden = 1,

	/// <summary>
	/// The password reveal button is not visible. The password is not obscured.
	/// </summary>
	Visible = 2,
}
