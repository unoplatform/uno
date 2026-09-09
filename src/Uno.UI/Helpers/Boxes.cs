using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml;

namespace Uno.UI.Helpers;


internal static class Boxes
{
	public static class BooleanBoxes
	{
		public static readonly object BoxedTrue = true;
		public static readonly object BoxedFalse = false;
	}

	public static class VerticalAlignmentBoxes
	{
		public static readonly object Top = VerticalAlignment.Top;
		public static readonly object Bottom = VerticalAlignment.Bottom;
		public static readonly object Stretch = VerticalAlignment.Stretch;
		public static readonly object Center = VerticalAlignment.Center;
	}

	public static class NullableDoubleBoxes
	{
		// The CLR boxes a Nullable<T> as a boxed T (or null), so a boxed double? is
		// indistinguishable from a boxed double and there is nothing separate to cache.
		public static readonly object Zero = 0.0d;
		public static readonly object One = 1.0d;
	}

	public static class StretchBoxes
	{
		public static readonly object None = Stretch.None;
		public static readonly object Fill = Stretch.Fill;
		public static readonly object Uniform = Stretch.Uniform;
		public static readonly object UniformToFill = Stretch.UniformToFill;
	}

	public static object Box(bool value) => value ? BooleanBoxes.BoxedTrue : BooleanBoxes.BoxedFalse;

	public static object Box(VerticalAlignment value) => value switch
	{
		VerticalAlignment.Top => VerticalAlignmentBoxes.Top,
		VerticalAlignment.Bottom => VerticalAlignmentBoxes.Bottom,
		VerticalAlignment.Stretch => VerticalAlignmentBoxes.Stretch,
		VerticalAlignment.Center => VerticalAlignmentBoxes.Center,
		_ => value,
	};

	public static object Box(Stretch value) => value switch
	{
		Stretch.None => StretchBoxes.None,
		Stretch.Fill => StretchBoxes.Fill,
		Stretch.Uniform => StretchBoxes.Uniform,
		Stretch.UniformToFill => StretchBoxes.UniformToFill,
		_ => value,
	};
}
