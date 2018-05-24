using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Popups
{
	public enum MessageDialogOptions
	{
		/// <summary> 
		/// No options are specified and default behavior is used.
		/// </summary>
		None = 0,

		/// <summary>
		/// Ignore user input for a short period. This enables browsers to defend against clickjacking.
		/// </summary>
		AcceptUserInputAfterDelay = 1,
    }
}
