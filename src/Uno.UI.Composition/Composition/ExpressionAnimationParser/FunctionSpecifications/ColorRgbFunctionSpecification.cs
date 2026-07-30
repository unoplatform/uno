#nullable enable

using System;
using System.Globalization;
using Windows.UI;

namespace Microsoft.UI.Composition;

// Used by Lottie-generated animations that bind colors via theme properties stored as Vector4 (A, R, G, B).
internal sealed class ColorRgbFunctionSpecification : IAnimationFunctionSpecification
{
	private ColorRgbFunctionSpecification()
	{
	}

	public static ColorRgbFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 4;

	public string MethodName => "ColorRGB";

	public string? ClassName => null;

	public object Evaluate(params object[] parameters)
	{
		var a = Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture);
		var r = Convert.ToSingle(parameters[1], CultureInfo.InvariantCulture);
		var g = Convert.ToSingle(parameters[2], CultureInfo.InvariantCulture);
		var b = Convert.ToSingle(parameters[3], CultureInfo.InvariantCulture);

		return Color.FromArgb(
			(byte)Math.Clamp(a, 0, 255),
			(byte)Math.Clamp(r, 0, 255),
			(byte)Math.Clamp(g, 0, 255),
			(byte)Math.Clamp(b, 0, 255));
	}
}
