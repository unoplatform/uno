#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Popups
{
    public enum Placement
    {
		/// <summary>
		/// Place the context menu above the selection rectangle. 
		/// </summary>
		Default = 0,

		/// <summary>
		/// Place the context menu above the selection rectangle.
		/// </summary>
		Above = 1,

		/// <summary>
		/// Place the context menu below the selection rectangle.
		/// </summary>
		Below = 2,

		/// <summary>
		/// Place the context menu to the left of the selection rectangle.
		/// </summary>
		Left = 3,

		/// <summary>
		/// Place the context menu to the right of the selection rectangle.
		/// </summary>
		Right = 4,
    }
}
