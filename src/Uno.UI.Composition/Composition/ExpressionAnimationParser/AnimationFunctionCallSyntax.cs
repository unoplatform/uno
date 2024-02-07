using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;

namespace Microsoft.UI.Composition;
internal class AnimationFunctionCallSyntax : AnimationExpressionSyntax
{
	private AnimationExpressionSyntax _identifierOrMemberAccess;
	private ImmutableArray<AnimationExpressionSyntax> _arguments;

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

	private object EvaluateFromIdentifier(AnimationIdentifierNameSyntax identifier, ExpressionAnimation expressionAnimation)
	{
		var name = (string)identifier.Identifier.Value;
		if (name == "Abs")
		{
			if (_arguments.Length != 1)
			{
				throw new ArgumentException($"A call to Abs should have one argument. Found '{_arguments.Length}' arguments.");
			}

			return Math.Abs(Convert.ToSingle(_arguments[0].Evaluate(expressionAnimation), CultureInfo.InvariantCulture));
		}

		if (name == "Max")
		{
			if (_arguments.Length != 2)
			{
				throw new ArgumentException($"A call to Max should have two arguments. Found '{_arguments.Length}' argument(s).");
			}

			var arg1 = _arguments[0].Evaluate(expressionAnimation);
			var arg2 = _arguments[1].Evaluate(expressionAnimation);

			return Math.Max(Convert.ToSingle(arg1, CultureInfo.InvariantCulture), Convert.ToSingle(arg2, CultureInfo.InvariantCulture));
		}

		if (name == "Min")
		{
			if (_arguments.Length != 2)
			{
				throw new ArgumentException($"A call to Min should have two arguments. Found '{_arguments.Length}' argument(s).");
			}

			var arg1 = _arguments[0].Evaluate(expressionAnimation);
			var arg2 = _arguments[1].Evaluate(expressionAnimation);

			return Math.Min(Convert.ToSingle(arg1, CultureInfo.InvariantCulture), Convert.ToSingle(arg2, CultureInfo.InvariantCulture));
		}

		if (name == "Vector2")
		{
			if (_arguments.Length != 2)
			{
				throw new ArgumentException($"A call to Vector2 constructor should have two arguments. Found '{_arguments.Length}' argument(s).");
			}

			var arg1 = _arguments[0].Evaluate(expressionAnimation);
			var arg2 = _arguments[1].Evaluate(expressionAnimation);

			return new Vector2(Convert.ToSingle(arg1, CultureInfo.InvariantCulture), Convert.ToSingle(arg2, CultureInfo.InvariantCulture));
		}

		if (name == "Vector3")
		{
			if (_arguments.Length != 3)
			{
				throw new ArgumentException($"A call to Vector3 constructor should have three arguments. Found '{_arguments.Length}' argument(s).");
			}

			var arg1 = _arguments[0].Evaluate(expressionAnimation);
			var arg2 = _arguments[1].Evaluate(expressionAnimation);
			var arg3 = _arguments[2].Evaluate(expressionAnimation);

			return new Vector3(Convert.ToSingle(arg1, CultureInfo.InvariantCulture), Convert.ToSingle(arg2, CultureInfo.InvariantCulture), Convert.ToSingle(arg3, CultureInfo.InvariantCulture));
		}

		throw new NotSupportedException($"Unsupported function call '{name}'.");
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
		throw new NotSupportedException($"Unsupported function call.");
	}
}
