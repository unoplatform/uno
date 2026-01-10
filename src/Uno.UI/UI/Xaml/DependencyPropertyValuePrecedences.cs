using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml
{
	public enum DependencyPropertyValuePrecedences : int
	{
		/// <summary>
		/// Defined by the value returned by PropertyMetadata.CoerceValueCallback (only if it differs from the input/base value)
		/// </summary>
		Coercion = 0,

		/// <summary>
		/// Defined by animation storyboards
		/// </summary>
		Animations,

		/// <summary>
		/// Values set directly on the Dependency Object
		/// </summary>
		Local,

		/// <summary>
		/// Defined when setting a style from the style property
		/// </summary>
		ExplicitOrImplicitStyle,

		/// <summary>
		/// Values defined by a default style
		/// </summary>
		DefaultStyle,

		/// <summary>
		/// Defined by the inheritance of a FrameworkElement property of the same name
		/// </summary>
		Inheritance,

		/// <summary>
		/// Defined on the dependency property metadata
		/// </summary>
		DefaultValue
	}
}
