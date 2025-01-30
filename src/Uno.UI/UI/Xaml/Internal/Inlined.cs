using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Xaml.Internal;

internal static class Inlined
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsCloseReal(float a, float b) => Math.Abs(a - b) < float.Epsilon; // TODO Uno: The original logic is more complex, but might not be necessary in Uno.

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsCloseReal(double a, double b) => Math.Abs(a - b) < double.Epsilon; // TODO Uno: The original logic is more complex, but might not be necessary in Uno.
}
