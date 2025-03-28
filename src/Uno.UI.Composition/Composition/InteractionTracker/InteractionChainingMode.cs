#nullable enable

namespace Windows.UI.Composition.Interactions;

public enum InteractionChainingMode
{
	/// <summary>
	/// Automatically determine whether to continue the manipulation.
	/// </summary>
	Auto = 0,

	/// <summary>
	/// Always continue the manipulation.
	/// </summary>
	Always = 1,

	/// <summary>
	/// Never continue the manipulation.
	/// </summary>
	Never = 2,
}
