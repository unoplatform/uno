using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Uno.Extensions
{
	internal static class DoubleExtensions
	{
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool IsFinite(this double value)
		{
#if !XAMARIN
			return !double.IsInfinity(value) && !double.IsNaN(value);
#else
			return double.IsFinite(value);
#endif
		}

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool IsFinite(this float value)
		{
#if !XAMARIN
			return !float.IsInfinity(value) && !float.IsNaN(value);
#else
			return float.IsFinite(value);
#endif
		}
	}
}
