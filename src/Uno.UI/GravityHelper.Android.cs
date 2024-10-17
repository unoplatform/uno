using System;
using System.Collections.Generic;
using System.Text;
using Android.Views;
using Windows.UI.Xaml;

namespace Uno.UI
{
	internal static class GravityHelper
	{
		internal static GravityFlags AlignmentsToGravity(VerticalAlignment verticalAlignment, HorizontalAlignment horizontalAlignment)
		{
			// Reset the gravity value
			var finalGravity = GravityFlags.NoGravity;

			switch (verticalAlignment)
			{
				case VerticalAlignment.Center:
					finalGravity |= GravityFlags.CenterVertical;
					break;

				case VerticalAlignment.Top:
					finalGravity |= GravityFlags.Top;
					break;

				case VerticalAlignment.Bottom:
					finalGravity |= GravityFlags.Bottom;
					break;
			}

			switch (horizontalAlignment)
			{
				case HorizontalAlignment.Center:
					finalGravity |= GravityFlags.CenterHorizontal;
					break;

				case HorizontalAlignment.Left:
					finalGravity |= GravityFlags.Left;
					break;

				case HorizontalAlignment.Right:
					finalGravity |= GravityFlags.Right;
					break;
			}

			return finalGravity;
		}
	}
}
