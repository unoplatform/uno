namespace Microsoft.UI.Content;

/// <summary>
/// Specifies the rounding methods used for converting screen coordinates (float to integer).
/// </summary>
public enum ContentCoordinateRoundingMode
{
	/// <summary>
	/// Use the current floating-point unit (FPU) setting. Default.
	/// </summary>
	Auto = 0,

	/// <summary>
	/// Rounds down to the largest integer less than or equal to the provided floating point number.
	/// </summary>
	Floor = 1,

	/// <summary>
	/// Rounds up or down, depending on the value of the provided floating point number.
	/// </summary>
	Round = 2,

	/// <summary>
	/// Rounds up to the smallest integer greater than or equal to the provided floating point number.
	/// </summary>
	Ceiling = 3,
}
