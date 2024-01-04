#nullable enable
using System;
using System.Linq;
using SkiaSharp;

namespace Microsoft.UI.Composition;

partial class InsetClip
{
	internal SKRect SKRect => new()
	{
		Top = TopInset - 1,
		Bottom = BottomInset + 1,
		Left = LeftInset - 1,
		Right = RightInset + 1
	};
}
