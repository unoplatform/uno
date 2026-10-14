using System;
using System.Runtime.CompilerServices;

namespace Uno.UI.Composition;

internal static class CompositionMathHelpers
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsCloseReal(float a, float b, float epsilon = 10.0f * float.Epsilon)
		=> MathF.Abs(a - b) <= epsilon;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsCloseRealZero(float a, float epsilon = 10.0f * float.Epsilon)
		=> MathF.Abs(a) < epsilon;
}
