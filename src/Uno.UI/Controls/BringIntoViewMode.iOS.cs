using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.Extensions
{
    public enum BringIntoViewMode
    {
		/// <summary>
		/// ScrollView is scroll in order to get the view visible either at the top or the bottom of the view port.
		/// </summary>
		ClosestEdge,

		/// <summary>
		/// ScrollView is scroll in order to get the view at the top / left of view port.
		/// </summary>
		TopLeftOfViewPort,

		/// <summary>
		/// ScrollView is scroll in order to get the center of the view at the middle of view port.
		/// </summary>
		CenterOfViewPort,

		/// <summary>
		/// ScrollView is scroll in order to get the view at the bottom / right of view port.
		/// </summary>
		BottomRightOfViewPort,
    }
}
