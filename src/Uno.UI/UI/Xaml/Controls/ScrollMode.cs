using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// Specifies the scroll behavior in ScrollViewer control.
	/// </summary>
	public enum ScrollMode
	{
		/// <summary>
		/// Disables scrolling
		/// </summary>
		Disabled,

		/// <summary>
		/// Enables scrolling
		/// </summary>
		Enabled,

		/// <summary>
		/// Scrolling is enabled but behavior uses a "rails" manipulation mode
		/// </summary>
		Auto,
	}
}
