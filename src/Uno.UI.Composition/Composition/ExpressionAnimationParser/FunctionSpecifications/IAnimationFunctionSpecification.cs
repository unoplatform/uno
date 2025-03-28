#nullable enable

using System;
using System.Diagnostics;

namespace Windows.UI.Composition;

// See https://learn.microsoft.com/en-us/uwp/api/windows.ui.composition.expressionanimation?view=winrt-22621#expression-functions-per-type
// Each function stated in this doc page is represented as a class that implements this interface.
// Classes implementing this interface should be added to the list of specifications in AnimationFunctionCallSyntax
internal interface IAnimationFunctionSpecification
{
	/// <summary>
	/// The number of parameters for this function
	/// </summary>
	int ParametersLength { get; }

	/// <summary>
	/// The name of the method.
	/// </summary>
	string MethodName { get; }

	/// <summary>
	/// A class name, or null if there is none.
	/// Note that some function calls are in the form ClassName.MethodName(arguments), for example
	/// Matrix3x2.CreateFromTranslation, and some others are just MethodName(arguments), for example Abs.
	/// </summary>
	string? ClassName { get; }

	object Evaluate(params object[] parameters);
}
