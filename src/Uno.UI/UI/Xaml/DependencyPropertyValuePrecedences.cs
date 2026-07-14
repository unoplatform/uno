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
		/// Defined by an applied style, either explicitly assigned through the Style property or resolved as an implicit style.
		/// </summary>
		/// <remarks>Corresponds to WinUI's <c>BaseValueSourceStyle</c>.</remarks>
		Style,

		/// <summary>
		/// Defined by the built-in (default) style from Generic.xaml.
		/// </summary>
		/// <remarks>
		/// Corresponds to WinUI's <c>BaseValueSourceBuiltInStyle</c>. This takes precedence over inherited property
		/// values so that setters not covered by an applied style are still honored.
		/// </remarks>
		BuiltInStyle,

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
