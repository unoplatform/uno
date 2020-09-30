#nullable enable

using System;
using System.Linq;

namespace Uno.UITest;

public interface IAppRect
{
	float Width { get; }
	float Height { get; }
	float X { get; }
	float Y { get; }
	float CenterX { get; }
	float CenterY { get; }
	float Right { get; }
	float Bottom { get; }
}
