using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Input
{
	public enum VirtualKeyModifiers
	{
		/// <summary>
		/// No virtual key modifier.
		/// </summary>
		None = 0,

		/// <summary>
		/// The Ctrl (control) virtual key.
		/// </summary>
		Control = 1,

		/// <summary>
		/// The Menu virtual key.
		/// </summary>
		Menu = 2,

		/// <summary>
		/// The Shift virtual key.
		/// </summary>
		Shift = 4,

		/// <summary>
		/// The Windows virtual key.
		/// </summary>
		Windows = 8,
	}
}
