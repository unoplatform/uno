using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Xaml.Media.Animation
{
	internal static partial class AnimatorFactory
	{
		/// <summary>
		/// Creates the actual animator instance
		/// </summary>
		internal static IValueAnimator Create(Timeline timeline, double startingValue, double targetValue)
		{
			return new NotSupportedAnimator();
		}
	}
}
