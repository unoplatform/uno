using System;
using System.ComponentModel;

namespace Uno.UI.Helpers;

#pragma warning disable UnoInternal0001 // This is the special boxes implementation :)

/// <summary>
/// This class is intended to be used by code generated from DependencyObjectGenerator.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class Boxes
{
	private static class BooleanBoxes
	{
		public static readonly object BoxedTrue = true;
		public static readonly object BoxedFalse = false;
	}

	private static class IntegerBoxes
	{
		public static readonly object NegativeOne = -1;
		public static readonly object Zero = 0;
		public static readonly object One = 1;
	}

	internal static class DefaultBox<T> where T : struct
	{
		public static readonly object Value = default(T);
	}

	public static object Box(bool value) => value ? BooleanBoxes.BoxedTrue : BooleanBoxes.BoxedFalse;

	public static object Box(int value) => value switch
	{
		// Keep the specialized integers in sync with BoxingDiagnosticAnalyzer
		-1 => IntegerBoxes.NegativeOne,
		0 => DefaultBox<int>.Value,
		1 => IntegerBoxes.One,
		_ => value,
	};
}
