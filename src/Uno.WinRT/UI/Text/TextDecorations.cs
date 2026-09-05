using System;

namespace Windows.UI.Text
{
	/// <summary>
	/// Defines constants that specify the decorations applied to text.
	/// </summary>
	[Flags]
	public enum TextDecorations : uint
	{
		/// <summary>
		/// No text decorations are applied.
		/// </summary>
		None = 0U,

		/// <summary>
		/// Underline is applied to the text.
		/// </summary>
		Underline = 1U,

		/// <summary>
		/// Strikethrough is applied to the text.
		/// </summary>
		Strikethrough = 2U
	}
}
