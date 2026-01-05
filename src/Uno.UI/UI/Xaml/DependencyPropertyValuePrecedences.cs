using System;
using System.Collections.Generic;
using System.ComponentModel;
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
		/// This is obsoleted and should not be used within the codebase.
		/// It is only kept for public API compatibility.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		TemplatedParent = 3,

		/// <summary>
		/// Defined when setting a style from the style property or resolving an implicit style
		/// </summary>
		ExplicitOrImplicitStyle = 4,

		[EditorBrowsable(EditorBrowsableState.Never)]
		ExplicitStyle = 4,

		[EditorBrowsable(EditorBrowsableState.Never)]
		ImplicitStyle = 5,

		/// <summary>
		/// Defined by the inheritance of a FrameworkElement property of the same name
		/// </summary>
		Inheritance,

		/// <summary>
		/// Values defined by a default style
		/// </summary>
		DefaultStyle,

		/// <summary>
		/// Defined on the dependency property metadata
		/// </summary>
		DefaultValue
	}
}
