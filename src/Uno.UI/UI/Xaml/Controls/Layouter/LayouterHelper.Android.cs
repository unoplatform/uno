using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Drawing;
using Uno.Extensions;
using Uno;
using Uno.Foundation.Logging;
using Uno.Collections;

using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
using System.Linq.Expressions;
using Uno.UI;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Provides access to internal methods for native View measuring.
	/// </summary>
	internal class LayouterHelper
	{
		internal static readonly UnsafeWeakAttachedDictionary<View, string> LayoutProperties = new UnsafeWeakAttachedDictionary<View, string>();

#pragma warning disable IL3050
		internal static readonly FastInvokerBuilder.FastInvokerHandler SetMeasuredDimensions = GetSetMeasuredDimensionMethod();
#pragma warning restore IL3050

		[RequiresDynamicCode("From DynamicMethod: Creating a DynamicMethod requires dynamic code.")]
		private static FastInvokerBuilder.FastInvokerHandler GetSetMeasuredDimensionMethod()
		{
			//
			// This method is present to work around the fact that the SetMeasuredMethod is protected, but
			// the layouter is an external type that controls the size of its children.
			//
			// This is required because the margin management is performed by the layouter, and not
			// by its children themselves.
			//
			// We generate a method using IL Emit for performance reasons.
			//

			var setMeasuredDimensionMethod = typeof(View)
				.GetMethod(
					"SetMeasuredDimension",
					global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.NonPublic
			);

			return FastInvokerBuilder.DynamicInvokerBuilder(setMeasuredDimensionMethod);
		}
	}
}
