using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml
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
		/// Values inherited from the templated parent
		/// </summary>
		TemplatedParent,

		/// <summary>
		/// Defined when setting a style from the style property
		/// </summary>
		ExplicitStyle,

		/// <summary>
		/// Values defined by an implicitly defined style
		/// </summary>
		/// <remarks>On Uno, this is actually used for values set by the default style, to allow for them to correctly take precedence over inherited values.</remarks>
		ImplicitStyle,

		/// <summary>
		/// Defined by the inheritance of a FrameworkElement property of the same name
		/// </summary>
		Inheritance,

		/// <summary>
		/// Defined by the default style from Generic.xaml
		/// </summary>
		DefaultStyle,

		/// <summary>
		/// Defined on the dependency property metadata
		/// </summary>
		DefaultValue
	}
}
