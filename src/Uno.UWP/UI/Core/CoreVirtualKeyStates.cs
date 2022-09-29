#nullable disable

using System;

namespace Windows.UI.Core
{
	/// <summary>
	/// Specifies flags for indicating the possible states of a virtual key.
	/// </summary>
	[Flags]
	public enum CoreVirtualKeyStates : uint
	{
		/// <summary>
		/// The key is up or in no specific state.
		/// </summary>
		None = 0U,
		/// <summary>
		/// The key is pressed down for the input event.
		/// </summary>
		Down = 1U,
		/// <summary>
		/// The key is in a toggled or modified state (for example, Caps Lock) for the input event.
		/// </summary>
		Locked = 2U
	}
}
