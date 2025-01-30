using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml
{
	public enum LineStackingStrategy
	{
		MaxHeight = 0,

		BlockLineHeight = 1,

		/// <summary>
		/// Not supported
		/// </summary>
		BaselineToBaseline = 2
	}
}
