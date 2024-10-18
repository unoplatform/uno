using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
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
		public static readonly VerticalAlignment Top = VerticalAlignment.Top;
		public static readonly VerticalAlignment Bottom = VerticalAlignment.Bottom;
		public static readonly VerticalAlignment Stretch = VerticalAlignment.Stretch;
		public static readonly VerticalAlignment Center = VerticalAlignment.Center;
	}

	public static class NullableDoubleBoxes
	{
		public static readonly double? Zero = 0.0d;
		public static readonly double? One = 1.0d;
	}

	public static class StretchBoxes
	{
		public static readonly Stretch None = Stretch.None;
		public static readonly Stretch Fill = Stretch.Fill;
		public static readonly Stretch Uniform = Stretch.Uniform;
		public static readonly Stretch UniformToFill = Stretch.UniformToFill;
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
