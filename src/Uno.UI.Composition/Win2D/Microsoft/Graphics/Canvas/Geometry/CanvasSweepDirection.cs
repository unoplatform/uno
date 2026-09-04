using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Graphics.Canvas.Geometry;

/// <summary>
/// Defines the direction in which an elliptical arc is drawn.
/// </summary>
internal enum CanvasSweepDirection
{
	/// <summary>
	/// Arcs are drawn in a counterclockwise (negative-angle) direction.
	/// </summary>
	CounterClockwise = 0,

	/// <summary>
	/// Arcs are drawn in a clockwise (positive-angle) direction.
	/// </summary>
	Clockwise = 1
}
