using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Text
{
	[Flags]
	public enum TextDecorations
	{
		/// <summary>
		/// No text decorations are applied.
		/// </summary>
		None = 0,

		/// <summary>
		/// Underline is applied to the text.
		/// </summary>
		Underline = 1,

		/// <summary>
		/// Strikethrough is applied to the text.
		/// </summary>
		Strikethrough = 2,
	}
}
