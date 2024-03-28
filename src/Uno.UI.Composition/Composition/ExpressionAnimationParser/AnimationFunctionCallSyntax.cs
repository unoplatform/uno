using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace Windows.UI.Composition;

internal class AnimationFunctionCallSyntax : AnimationExpressionSyntax
{
	private AnimationExpressionSyntax _identifierOrMemberAccess;
	private ImmutableArray<AnimationExpressionSyntax> _arguments;

	private static ImmutableArray<IAnimationFunctionSpecification> _specifications = ImmutableArray.Create<IAnimationFunctionSpecification>(
		AbsFloatFunctionSpecification.Instance,
		MinFloatFloatFunctionSpecification.Instance,
		MaxFloatFloatFunctionSpecification.Instance,
		Vector2FloatFloatFunctionSpecification.Instance,
		Vector3FloatFloatFloatFunctionSpecification.Instance,
		ClampFloatFloatFloatFunctionSpecification.Instance
		);

	public AnimationFunctionCallSyntax(AnimationExpressionSyntax identifierOrMemberAccess, ImmutableArray<AnimationExpressionSyntax> arguments)
	{
		_identifierOrMemberAccess = identifierOrMemberAccess;
		_arguments = arguments;
	}

	public override object Evaluate(ExpressionAnimation expressionAnimation)
	{
		if (_identifierOrMemberAccess is AnimationIdentifierNameSyntax identifier)
		{
			return EvaluateFromIdentifier(identifier, expressionAnimation);
		}
		else if (_identifierOrMemberAccess is AnimationMemberAccessExpressionSyntax memberAccess)
		{
			return EvaluateFromMemberAccess(memberAccess, expressionAnimation);
		}

		throw new InvalidOperationException($"Unexpected type '{_identifierOrMemberAccess.GetType()}'");
	}

	private object EvaluateSpecification(IAnimationFunctionSpecification specification, ExpressionAnimation expressionAnimation)
	{
		if (_arguments.Length != specification.ParametersLength)
		{
			string call = specification.ClassName is null ? specification.MethodName : $"{specification.ClassName}.{specification.MethodName}";
			throw new ArgumentException($"A call to '{call}' should have '{specification.ParametersLength}' argument(s). Found '{_arguments.Length}' argument(s).");
		}

		return specification.Evaluate(_arguments.Select(arg => arg.Evaluate(expressionAnimation)).ToArray());
	}

	private object EvaluateFromIdentifier(AnimationIdentifierNameSyntax identifier, ExpressionAnimation expressionAnimation)
	{
		var name = (string)identifier.Identifier.Value;
		foreach (var specification in _specifications)
		{
			if (specification.ClassName is null &&
				// WinUI appears to be case-insensitive.
				// Actually, their usage of ExpressionAnimation in RefreshContainer uses `min` instead of `Min`
				specification.MethodName.Equals(name, StringComparison.OrdinalIgnoreCase) &&
				specification.ParametersLength == _arguments.Length)
			{
				return EvaluateSpecification(specification, expressionAnimation);
			}
		}

		throw new NotSupportedException($"Unsupported function call '{name}' with argument length '{_arguments.Length}'.");
	}

	private object EvaluateFromMemberAccess(AnimationMemberAccessExpressionSyntax memberAccess, ExpressionAnimation expressionAnimation)
	{
		// From https://learn.microsoft.com/en-us/uwp/api/windows.ui.composition.expressionanimation?view=winrt-22621
		// Supported calls with member access syntax are:
		// Matrix3x2.CreateFromScale(Vector2 scale)
		// Matrix3x2.CreateFromTranslation(Vector2 translation)
		// Matrix3x2.CreateSkew(Float x, Float y, Vector2 centerpoint)
		// Matrix3x2.CreateRotation(Float radians)
		// Matrix3x2.CreateTranslation(Vector2 translation)
		// Matrix3x2.CreateScale(Vector2 scale)
		// Matrix4x4.CreateFromScale(Vector3 scale)
		// Matrix4x4.CreateFromTranslation(Vector3 translation)
		// Matrix4x4.CreateTranslation(Vector3 translation)
		// Matrix4x4.CreateScale(Vector3 scale)
		// Matrix4x4.CreateFromAxisAngle(Vector3 axis, Float angle)
		// Quaternion.CreateFromAxisAngle(Vector3 axis, Scalar angle)
		if (memberAccess.Expression is not AnimationIdentifierNameSyntax identifier)
		{
			throw new ArgumentException("A function call throw member access should have the member access expression as an identifier.");
		}

		var className = (string)identifier.Identifier.Value;
		var methodName = (string)memberAccess.Identifier.Value;

		foreach (var specification in _specifications)
		{
			if (specification.ClassName?.Equals(className, StringComparison.Ordinal) == true &&
				specification.MethodName.Equals(methodName, StringComparison.Ordinal) &&
				specification.ParametersLength == _arguments.Length)
			{
				return EvaluateSpecification(specification, expressionAnimation);
			}
		}

		throw new NotSupportedException($"Unsupported function call '{className}.{methodName}' with argument length '{_arguments.Length}'.");
	}
}
