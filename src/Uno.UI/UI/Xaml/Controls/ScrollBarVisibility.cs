using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// Specifies the scroll bar visibility in ScrollViewer control.
	/// </summary>
	public enum ScrollBarVisibility
	{
		/// <summary>
		/// Disables scrolling.
		/// </summary>
		Disabled,

		/// <summary>
		/// Enables the scrollbars if the content is greater than the view port.
		/// </summary>
		Auto,

		/// <summary>
		/// Enables scrolling, but the scrollbars are not visible.
		/// </summary>
		Hidden,

		/// <summary>
		/// Enables scrolling, with scrollbars visible.
		/// </summary>
		Visible,
	}
}
