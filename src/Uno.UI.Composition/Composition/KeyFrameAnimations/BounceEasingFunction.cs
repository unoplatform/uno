using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition;

public partial class BounceEasingFunction
{
	public CompositionEasingFunctionMode Mode { get; }
	public int Bounces { get; }
	public float Bounciness { get; }

	internal BounceEasingFunction(Compositor owner, CompositionEasingFunctionMode mode, int bounces, float bounciness) : base(owner)
	{
		Mode = mode;
		Bounces = Math.Max(0, bounces);
		Bounciness = float.IsFinite(bounciness) && bounciness >= 1.01f ? bounciness : 1.01f;
	}

	internal override float Ease(float t) => Ease(t, Mode);

	// References:
	// https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Media/Animation/BounceEase.cs
	// https://github.com/microsoft/microsoft-ui-xaml/blob/main/src/dxaml/xcp/components/animation/EasingFunctions.cpp

	internal override float EaseIn(float t)
	{
		var fn = Bounces;
		var fb = Bounciness;

		var a = ((1.0f - MathF.Pow(fb, fn)) / (1.0f - fb)) + (MathF.Pow(fb, fn) / 2.0f);
		var b = MathF.Floor(MathF.Log(1.0f - a * t * (1.0f - fb)) / MathF.Log(fb));
		var c = (1.0f - MathF.Pow(fb, b)) / ((1.0f - fb) * a);
		var d = (1.0f - MathF.Pow(fb, b + 1.0f)) / ((1.0f - fb) * a);
		var e = 1.0f / MathF.Pow(fb, fn - b);
		var f = (d - c) / 2.0f;
		var g = t - (c + f);

		return (-e / (f * f)) * (g - f) * (g + f);
	}
}
