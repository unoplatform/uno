using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Graphics.Canvas.Geometry;

/// <summary>
/// Specifies whether an arc should be greater than 180 degrees.
/// </summary>
internal enum CanvasArcSize
{
	/// <summary>
	/// The arc's sweep should be 180 degrees or less.
	/// </summary>
	Small = 0,

	/// <summary>
	/// The arc's sweep should be 180 degrees or greater.
	/// </summary>
	Large = 1
}
