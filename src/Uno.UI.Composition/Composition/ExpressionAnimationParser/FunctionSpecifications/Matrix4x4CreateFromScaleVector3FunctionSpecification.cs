#nullable enable

using System.Numerics;

namespace Microsoft.UI.Composition;

internal sealed class Matrix4x4CreateFromScaleVector3FunctionSpecification : IAnimationFunctionSpecification
{
	private Matrix4x4CreateFromScaleVector3FunctionSpecification()
	{
	}

	public static Matrix4x4CreateFromScaleVector3FunctionSpecification Instance { get; } = new();

	public int ParametersLength => 1;

	public string MethodName => "CreateFromScale";

	public string? ClassName => "Matrix4x4";

	public object Evaluate(params object[] parameters)
		=> Matrix4x4.CreateScale((Vector3)parameters[0]);
}
